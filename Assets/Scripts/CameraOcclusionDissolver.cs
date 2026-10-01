using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks only renderers between the gameplay camera and the selected unit.
/// Original shared materials are restored after the obstruction fades back in.
/// </summary>
public sealed class CameraOcclusionDissolver
{
    private const float OcclusionProbeRadius = .04f;
    private const float SurfaceOverlapRadius = .1f;

    private sealed class OccluderState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public MaterialPropertyBlock propertyBlock;
        public Texture baseTexture;
        public Vector4 baseMapTransform;
        public Color baseColor;
        public Vector3 cutoutPositionWorld;
        public float amount;
        public bool isObstructing;
    }

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseMapTransformId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int CutoutPositionWorldId = Shader.PropertyToID("_CutoutPositionWS");
    private static readonly int CutoutRadiusId = Shader.PropertyToID("_CutoutRadius");
    private static readonly int CutoutAmountId = Shader.PropertyToID("_CutoutAmount");
    private static readonly int CutoutEdgeWidthId = Shader.PropertyToID("_CutoutEdgeWidth");
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");

    private readonly Dictionary<Renderer, OccluderState> states = new Dictionary<Renderer, OccluderState>();
    private readonly List<Renderer> removalBuffer = new List<Renderer>();
    private readonly RaycastHit[] hitBuffer = new RaycastHit[64];
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly LayerMask occluderMask;
    private readonly float dissolveSpeed;
    private readonly float restoreSpeed;
    private readonly float cutoutRadius;
    private readonly float cutoutEdgeWidth;
    private readonly Color edgeColor;
    private readonly Material dissolveMaterial;
    private Unit trackedUnit;
    private Unit boundsUnit;
    private Renderer[] boundsRenderers;

    public CameraOcclusionDissolver(
        LayerMask occluderMask,
        float dissolveSpeed,
        float restoreSpeed,
        float cutoutRadius,
        float cutoutEdgeWidth,
        Color edgeColor)
    {
        this.occluderMask = occluderMask;
        this.dissolveSpeed = Mathf.Max(.01f, dissolveSpeed);
        this.restoreSpeed = Mathf.Max(.01f, restoreSpeed);
        this.cutoutRadius = Mathf.Max(.01f, cutoutRadius);
        this.cutoutEdgeWidth = Mathf.Max(.001f, cutoutEdgeWidth);
        this.edgeColor = edgeColor;

        Shader shader = Shader.Find("StrategyGame/TacticalWallDissolve");
        if (shader != null)
        {
            dissolveMaterial = new Material(shader)
            {
                name = "Runtime Tactical Wall Dissolve",
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
        else
        {
            Debug.LogError("Tactical wall dissolve shader could not be found.");
        }
    }

    public void Update(Camera gameplayCamera, Unit selectedUnit, float deltaTime)
    {
        bool restartTransition = trackedUnit != selectedUnit;
        if (restartTransition)
        {
            trackedUnit = selectedUnit;
            boundsUnit = null;
            boundsRenderers = null;
        }

        foreach (KeyValuePair<Renderer, OccluderState> pair in states)
            pair.Value.isObstructing = false;

        if (gameplayCamera != null && selectedUnit != null && dissolveMaterial != null)
        {
            Bounds unitBounds = GetUnitBounds(selectedUnit);
            if (Vector3.Dot(unitBounds.center - gameplayCamera.transform.position, gameplayCamera.transform.forward) > 0f)
            {
                MarkCurrentOccluders(gameplayCamera, unitBounds, restartTransition);
            }
        }

        AnimateStates(deltaTime);
    }

    public void Dispose()
    {
        foreach (KeyValuePair<Renderer, OccluderState> pair in states)
            RestoreRenderer(pair.Value);
        states.Clear();

        if (dissolveMaterial != null)
        {
            if (Application.isPlaying) Object.Destroy(dissolveMaterial);
            else Object.DestroyImmediate(dissolveMaterial);
        }
    }

    private void MarkCurrentOccluders(Camera gameplayCamera, Bounds unitBounds, bool restartTransition)
    {
        Vector3 origin = gameplayCamera.transform.position;
        Vector3 target = unitBounds.center;
        float verticalRadius = Mathf.Max(.25f, unitBounds.extents.y * .8f);
        float horizontalRadius = Mathf.Max(.15f, Mathf.Max(unitBounds.extents.x, unitBounds.extents.z) * .7f);
        Vector3 right = gameplayCamera.transform.right * horizontalRadius;
        Vector3 up = Vector3.up * verticalRadius;

        MarkCameraNearClipOverlaps(gameplayCamera, unitBounds.center, restartTransition);

        MarkOccludersBetween(target, origin, unitBounds.center, restartTransition);
        MarkOccludersBetween(target + right, origin, unitBounds.center, restartTransition);
        MarkOccludersBetween(target - right, origin, unitBounds.center, restartTransition);
        MarkOccludersBetween(target + up, origin, unitBounds.center, restartTransition);
        MarkOccludersBetween(target - up, origin, unitBounds.center, restartTransition);
    }

    private void MarkOccludersBetween(
        Vector3 unitSample,
        Vector3 cameraPosition,
        Vector3 cutoutPosition,
        bool restartTransition)
    {
        MarkOverlappingOccluders(
            unitSample,
            OcclusionProbeRadius,
            cutoutPosition,
            restartTransition);
        MarkOccludersAlongCast(unitSample, cameraPosition, cutoutPosition, restartTransition);
        MarkOccludersAlongCast(cameraPosition, unitSample, cutoutPosition, restartTransition);
    }

    private void MarkOccludersAlongCast(
        Vector3 origin,
        Vector3 target,
        Vector3 cutoutPosition,
        bool restartTransition)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= Mathf.Epsilon) return;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            OcclusionProbeRadius,
            direction / distance,
            hitBuffer,
            distance,
            occluderMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            MarkCollider(hitBuffer[i].collider, cutoutPosition, restartTransition);
            MarkOverlappingOccluders(
                hitBuffer[i].point,
                SurfaceOverlapRadius,
                cutoutPosition,
                restartTransition);
        }
    }

    private void MarkCameraNearClipOverlaps(
        Camera gameplayCamera,
        Vector3 cutoutPosition,
        bool restartTransition)
    {
        float near = Mathf.Max(gameplayCamera.nearClipPlane, OcclusionProbeRadius);
        float halfHeight = gameplayCamera.orthographic
            ? gameplayCamera.orthographicSize
            : near * Mathf.Tan(gameplayCamera.fieldOfView * .5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * gameplayCamera.aspect;

        MarkOverlappingOccluders(
            gameplayCamera.transform.position,
            OcclusionProbeRadius,
            cutoutPosition,
            restartTransition);

        int overlapCount = Physics.OverlapBoxNonAlloc(
            gameplayCamera.transform.position + gameplayCamera.transform.forward * near,
            new Vector3(halfWidth, halfHeight, OcclusionProbeRadius),
            overlapBuffer,
            gameplayCamera.transform.rotation,
            occluderMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
            MarkCollider(overlapBuffer[i], cutoutPosition, restartTransition);
    }

    private void MarkOverlappingOccluders(
        Vector3 position,
        float radius,
        Vector3 cutoutPosition,
        bool restartTransition)
    {
        int overlapCount = Physics.OverlapSphereNonAlloc(
            position,
            radius,
            overlapBuffer,
            occluderMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
            MarkCollider(overlapBuffer[i], cutoutPosition, restartTransition);
    }

    private void MarkCollider(Collider collider, Vector3 cutoutPosition, bool restartTransition)
    {
        if (collider == null) return;

        Renderer renderer = collider.GetComponentInParent<Renderer>();
        if (renderer == null) return;

        if (!states.TryGetValue(renderer, out OccluderState state))
        {
            state = CreateState(renderer);
            states.Add(renderer, state);
        }
        else if (restartTransition)
        {
            state.amount = 0f;
        }

        state.cutoutPositionWorld = cutoutPosition;
        state.isObstructing = true;
    }

    private Bounds GetUnitBounds(Unit selectedUnit)
    {
        if (boundsUnit != selectedUnit)
        {
            boundsUnit = selectedUnit;
            boundsRenderers = selectedUnit.GetComponentsInChildren<Renderer>();
        }

        Bounds bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < boundsRenderers.Length; i++)
        {
            if (boundsRenderers[i] == null) continue;
            if (!hasBounds)
            {
                bounds = boundsRenderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(boundsRenderers[i].bounds);
            }
        }

        return hasBounds
            ? bounds
            : new Bounds(selectedUnit.GetWorldPosition() + Vector3.up, new Vector3(.5f, 2f, .5f));
    }

    private OccluderState CreateState(Renderer renderer)
    {
        Material[] originals = renderer.sharedMaterials;
        Material source = originals.Length > 0 ? originals[0] : null;
        Material[] replacements = new Material[originals.Length];
        for (int i = 0; i < replacements.Length; i++) replacements[i] = dissolveMaterial;

        Texture texture = Texture2D.whiteTexture;
        Vector2 textureScale = Vector2.one;
        Vector2 textureOffset = Vector2.zero;
        Color color = Color.white;
        if (source != null)
        {
            string textureProperty = null;
            if (source.HasProperty("_Grid")) textureProperty = "_Grid";
            else if (source.HasProperty("_BaseMap")) textureProperty = "_BaseMap";
            else if (source.HasProperty("_MainTex")) textureProperty = "_MainTex";

            if (textureProperty != null)
            {
                texture = source.GetTexture(textureProperty);
                textureScale = source.GetTextureScale(textureProperty);
                textureOffset = source.GetTextureOffset(textureProperty);
            }

            if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color")) color = source.GetColor("_Color");
        }

        renderer.sharedMaterials = replacements;
        return new OccluderState
        {
            renderer = renderer,
            originalMaterials = originals,
            propertyBlock = new MaterialPropertyBlock(),
            baseTexture = texture != null ? texture : Texture2D.whiteTexture,
            baseMapTransform = new Vector4(textureScale.x, textureScale.y, textureOffset.x, textureOffset.y),
            baseColor = color,
        };
    }

    private void AnimateStates(float deltaTime)
    {
        removalBuffer.Clear();
        foreach (KeyValuePair<Renderer, OccluderState> pair in states)
        {
            OccluderState state = pair.Value;
            if (state.renderer == null)
            {
                removalBuffer.Add(pair.Key);
                continue;
            }

            float target = state.isObstructing ? 1f : 0f;
            float speed = state.isObstructing ? dissolveSpeed : restoreSpeed;
            state.amount = Mathf.MoveTowards(state.amount, target, speed * deltaTime);
            ApplyProperties(state);

            if (!state.isObstructing && state.amount <= 0f)
            {
                RestoreRenderer(state);
                removalBuffer.Add(pair.Key);
            }
        }

        for (int i = 0; i < removalBuffer.Count; i++) states.Remove(removalBuffer[i]);
    }

    private void ApplyProperties(OccluderState state)
    {
        state.propertyBlock.Clear();
        state.propertyBlock.SetTexture(BaseMapId, state.baseTexture);
        state.propertyBlock.SetVector(BaseMapTransformId, state.baseMapTransform);
        state.propertyBlock.SetColor(BaseColorId, state.baseColor);
        state.propertyBlock.SetColor(EdgeColorId, edgeColor);
        state.propertyBlock.SetVector(CutoutPositionWorldId, state.cutoutPositionWorld);
        state.propertyBlock.SetFloat(CutoutRadiusId, cutoutRadius);
        state.propertyBlock.SetFloat(CutoutEdgeWidthId, cutoutEdgeWidth);
        state.propertyBlock.SetFloat(CutoutAmountId, state.amount);
        state.renderer.SetPropertyBlock(state.propertyBlock);
    }

    private static void RestoreRenderer(OccluderState state)
    {
        if (state.renderer == null) return;
        state.renderer.SetPropertyBlock(null);
        state.renderer.sharedMaterials = state.originalMaterials;
    }
}
