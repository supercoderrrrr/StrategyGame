using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class BaseAction : MonoBehaviour
{
    public static event EventHandler OnAnyActionStart;
    public static event EventHandler OnAnyActionCompleted;

    protected Unit unit;
    protected bool isActive;
    protected Action onActionComplete;

    public bool IsActive => isActive;

    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }

    protected virtual void OnDestroy()
    {
        if (isActive)
        {
            ActionComplete();
        }
    }

    public abstract string GetActionName();

    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositions = GetValidActionGridPositionList();
        return validGridPositions.Contains(gridPosition);
    }

    public abstract List<GridPosition> GetValidActionGridPositionList();

    public virtual int GetActionPointsCost()
    {
        return 1;
    }

    protected void ActionStart(Action onActionComplete)
    {
        isActive = true;
        this.onActionComplete = onActionComplete;

        OnAnyActionStart?.Invoke(this, EventArgs.Empty);
    }

    protected void ActionComplete()
    {
        isActive = false;
        Action actionComplete = onActionComplete;
        onActionComplete = null;
        actionComplete?.Invoke();

        OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetUnit()
    {
        return unit;
    }

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();
        EnemyAIAction bestAction = null;

        foreach(GridPosition gridPosition in validActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            if (enemyAIAction != null &&
                (bestAction == null || enemyAIAction.actionValue > bestAction.actionValue))
            {
                bestAction = enemyAIAction;
            }
        }

        return bestAction;
    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);
}
