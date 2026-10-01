using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeAction : BaseAction
{
    [SerializeField] private Transform grenadeProjectilePrefab;

    [SerializeField] private int maxThrowDistance = 7;
    [SerializeField] private LayerMask obstaclesLayerMask;
    [SerializeField] private int maxGrenadeCount = 2;

    private int currentGrenadeCount;
    private readonly List<GridPosition> validGridPositionList = new List<GridPosition>();

    protected override void Awake()
    {
        base.Awake();
        currentGrenadeCount = maxGrenadeCount;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }
    }

    public override string GetActionName()
    {
        return $"Grenade ({currentGrenadeCount})";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        const float damageRadius = 4f;
        Vector3 targetWorldPosition = LevelGrid.Instance.GetWorldPosition(gridPosition);
        int enemyTargets = 0;
        int friendlyTargets = 0;

        List<Unit> allUnits = UnitManager.Instance.GetUnitList();
        for (int i = 0; i < allUnits.Count; i++)
        {
            Unit target = allUnits[i];
            if (target == null || Vector3.Distance(target.GetWorldPosition(), targetWorldPosition) > damageRadius)
                continue;
            if (target.IsEnemy() == unit.IsEnemy()) friendlyTargets++;
            else enemyTargets++;
        }

        if (enemyTargets == 0) return null;
        int offenseValue = enemyTargets * 55;
        int survivalValue = friendlyTargets * -70;
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            offenseValue = offenseValue,
            survivalValue = survivalValue,
            actionValue = offenseValue + survivalValue,
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        return GetValidActionGridPositionList(unit.GetGridPosition());
    }

    public List<GridPosition> GetValidActionGridPositionList(GridPosition origin)
    {
        validGridPositionList.Clear();

        if(currentGrenadeCount <= 0)
        {
            return validGridPositionList;
        }

        for (int x = -maxThrowDistance; x <= maxThrowDistance; x++)
        {
            for (int z = -maxThrowDistance; z <= maxThrowDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = origin + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                int testDistance = Mathf.Abs(x) + Mathf.Abs(z);
                if (testDistance > maxThrowDistance)
                {
                    continue;
                }

                Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(origin);
                Vector3 targetWorldPosition = LevelGrid.Instance.GetWorldPosition(testGridPosition);

                Vector3 throwDir = (targetWorldPosition - unitWorldPosition).normalized;

                float unitShoulderHeight = 1.7f;
                float throwDistance = Vector3.Distance(unitWorldPosition, targetWorldPosition);

                if (Physics.Raycast(
                        unitWorldPosition + Vector3.up * unitShoulderHeight,
                        throwDir,
                        throwDistance,
                        obstaclesLayerMask))
                {
                    //Ray hit the wall, so block this path (and destination grid)
                    continue;
                }

                validGridPositionList.Add(testGridPosition);
            }
        }

        return validGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        currentGrenadeCount--;

        Transform grenadeProjectileTransform = Instantiate(grenadeProjectilePrefab, unit.GetWorldPosition(), Quaternion.identity);
        GrenadeProjectile grenadeProjectile = grenadeProjectileTransform.GetComponent<GrenadeProjectile>();
        grenadeProjectile.Setup(gridPosition, OnGrenadeBehaviourComplete);

        ActionStart(onActionComplete);
    }

    private void OnGrenadeBehaviourComplete()
    {
        ActionComplete();
    }
}
