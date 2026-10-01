using System.Collections;
using UnityEngine;

public class CritEffectManager : MonoBehaviour
{
    public static CritEffectManager Instance { get; private set; }

    [SerializeField] private Light mainDirectionalLight;

    [SerializeField] private float darkenMultiplier = 0.1f;
    [SerializeField] private float darkDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.15f; 

    [SerializeField] private float hitStopDuration = 0.15f;
    [SerializeField] private float hitStopTimeScale = 0.05f;

    private float originalLightIntensity;
    private float originalAmbientIntensity;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mainDirectionalLight != null)
        {
            originalLightIntensity = mainDirectionalLight.intensity;
        }
        originalAmbientIntensity = RenderSettings.ambientIntensity;
    }

    public void StartDarkScreen()
    {
        StartCoroutine(DarkScreenRoutine());
    }

    private IEnumerator DarkScreenRoutine()
    {
        float timer = 0f;
        float targetLight = originalLightIntensity * darkenMultiplier;
        float targetAmbient = originalAmbientIntensity * darkenMultiplier;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / fadeDuration;

            if (mainDirectionalLight != null)
                mainDirectionalLight.intensity = Mathf.Lerp(originalLightIntensity, targetLight, progress);
            RenderSettings.ambientIntensity = Mathf.Lerp(originalAmbientIntensity, targetAmbient, progress);

            yield return null;
        }

        yield return new WaitForSecondsRealtime(darkDuration);

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / fadeDuration;

            if (mainDirectionalLight != null)
                mainDirectionalLight.intensity = Mathf.Lerp(targetLight, originalLightIntensity, progress);
            RenderSettings.ambientIntensity = Mathf.Lerp(targetAmbient, originalAmbientIntensity, progress);

            yield return null;
        }

        if (mainDirectionalLight != null) mainDirectionalLight.intensity = originalLightIntensity;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
    }

    public void StartHitStop()
    {
        StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }
}