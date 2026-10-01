using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private GameObject transitionCanvasPrefab;
    [SerializeField] private float transitionDuration = 0.8f;

    [Header("Multi-Layer Colors")]
    [SerializeField]
    private Color[] transitionColors = new Color[]
    {
        new Color(0.2f, 0.2f, 0.2f, 1f),
        Color.black 
    };
    [SerializeField] private float layerDelay = 0.15f;

    private GameObject canvasInstance;
    private Material[] transitionMaterials;
    private int radiusPropID;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (transitionCanvasPrefab != null)
        {
            canvasInstance = Instantiate(transitionCanvasPrefab);
            canvasInstance.transform.SetParent(transform);

            Image baseImage = canvasInstance.GetComponentInChildren<Image>();

            if (baseImage != null)
            {
                radiusPropID = Shader.PropertyToID("_Radius");

                if (transitionColors == null || transitionColors.Length == 0)
                {
                    transitionColors = new Color[] { Color.black };
                }

                transitionMaterials = new Material[transitionColors.Length];

                for (int i = 0; i < transitionColors.Length; i++)
                {
                    GameObject layerObj = Instantiate(baseImage.gameObject, baseImage.transform.parent);
                    layerObj.name = "TransitionLayer_" + i;

                    Image img = layerObj.GetComponent<Image>();

                    Material mat = new Material(baseImage.material);
                    mat.SetColor("_Color", transitionColors[i]);
                    mat.SetFloat(radiusPropID, 1.5f);

                    img.material = mat;
                    transitionMaterials[i] = mat;
                }

                Destroy(baseImage.gameObject);
            }
            else
            {
                Debug.LogError("SceneLoader: Transition Prefab 里找不到 Image 组件！");
            }

            canvasInstance.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (canvasInstance != null) canvasInstance.SetActive(true);

        float totalDuration = transitionDuration + (transitionColors.Length - 1) * layerDelay;

        float timer = 0f;
        while (timer < totalDuration)
        {
            for (int i = 0; i < transitionColors.Length; i++)
            {
                float layerTimer = timer - (i * layerDelay);
                float progress = Mathf.Clamp01(layerTimer / transitionDuration);

                float radius = Mathf.Lerp(1.5f, 0f, progress);
                if (transitionMaterials[i] != null) transitionMaterials[i].SetFloat(radiusPropID, radius);
            }
            yield return null;
            timer += Time.unscaledDeltaTime;
        }

        foreach (var mat in transitionMaterials)
        {
            if (mat != null) mat.SetFloat(radiusPropID, 0f);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f) yield return null;

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone) yield return null;

        yield return null;
        yield return null;

        timer = 0f;
        while (timer < totalDuration)
        {
            for (int i = 0; i < transitionColors.Length; i++)
            {
                float layerDelayOffset = (transitionColors.Length - 1 - i) * layerDelay;
                float layerTimer = timer - layerDelayOffset;

                float progress = Mathf.Clamp01(layerTimer / transitionDuration);
                float radius = Mathf.Lerp(0f, 1.5f, progress);

                if (transitionMaterials[i] != null) transitionMaterials[i].SetFloat(radiusPropID, radius);
            }
            yield return null;
            timer += Time.unscaledDeltaTime;
        }

        foreach (var mat in transitionMaterials)
        {
            if (mat != null) mat.SetFloat(radiusPropID, 1.5f);
        }

        if (canvasInstance != null) canvasInstance.SetActive(false);
    }
}