using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Two-ply utility planner. It keeps only the strongest first-step candidates,
/// then expands those candidates to bound the cost of Physics-based evaluations.
/// </summary>
public sealed class TacticalPlanner
{
    private const int MaxBeamWidth = 12;
    private const int DefaultBeamWidth = 8;
    private const float DefaultFollowUpDiscount = .8f;
    private const int DefaultKillBonus = 80;

    private readonly TacticalWorldState worldState = new TacticalWorldState();
    private readonly TacticalPlanStep[] beam = new TacticalPlanStep[MaxBeamWidth];
    private int beamCount;

    public int LastCandidateEvaluationCount { get; private set; }
    public int BeamCount => beamCount;

    public bool TryBuildPlan(Unit unit, out TacticalPlan bestPlan)
    {
        bestPlan = default;
        beamCount = 0;
        LastCandidateEvaluationCount = 0;
        worldState.Capture(UnitManager.Instance.GetUnitList());

        TacticalAIProfile profile = unit.GetTacticalAIProfile();
        int beamWidth = profile == null ? DefaultBeamWidth : profile.BeamWidth;
        int availableActionPoints = unit.GetActionPoints();
        BaseAction[] actions = unit.GetBaseActionArray();

        for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
        {
            BaseAction action = actions[actionIndex];
            if (!IsSupported(action) || action.GetActionPointsCost() > availableActionPoints) continue;

            List<GridPosition> positions = GetCandidatePositions(action, unit.GetGridPosition());
            for (int positionIndex = 0; positionIndex < positions.Count; positionIndex++)
            {
                GridPosition position = positions[positionIndex];
                EnemyAIAction utility = EvaluateAt(action, unit.GetGridPosition(), position);
                if (utility == null) continue;

                TacticalPlanStep step = CreateStep(action, position, utility, profile, default, false);
                InsertIntoBeam(step, beamWidth);
                LastCandidateEvaluationCount++;
            }
        }

        if (beamCount == 0) return false;

        bestPlan.firstStep = beam[0];
        bestPlan.totalScore = beam[0].score;
        int bestTotalScore = bestPlan.totalScore;

        for (int beamIndex = 0; beamIndex < beamCount; beamIndex++)
        {
            TacticalPlanStep firstStep = beam[beamIndex];
            int remainingActionPoints = availableActionPoints - firstStep.action.GetActionPointsCost();
            if (remainingActionPoints <= 0) continue;

            GridPosition simulatedPosition = firstStep.action is MoveAction
                ? firstStep.gridPosition
                : unit.GetGridPosition();
            bool firstStepKillsTarget = WouldKillTarget(firstStep.action, firstStep.gridPosition);

            for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
            {
                BaseAction secondAction = actions[actionIndex];
                if (!IsSupported(secondAction) || secondAction.GetActionPointsCost() > remainingActionPoints) continue;

                List<GridPosition> positions = GetCandidatePositions(secondAction, simulatedPosition);
                for (int positionIndex = 0; positionIndex < positions.Count; positionIndex++)
                {
                    GridPosition position = positions[positionIndex];
                    if (firstStepKillsTarget && IsAttack(secondAction) && position == firstStep.gridPosition) continue;
                    if (firstStep.action is InteractAction && secondAction is InteractAction &&
                        position == firstStep.gridPosition) continue;

                    EnemyAIAction utility = EvaluateAt(secondAction, simulatedPosition, position);
                    if (utility == null) continue;

                    TacticalPlanStep secondStep = CreateStep(
                        secondAction, position, utility, profile, firstStep, firstStepKillsTarget);
                    float discount = profile == null ? DefaultFollowUpDiscount : profile.FollowUpDiscount;
                    int totalScore = firstStep.score + Mathf.RoundToInt(secondStep.score * discount);
                    LastCandidateEvaluationCount++;

                    if (totalScore > bestTotalScore)
                    {
                        bestTotalScore = totalScore;
                        bestPlan.firstStep = firstStep;
                        bestPlan.secondStep = secondStep;
                        bestPlan.hasSecondStep = true;
                        bestPlan.totalScore = totalScore;
                    }
                }
            }
        }

        return true;
    }

