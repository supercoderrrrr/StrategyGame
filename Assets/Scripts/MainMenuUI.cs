using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private GameObject gameTitleGameObject;

    [SerializeField] private GameObject unitSelectionPanel;

    [SerializeField] private GameObject levelSelectPanel; 
    [SerializeField] private Button level1Button; 
    [SerializeField] private Button backButton;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeSettingsButton;

    [SerializeField] private Transform mainCamera; 
    [SerializeField] private Transform startViewPoint; 
    [SerializeField] private Transform mapViewPoint;
    [SerializeField] private float transitionSpeed = 1.0f;
    [SerializeField] private AnimationCurve movementCurve;

    private void Awake()
    {
        playButton.onClick.AddListener(() => {
            StartCoroutine(MoveCamera(startViewPoint, mapViewPoint, true));
        });

        settingsButton.onClick.AddListener(() => {
            settingsPanel.SetActive(true);
        });

        quitButton.onClick.AddListener(() => {
            Debug.Log("Quit Game");
            Application.Quit();
        });

        closeSettingsButton.onClick.AddListener(() => {
            settingsPanel.SetActive(false);
        });

        level1Button.onClick.AddListener(() => {
            //SceneManager.LoadScene("Level1"); 
            SceneLoader.Instance.LoadScene("Level1");
        });

        backButton.onClick.AddListener(() => {
            StartCoroutine(MoveCamera(mapViewPoint, startViewPoint, false));
        });

        settingsPanel.SetActive(false);
        levelSelectPanel.SetActive(false);

        if (unitSelectionPanel != null) unitSelectionPanel.SetActive(true);

        if (mainCamera != null && startViewPoint != null)
        {
            mainCamera.position = startViewPoint.position;
            mainCamera.rotation = startViewPoint.rotation;
        }
    }

    private IEnumerator MoveCamera(Transform from, Transform to, bool showLevelSelectAtEnd)
    {
        playButton.interactable = false;
        levelSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);

        playButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);

        if (unitSelectionPanel != null)
        {
            unitSelectionPanel.SetActive(false);
        }

        if (gameTitleGameObject != null)
        {
            gameTitleGameObject.SetActive(false);
        }

        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * transitionSpeed;
            float value = movementCurve.Evaluate(timer); 

            mainCamera.position = Vector3.Lerp(from.position, to.position, value);
            mainCamera.rotation = Quaternion.Slerp(from.rotation, to.rotation, value);

            yield return null;
        }

        if (showLevelSelectAtEnd)
        {
            levelSelectPanel.SetActive(true);
            playButton.gameObject.SetActive(false);
            settingsButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(false);
        }
        else
        {
            playButton.interactable = true; 

            playButton.gameObject.SetActive(true);
            settingsButton.gameObject.SetActive(true);
            quitButton.gameObject.SetActive(true);

            if (unitSelectionPanel != null)
            {
                unitSelectionPanel.SetActive(true);
            }

            if (gameTitleGameObject != null)
            {
                gameTitleGameObject.SetActive(true);
            }

            levelSelectPanel.SetActive(false);
        }
    }
}