using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TurnSystemUI : MonoBehaviour
{
    [SerializeField] private Button endTurnBtn;
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private TextMeshProUGUI bombTimerText;
    [SerializeField] private GameObject enemyTurnVisualGameObject;

    private void Start()
    {
        endTurnBtn.onClick.AddListener(() =>
        {
          TurnSystem.Instance.NextTurn();
        });

        TurnSystem.Instance.onTurnChanged += TurnSystem_OnTurnChanged;

        UpdateTurnText();
        UpdateEnemyTurnVisual();
        UpdateEndTurnButtonVisibility();
    }

    private void Update()
    {
        if (TurnSystem.Instance.IsPlayerTurn())
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TurnSystem.Instance.NextTurn();
            }
        }
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateTurnText();
        UpdateEnemyTurnVisual();
        UpdateEndTurnButtonVisibility();
    }

    private void UpdateTurnText()
    {
        turnNumberText.text = "TURN " + TurnSystem.Instance.GetTurnNumber();
    }

    private void UpdateEnemyTurnVisual()
    {
        enemyTurnVisualGameObject.SetActive(!TurnSystem.Instance.IsPlayerTurn());

        if (MissionManager.Instance != null)
        {
            int remainingTurns = MissionManager.Instance.GetBombRemainingTurns();

            //Bomb count down
            bombTimerText.text = $"BOMB explodes in {remainingTurns} turns!";

            if (remainingTurns <= 3)
            {
                bombTimerText.color = Color.red;
            }
            else
            {
                bombTimerText.color = Color.white;
            }
        }
    }

    private void UpdateEndTurnButtonVisibility()
    {
        endTurnBtn.gameObject.SetActive(TurnSystem.Instance.IsPlayerTurn());
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.onTurnChanged -= TurnSystem_OnTurnChanged;
        }
    }
}
