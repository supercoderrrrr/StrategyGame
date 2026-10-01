using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    private static readonly int RevealAmountId = Shader.PropertyToID("_RevealAmount");

    [SerializeField] private GameObject fogVisualObject; //Block Object
    [SerializeField] private bool startRevealed = false; //The starting room
    [SerializeField] private List<Unit> enemiesInRoom; //Enemies in this room

    [Header("Fog Reveal")]
    [SerializeField, Min(0.1f)] private float fogRevealDuration = 1.0f;
    [SerializeField] private Shader fogDissolveShader;

    private bool isRevealed;
    private Renderer[] fogRenderers;
    private Collider[] fogColliders;
    private Material[][] originalFogMaterials;
    private Material fogRuntimeMaterial;
    private MaterialPropertyBlock fogPropertyBlock;
    private Coroutine revealCoroutine;

    private void Awake()
    {
        CacheFogVisual();
    }

    private void Start()
    {
        if (startRevealed)
        {
            RevealRoomImmediately();
        }
        else
        {
            HideRoom();
        }
    }

    private void HideRoom()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        isRevealed = false;
        if (fogVisualObject != null)
        {
            fogVisualObject.SetActive(true);
            SetFogCollidersEnabled(true);
            SetRevealAmount(0f);
        }

        //Enemy in the room that is not revealed
        foreach (Unit enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.SetIsHidden(true);
            }
        }
    }

    public void RevealRoom()
    {
        if (isRevealed) return;

        isRevealed = true;
        SetFogCollidersEnabled(false);

        //Activate enemies
        foreach (Unit enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.SetIsHidden(false);
            }
        }

        if (fogVisualObject == null || fogRuntimeMaterial == null)
        {
            if (fogVisualObject != null) fogVisualObject.SetActive(false);
            return;
        }

        revealCoroutine = StartCoroutine(AnimateFogReveal());
    }

    public bool IsRevealed() => isRevealed;

    private void CacheFogVisual()
    {
        if (fogVisualObject == null) return;

        fogRenderers = fogVisualObject.GetComponentsInChildren<Renderer>(true);
        fogColliders = fogVisualObject.GetComponentsInChildren<Collider>(true);
        if (fogRenderers.Length == 0) return;

        Shader dissolveShader = fogDissolveShader != null
            ? fogDissolveShader
            : Resources.Load<Shader>("Shaders/FogOfWarDissolve");

        if (dissolveShader == null)
        {
            Debug.LogWarning("FogOfWarDissolve shader could not be loaded. Room fog will reveal instantly.", this);
            return;
        }

        fogRuntimeMaterial = new Material(dissolveShader)
        {
            name = $"{name}_FogOfWarDissolve (Runtime)"
        };
        fogPropertyBlock = new MaterialPropertyBlock();
        originalFogMaterials = new Material[fogRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < fogRenderers.Length; rendererIndex++)
        {
            Renderer fogRenderer = fogRenderers[rendererIndex];
            originalFogMaterials[rendererIndex] = fogRenderer.sharedMaterials;

            Material[] dissolveMaterials = new Material[originalFogMaterials[rendererIndex].Length];
            for (int materialIndex = 0; materialIndex < dissolveMaterials.Length; materialIndex++)
            {
                dissolveMaterials[materialIndex] = fogRuntimeMaterial;
            }

            fogRenderer.sharedMaterials = dissolveMaterials;
        }

        SetRevealAmount(0f);
    }

    private IEnumerator AnimateFogReveal()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, fogRevealDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetRevealAmount(Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        SetRevealAmount(1f);
        fogVisualObject.SetActive(false);
        revealCoroutine = null;
    }

    private void RevealRoomImmediately()
    {
        isRevealed = true;
        SetFogCollidersEnabled(false);
        if (fogVisualObject != null) fogVisualObject.SetActive(false);

        foreach (Unit enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.SetIsHidden(false);
            }
        }
    }

    private void SetRevealAmount(float amount)
    {
        if (fogRenderers == null || fogPropertyBlock == null) return;

        foreach (Renderer fogRenderer in fogRenderers)
        {
            if (fogRenderer == null) continue;

            fogRenderer.GetPropertyBlock(fogPropertyBlock);
            fogPropertyBlock.SetFloat(RevealAmountId, amount);
            fogRenderer.SetPropertyBlock(fogPropertyBlock);
        }
    }

    private void SetFogCollidersEnabled(bool enabled)
    {
        if (fogColliders == null) return;

        foreach (Collider fogCollider in fogColliders)
        {
            if (fogCollider != null)
            {
                fogCollider.enabled = enabled;
            }
        }
    }

    private void OnDestroy()
    {
        if (fogRenderers != null && originalFogMaterials != null)
        {
            for (int rendererIndex = 0; rendererIndex < fogRenderers.Length; rendererIndex++)
            {
                if (fogRenderers[rendererIndex] != null)
                {
                    fogRenderers[rendererIndex].sharedMaterials = originalFogMaterials[rendererIndex];
                }
            }
        }

        if (fogRuntimeMaterial != null)
        {
            Destroy(fogRuntimeMaterial);
        }
    }
}
