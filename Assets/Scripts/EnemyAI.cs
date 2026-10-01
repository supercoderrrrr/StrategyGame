using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private bool logDecisions;
    [SerializeField] private bool drawDecisionDebug;

    private enum State
    {
        WaitingForEnemyTurn,
        TakingTurn,
        Busy
    }

    private State state;
    private float timer;
    private TacticalPlanner planner;
    private TacticalPlan lastPlan;
    private Unit lastPlannedUnit;
    private int totalDecisionCount;
    private int totalCandidateEvaluationCount;

    private void Awake()
    {
        state=State.WaitingForEnemyTurn;
        planner = new TacticalPlanner();
    }

    private void Start()
    {
        TurnSystem.Instance.onTurnChanged += TurnSystem_OnTurnChanged;
    }

    private void Update()
    {
        if(TurnSystem.Instance.IsPlayerTurn())
        {
            return;
        }

        switch (state)
        {
            case State.WaitingForEnemyTurn:
                break;
            case State.TakingTurn:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (TryTakeEnemyAIAction(SetStateTakingTurn))
                    {
                        state = State.Busy;
                    }
                    else
                    {
                        //No more enemies have actions that they can take, so end the turn
                        TurnSystem.Instance.NextTurn();
                    }
                }
                break;
            case State.Busy:
                break;
        }
    }

    private void SetStateTakingTurn()
    {
        timer = 0.5f;
        state = State.TakingTurn;
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if (!TurnSystem.Instance.IsPlayerTurn())
        {
            state = State.TakingTurn;
            timer = 2f;
        }
    }

    private bool TryTakeEnemyAIAction(Action onEnemyAIActionComplete)
    {
        foreach(Unit enemyUnit in UnitManager.Instance.GetEnemyUnitList())
        {
            if(!enemyUnit.IsEngaged())
            {
                continue;
            }

            if(enemyUnit.IsHiddenInFog())
            {
                continue;
            }

            if (TryTakeEnemyAIAction(enemyUnit, onEnemyAIActionComplete))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryTakeEnemyAIAction(Unit enemyUnit, Action onEnemyAIActionComplete)
    {
        if (!planner.TryBuildPlan(enemyUnit, out TacticalPlan plan)) return false;

        TacticalPlanStep step = plan.firstStep;
        if (!step.action.IsValidActionGridPosition(step.gridPosition))
        {
            return false;
        }

        if(enemyUnit.TrySpendActionPointsToTakeAction(step.action)) {
            lastPlan = plan;
            lastPlannedUnit = enemyUnit;
            totalDecisionCount++;
            totalCandidateEvaluationCount += planner.LastCandidateEvaluationCount;

            if (logDecisions)
            {
                Debug.Log($"AI {enemyUnit.name}: {plan.GetDescription()} " +
                          $"[evaluated={planner.LastCandidateEvaluationCount}]", enemyUnit);
            }
            step.action.TakeAction(step.gridPosition, onEnemyAIActionComplete);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDecisionDebug || !Application.isPlaying || planner == null || LevelGrid.Instance == null)
            return;

        int count = planner.BeamCount;
        for (int i = 0; i < count; i++)
        {
            TacticalPlanStep step = planner.GetBeamStep(i);
            float strength = count <= 1 ? 1f : 1f - (float)i / (count - 1);
            Gizmos.color = Color.Lerp(new Color(1f, .2f, .1f, .6f), new Color(.1f, 1f, .25f, .9f), strength);
            Vector3 worldPosition = LevelGrid.Instance.GetWorldPosition(step.gridPosition) + Vector3.up * .25f;
            Gizmos.DrawWireSphere(worldPosition, .25f + strength * .2f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPosition + Vector3.up * .3f, step.score.ToString());
#endif
        }

        if (lastPlannedUnit != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 start = lastPlannedUnit.GetWorldPosition() + Vector3.up;
            Vector3 end = LevelGrid.Instance.GetWorldPosition(lastPlan.firstStep.gridPosition) + Vector3.up;
            Gizmos.DrawLine(start, end);
        }
    }

    private void OnDestroy()
    {
        if (logDecisions && totalDecisionCount > 0)
        {
            Debug.Log($"AI planning summary: {totalDecisionCount} decisions, " +
                      $"{totalCandidateEvaluationCount / totalDecisionCount} average candidate evaluations.", this);
        }

        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.onTurnChanged -= TurnSystem_OnTurnChanged;
        }
    }
}
