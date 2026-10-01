using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public event EventHandler OnMissionVictory;
    public event EventHandler OnMissionDefeat;

    [SerializeField] private int maxBombTurns = 15;
    [SerializeField] private BombObject bomb;

    private bool isGameActive = true;
    private bool isDefeatPending;

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
        if (bomb == null)
        {
            bomb = FindObjectOfType<BombObject>();
        }

        TurnSystem.Instance.onTurnChanged += TurnSystem_OnTurnChanged;

        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;

        BombObject.OnAnyBombDefused += BombObject_OnAnyBombDefused;
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if (!isGameActive) return;

        //Every turn contains a player turn and a enemy turn, when roll back to player turn means a turn passed
        if (TurnSystem.Instance.IsPlayerTurn())
        {
            int currentTurn = TurnSystem.Instance.GetTurnNumber();

            if (currentTurn > maxBombTurns)
            {
                if (bomb != null)
                {
                    bomb.Explode();
                }

                ScheduleDefeat();
            }
        }
    }

    public int GetBombRemainingTurns()
    {
        int currentTurn = TurnSystem.Instance.GetTurnNumber();
        int remaining = maxBombTurns - currentTurn + 1;

        return Mathf.Max(0, remaining);
    }

    private void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        if (!isGameActive) return;

        //Examine if the player is all dead
        if (UnitManager.Instance.GetFriendlyUnitList().Count == 0)
        {
            ScheduleDefeat();
        }
    }

    private void BombObject_OnAnyBombDefused(object sender, EventArgs e)
    {
        if (!isGameActive) return;
        TriggerVictory();
    }

    private IEnumerator DelayedDefeat()
    {
        yield return new WaitForSeconds(4f);

        if (isGameActive)
        {
            TriggerDefeat();
        }
    }

    public bool TryGetObjectiveGridPosition(out GridPosition gridPosition)
    {
        if (bomb != null)
        {
            gridPosition = bomb.GetGridPosition();
            return true;
        }

        gridPosition = default;
        return false;
    }

    private void ScheduleDefeat()
    {
        if (!isGameActive || isDefeatPending)
        {
            return;
        }

        isDefeatPending = true;
        StartCoroutine(DelayedDefeat());
    }

    private void TriggerVictory()
    {
        isGameActive = false;
        Debug.Log("Success!");
        OnMissionVictory?.Invoke(this, EventArgs.Empty);
    }

    private void TriggerDefeat()
    {
        isGameActive = false;
        Debug.Log("Failed!");
        OnMissionDefeat?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.onTurnChanged -= TurnSystem_OnTurnChanged;
        }

        Unit.OnAnyUnitDead -= Unit_OnAnyUnitDead;
        BombObject.OnAnyBombDefused -= BombObject_OnAnyBombDefused;

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