    public TacticalPlanStep GetBeamStep(int index)
    {
        if (index < 0 || index >= beamCount) throw new ArgumentOutOfRangeException(nameof(index));
        return beam[index];
    }

    private List<GridPosition> GetCandidatePositions(BaseAction action, GridPosition simulatedPosition)
    {
        if (action is MoveAction moveAction)
            return moveAction.GetValidActionGridPositionList(simulatedPosition);
        if (action is ShootAction shootAction)
            return shootAction.GetValidActionGridPositionList(simulatedPosition);
        if (action is SwordAction swordAction)
            return swordAction.GetValidActionGridPositionList(simulatedPosition);
        if (action is GrenadeAction grenadeAction)
            return grenadeAction.GetValidActionGridPositionList(simulatedPosition);
        if (action is InteractAction interactAction)
            return interactAction.GetValidActionGridPositionList(simulatedPosition);
        return action.GetValidActionGridPositionList();
    }

    private EnemyAIAction EvaluateAt(BaseAction action, GridPosition simulatedPosition, GridPosition targetPosition)
    {
        if (action is MoveAction moveAction)
            return moveAction.GetEnemyAIAction(simulatedPosition, targetPosition);
        return action.GetEnemyAIAction(targetPosition);
    }

    private TacticalPlanStep CreateStep(BaseAction action, GridPosition position, EnemyAIAction utility,
        TacticalAIProfile profile, TacticalPlanStep firstStep, bool firstStepKillsTarget)
    {
        int killBonus = 0;
        if (WouldKillTarget(action, position) &&
            !(firstStepKillsTarget && IsAttack(action) && position == firstStep.gridPosition))
        {
            killBonus = profile == null ? DefaultKillBonus : profile.KillBonus;
        }

        int baseScore = profile == null ? utility.actionValue : profile.Evaluate(utility);
        return new TacticalPlanStep
        {
            action = action,
            gridPosition = position,
            offenseValue = utility.offenseValue + killBonus,
            survivalValue = utility.survivalValue,
            positioningValue = utility.positioningValue,
            score = baseScore + killBonus,
        };
    }

    private bool WouldKillTarget(BaseAction action, GridPosition targetPosition)
    {
        int predictedDamage = GetPredictedDamage(action);
        if (predictedDamage <= 0) return false;

        TacticalWorldState.UnitState? target = worldState.GetUnitAt(targetPosition);
        return target.HasValue && target.Value.health <= predictedDamage &&
               target.Value.isEnemy != action.GetUnit().IsEnemy();
    }

    private static int GetPredictedDamage(BaseAction action)
    {
        if (action is ShootAction) return 40;
        if (action is SwordAction) return 100;
        return 0;
    }

    private static bool IsAttack(BaseAction action)
    {
        return action is ShootAction || action is SwordAction;
    }

    private static bool IsSupported(BaseAction action)
    {
        return action is MoveAction || action is ShootAction || action is SwordAction ||
               action is GrenadeAction || action is InteractAction;
    }

    private void InsertIntoBeam(TacticalPlanStep candidate, int requestedBeamWidth)
    {
        int beamWidth = Mathf.Clamp(requestedBeamWidth, 1, MaxBeamWidth);
        int insertionIndex = beamCount;
        for (int i = 0; i < beamCount; i++)
        {
            if (candidate.score > beam[i].score)
            {
                insertionIndex = i;
                break;
            }
        }

        if (insertionIndex >= beamWidth) return;
        int newCount = Mathf.Min(beamCount + 1, beamWidth);
        for (int i = newCount - 1; i > insertionIndex; i--)
            beam[i] = beam[i - 1];
        beam[insertionIndex] = candidate;
        beamCount = newCount;
    }
}
