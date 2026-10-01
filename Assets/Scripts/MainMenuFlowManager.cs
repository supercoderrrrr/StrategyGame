using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuFlowManager : MonoBehaviour
{
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform startViewPoint;
    [SerializeField] private Transform mapViewPoint;
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private AnimationCurve movementCurve;

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        // MainMenuUI is the current menu controller. Keep this component only as
        // a fallback for older scenes that do not contain MainMenuUI.
        if (FindObjectOfType<MainMenuUI>() != null)
        {
            enabled = false;
        }
    }

    private void Start()
    {
        mainCamera.position = startViewPoint.position;
        mainCamera.rotation = startViewPoint.rotation;

        mainMenuPanel.SetActive(true);
        levelSelectPanel.SetActive(false);

        startButton.onClick.AddListener(() => {
            StartCoroutine(MoveCamera(startViewPoint, mapViewPoint, true));
        });

        backButton.onClick.AddListener(() => {
            StartCoroutine(MoveCamera(mapViewPoint, startViewPoint, false));
        });
    }

    private IEnumerator MoveCamera(Transform from, Transform to, bool showLevelSelectAtEnd)
    {
        mainMenuPanel.SetActive(false);
        levelSelectPanel.SetActive(false);

        float timer = 0f;

        Vector3 startPos = from.position;
        Quaternion startRot = from.rotation;

        Vector3 endPos = to.position;
        Quaternion endRot = to.rotation;

        while (timer < 1f)
        {
            timer += Time.deltaTime / transitionDuration;

            float curveValue = movementCurve.Evaluate(timer);

            mainCamera.position = Vector3.Lerp(startPos, endPos, curveValue);
            mainCamera.rotation = Quaternion.Slerp(startRot, endRot, curveValue);

            yield return null;
        }

        if (showLevelSelectAtEnd)
        {
            levelSelectPanel.SetActive(true);
        }
        else
        {
            mainMenuPanel.SetActive(true);
        }
    }
}
