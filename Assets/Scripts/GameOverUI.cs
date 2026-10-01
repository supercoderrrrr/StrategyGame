using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject contentParent;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        MissionManager.Instance.OnMissionVictory += MissionManager_OnMissionVictory;
        MissionManager.Instance.OnMissionDefeat += MissionManager_OnMissionDefeat;

        restartButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        Hide();
    }

    private void MissionManager_OnMissionVictory(object sender, System.EventArgs e)
    {
        Show();
        resultText.text = "Mission Accomplished";
        resultText.color = Color.white;
    }

    private void MissionManager_OnMissionDefeat(object sender, System.EventArgs e)
    {
        Show();
        resultText.text = "Mission Failed";
        resultText.color = Color.white;
    }

    private void Show()
    {
        contentParent.SetActive(true);

        //Time.timeScale = 0f;

        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.enabled = false;
        }
    }

    private void Hide()
    {
        contentParent.SetActive(false);
    }

    private void OnDestroy()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionVictory -= MissionManager_OnMissionVictory;
            MissionManager.Instance.OnMissionDefeat -= MissionManager_OnMissionDefeat;
        }
    }
}