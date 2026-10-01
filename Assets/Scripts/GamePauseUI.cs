using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button mainMenuButton;

    private bool isPaused = false;

    private void Start()
    {
        resumeButton.onClick.AddListener(() =>
        {
            TogglePause();
        });

        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        pauseButton.onClick.AddListener(() =>
        {
            TogglePause();
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;

            //SceneManager.LoadScene("MainMenuScene");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene("MainMenuScene");
            }
            else
            {
                Debug.LogWarning("÷±Ω””≤º”‘ÿ");
                SceneManager.LoadScene("MainMenuScene");
            }
        });

        InputManager.Instance.OnPauseAction += InputManager_OnPauseAction;

        Hide();
    }

    private void InputManager_OnPauseAction(object sender, System.EventArgs e)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        pausePanel.SetActive(true);

        pauseButton.gameObject.SetActive(false);

        Time.timeScale = 0f;
    }

    private void Hide()
    {
        pausePanel.SetActive(false);

        pauseButton.gameObject.SetActive(true);

        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPauseAction -= InputManager_OnPauseAction;
        }
    }
}