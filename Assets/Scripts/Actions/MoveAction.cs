using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    [SerializeField] private int maxMoveDistance = 4;

    private List<Vector3> positionList;
    private readonly List<GridPosition> validGridPositionList = new List<GridPosition>();
    private readonly List<GridPosition> pathGridPositionList = new List<GridPosition>();
    private int currentPositionIndex;

    private void Update()
    {

        if (!isActive)
        {
            return;
        }

        Vector3 targetPosition = positionList[currentPositionIndex];
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        float rotateSpeed = 10f;
        transform.forward = Vector3.Lerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);

        float stoppingDistance = .1f;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            GridPosition currentGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);

            int terrainMultiplier = PathFinding.Instance.GetMoveCostMultiplierAtGridPosition(currentGridPosition);

            float baseMoveSpeed = 4f;
            float actualMoveSpeed = baseMoveSpeed;

            if (terrainMultiplier > 1)
            {
                actualMoveSpeed = baseMoveSpeed / terrainMultiplier;
            }

            transform.position += moveDirection * actualMoveSpeed * Time.deltaTime;
        }
        else
        {
            currentPositionIndex++;
            if(currentPositionIndex >= positionList.Count)
            {
                OnStopMoving?.Invoke(this, EventArgs.Empty);

                ActionComplete();
            }      
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        bool pathFound = PathFinding.Instance.TryBuildPath(
            unit.GetGridPosition(), gridPosition, maxMoveDistance * 10,
            pathGridPositionList, out _);

        if (!pathFound)
        {
            Debug.LogError($"MoveAction could not build a valid path to {gridPosition}.", this);
            onActionComplete?.Invoke();
            return;
        }

        currentPositionIndex = 0;
        if (positionList == null) positionList = new List<Vector3>(maxMoveDistance + 1);
        else positionList.Clear();

        foreach(GridPosition pathGridPosition in pathGridPositionList)
        {
            positionList.Add(LevelGrid.Instance.GetWorldPosition(pathGridPosition));
        }

        OnStartMoving?.Invoke(this, EventArgs.Empty);

        ActionStart(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        return GetValidActionGridPositionList(unit.GetGridPosition());
    }

    public List<GridPosition> GetValidActionGridPositionList(GridPosition origin)
    {
        PathFinding.Instance.GetReachableGridPositions(
            origin, maxMoveDistance * 10, validGridPositionList);

        return validGridPositionList;
    }

    public override string GetActionName()
    {
        return "Move";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return GetEnemyAIAction(unit.GetGridPosition(), gridPosition);
    }

    public EnemyAIAction GetEnemyAIAction(GridPosition origin, GridPosition gridPosition)
    {
        ShootAction shootAction = unit.GetAction<ShootAction>();
        int targetCountAtGridPosition = shootAction == null ? 0 : shootAction.GetTargetCountAtPosition(gridPosition);
        int opposingInfluence = PathFinding.Instance.GetOpposingInfluenceAtGridPosition(gridPosition, unit.IsEnemy());
        int friendlyInfluence = PathFinding.Instance.GetFriendlyInfluenceAtGridPosition(gridPosition, unit.IsEnemy());
        int terrainValue = PathFinding.Instance.IsTallGrassAtGridPosition(gridPosition) ? 18 : 0;
        int hazardValue = PathFinding.Instance.IsFireAtGridPosition(gridPosition) ? -120 : 0;
        int approachValue = GetApproachValue(gridPosition);
        int movementCost = PathFinding.Instance.GetReachableCost(origin, gridPosition, maxMoveDistance * 10);
        int objectiveValue = GetObjectiveValue(gridPosition);
        int offenseValue = targetCountAtGridPosition * 25;
        int survivalValue = -opposingInfluence / 10 + terrainValue + hazardValue;
        int positioningValue = friendlyInfluence / 12 + approachValue + objectiveValue - movementCost / 10;

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            offenseValue = offenseValue,
            survivalValue = survivalValue,
            positioningValue = positioningValue,
            actionValue = offenseValue + survivalValue + positioningValue,
        };
    }

    private int GetApproachValue(GridPosition candidate)
    {
        List<Unit> opponents = unit.IsEnemy()
            ? UnitManager.Instance.GetFriendlyUnitList()
            : UnitManager.Instance.GetEnemyUnitList();
        int nearestDistance = int.MaxValue;

        for (int i = 0; i < opponents.Count; i++)
        {
            Unit opponent = opponents[i];
            if (opponent == null || opponent.IsHiddenInFog()) continue;
            GridPosition opponentPosition = opponent.GetGridPosition();
            int distance = Mathf.Abs(candidate.x - opponentPosition.x) + Mathf.Abs(candidate.z - opponentPosition.z);
            nearestDistance = Mathf.Min(nearestDistance, distance);
        }

        return nearestDistance == int.MaxValue ? 0 : Mathf.Max(0, 12 - nearestDistance * 2);
    }

    private int GetObjectiveValue(GridPosition candidate)
    {
        if (!unit.IsEnemy() || MissionManager.Instance == null ||
            !MissionManager.Instance.TryGetObjectiveGridPosition(out GridPosition objective))
        {
            return 0;
        }

        int distance = Mathf.Abs(candidate.x - objective.x) + Mathf.Abs(candidate.z - objective.z);
        return Mathf.Max(0, 12 - distance);
    }
}
