using System;
using System.Collections.Generic;
using UnityEngine;

public class ThrowFireballAction : BaseAction
{
    public event EventHandler OnFireballActionStarted;
    public event EventHandler OnFireballActionCompleted;

    public event EventHandler<OnFireballSpawnEventArgs> OnFireballSpawned;

    public class OnFireballSpawnEventArgs : EventArgs
    {
        public GridPosition targetGridPosition;
        public Unit shootingUnit;
    }

    private enum State
    {
        Aiming,
        Casting,
        Cooloff
    }

    [SerializeField] private int maxThrowDistance = 5;
    [SerializeField] private float spawnFireballDelay = 0.4f;

    private State state;
    private float stateTimer;
    private float spawnTimer;
    private GridPosition targetGridPosition;
    private bool canSpawnFireball;

    private void Update()
    {
        if (!isActive) return;

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Aiming:
                Vector3 targetWorldPosition = LevelGrid.Instance.GetWorldPosition(targetGridPosition);
                Vector3 aimDir = (targetWorldPosition - unit.GetWorldPosition()).normalized;
                aimDir.y = 0;

                float rotateSpeed = 10f;
                transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                break;

            case State.Casting:
                if (canSpawnFireball)
                {
                    spawnTimer -= Time.deltaTime;
                    if (spawnTimer <= 0f)
                    {
                        SpawnFireball();
                        canSpawnFireball = false;
                    }
                }
                break;

            case State.Cooloff:
                break;
        }

        if (stateTimer <= 0f)
        {
            NextState();
        }
    }

    private void NextState()
    {
        switch (state)
        {
            case State.Aiming:
                state = State.Casting;
                float castingStateTime = 1.0f;
                stateTimer = castingStateTime;
                spawnTimer = spawnFireballDelay;
                break;

            case State.Casting:
                state = State.Cooloff;
                float coolOffStateTime = 0.5f;
                stateTimer = coolOffStateTime;
                break;

            case State.Cooloff:
                OnFireballActionCompleted?.Invoke(this, EventArgs.Empty);
                ActionComplete();
                break;
        }
    }

    private void SpawnFireball()
    {
        OnFireballSpawned?.Invoke(this, new OnFireballSpawnEventArgs
        {
            targetGridPosition = targetGridPosition,
            shootingUnit = unit
        }) ;
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetGridPosition = gridPosition;

        state = State.Aiming;
        float aimingStateTime = 0.5f;
        stateTimer = aimingStateTime;
        canSpawnFireball = true;

        ActionStart(onActionComplete);

        OnFireballActionStarted?.Invoke(this, EventArgs.Empty);
    }

    public override string GetActionName()
    {
        return "Fireball";
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        if (unit.IsEnemy())
        {
            return validGridPositionList;
        }

        GridPosition unitGridPosition = unit.GetGridPosition();

        for (int x = -maxThrowDistance; x <= maxThrowDistance; x++)
        {
            for (int z = -maxThrowDistance; z <= maxThrowDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) continue;

                int testDistance = Mathf.Abs(x) + Mathf.Abs(z);
                if (testDistance > maxThrowDistance) continue;


                validGridPositionList.Add(testGridPosition);
            }
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0
        };
    }
}