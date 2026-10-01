using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefuseAction : BaseAction
{
    private float totalSpinAmount;
    private BombObject targetBomb;

    private void Update()
    {
        if (!isActive) return;

        float spinAddAmount = 360f * Time.deltaTime;
        transform.eulerAngles += new Vector3(0, spinAddAmount, 0);
        totalSpinAmount += spinAddAmount;

        if (totalSpinAmount >= 360f)
        {
            targetBomb?.Defuse();
            targetBomb = null;
            ActionComplete();
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetBomb = null;
        Collider[] colliderArray = Physics.OverlapSphere(LevelGrid.Instance.GetWorldPosition(gridPosition), 1f);
        foreach (Collider collider in colliderArray)
        {
            if (collider.TryGetComponent(out BombObject bombObject))
            {
                targetBomb = bombObject;
                break;
            }
        }

        totalSpinAmount = 0f;
        ActionStart(onActionComplete);
    }

    public override string GetActionName()
    {
        return "Defuse";
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) continue;


                if (testGridPosition == unitGridPosition) continue;

                Collider[] colliderArray = Physics.OverlapSphere(LevelGrid.Instance.GetWorldPosition(testGridPosition), 1f);
                bool hasBomb = false;
                foreach (Collider collider in colliderArray)
                {
                    if (collider.GetComponent<BombObject>())
                    {
                        hasBomb = true;
                        break;
                    }
                }

                if (hasBomb)
                {
                    validGridPositionList.Add(testGridPosition);
                }
            }
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction { gridPosition = gridPosition, actionValue = 0 };
    }
}
