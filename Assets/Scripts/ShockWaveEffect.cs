using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockWaveEffect : MonoBehaviour
{
    [SerializeField] private Transform shockwaveRing;

    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float expandSpeed = 2f;
    [SerializeField] private float maxScale = 50f;
    [SerializeField] private AnimationCurve expandCurve;
    [SerializeField] private float flashDelay = 0.5f;

    private Material ringMaterial;
    private Color startColor;

    private void Awake()
    {
        if (expandCurve.length == 0)
        {
            expandCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }

        if (shockwaveRing != null)
        {
            Renderer renderer = shockwaveRing.GetComponent<Renderer>();
            if (renderer != null)
            {
                ringMaterial = renderer.material;
                startColor = ringMaterial.color;
            }
            shockwaveRing.gameObject.SetActive(false);
        }
    }

    public void TriggerExplosionVisual()
    {
        StartCoroutine(ExplosionSequenceRoutine());
    }

    private IEnumerator ExplosionSequenceRoutine()
    {
        

        if (shockwaveRing != null)
        {
            shockwaveRing.gameObject.SetActive(true);
            StartCoroutine(ExpandRingRoutine());
        }

        yield return new WaitForSeconds(flashDelay);

        if (ScreenShake.Instance != null) ScreenShake.Instance.Shake(shakeIntensity);

        if (ScreenFlashUI.Instance != null)
        {
            ScreenFlashUI.Instance.TriggerFlash();
        }
    }

    private IEnumerator ExpandRingRoutine()
    {
        float timer = 0f;
        float duration = 1f;

        while (timer < duration)
        {
            timer += Time.deltaTime * expandSpeed;
            float progress = timer / duration;

            float currentScale = expandCurve.Evaluate(progress) * maxScale;
            shockwaveRing.localScale = new Vector3(currentScale, currentScale, currentScale);

            if (ringMaterial != null)
            {
                float currentAlpha = Mathf.Lerp(startColor.a, 0f, progress);
                ringMaterial.color = new Color(startColor.r, startColor.g, startColor.b, currentAlpha);
            }

            yield return null;
        }

        shockwaveRing.gameObject.SetActive(false);
    }
}