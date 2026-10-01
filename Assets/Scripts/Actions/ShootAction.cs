using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShootAction : BaseAction
{
    public static event EventHandler<OnShootEventArgs> OnAnyShoot;

    public event EventHandler<OnShootEventArgs> OnShoot;

    public event EventHandler OnShootActionStarted;
    public event EventHandler OnShootActionCompleted;

    public class OnShootEventArgs: EventArgs
    {
        public Unit targetUnit;
        public Unit shootingUnit;
    }

    private enum State
    {
        Aiming,
        Shooting,
        Cooloff,
    }

    [SerializeField] private LayerMask obstaclesLayerMask;
    private State state;
    private int maxShootDistance = 7;
    private float stateTimer;
    private Unit targetUnit;
    private bool canShootBullet;
    private readonly List<GridPosition> validGridPositionList = new List<GridPosition>();

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Aiming:
                Vector3 aimDir = (targetUnit.GetWorldPosition() - unit.GetWorldPosition()).normalized;

                float rotateSpeed = 10f;
                transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                break;         
            case State.Shooting:
                if (canShootBullet)
                {
                    Shoot();
                    canShootBullet = false;
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
                if (stateTimer <= 0f)
                {
                    state = State.Shooting;
                    float shootingStateTime = 0.1f;
                    stateTimer = shootingStateTime;
                }
                break;
            case State.Shooting:
                if (stateTimer <= 0f)
                {
                    state = State.Cooloff;
                    float coolOffStateTime = 0.5f;
                    stateTimer = coolOffStateTime;
                }
                break;
            case State.Cooloff:
                OnShootActionCompleted?.Invoke(this, EventArgs.Empty);
                ActionComplete();
                break;
        }
    }

    public override string GetActionName()
    {
        return "Shoot";
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        return GetValidActionGridPositionList(unitGridPosition);
    }

    public List<GridPosition> GetValidActionGridPositionList(GridPosition unitGridPosition)
    {
        validGridPositionList.Clear();

        for (int x = -maxShootDistance; x <= maxShootDistance; x++)
        {
            for (int z = -maxShootDistance; z <= maxShootDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                int testDistance = Mathf.Abs(x) + Mathf.Abs(z);
                if(testDistance > maxShootDistance)
                {
                    continue;
                }

                if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                {
                    //Grid position empty
                    continue;
                }

                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);

                if (targetUnit.IsEnemy() == unit.IsEnemy())
                {
                    //Both Units are on the same team
                    continue;
                }

                if (targetUnit.IsHiddenInFog())
                {
                    continue;
                }

                Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition);
                Vector3 shootDir = (targetUnit.GetWorldPosition() - unitWorldPosition).normalized;

                float unitShoulderHeight = 1.7f;
                if(Physics.Raycast(
                        unitWorldPosition + Vector3.up * unitShoulderHeight,
                        shootDir,
                        Vector3.Distance(unitWorldPosition, targetUnit.GetWorldPosition()),
                        obstaclesLayerMask))
                {
                    //means blocked by the obstacles
                    continue;
                }

                validGridPositionList.Add(testGridPosition);
            }
        }

        return validGridPositionList;
    }

    private void Shoot()
    {
        OnAnyShoot?.Invoke(this, new OnShootEventArgs
        {
            targetUnit = targetUnit,
            shootingUnit = unit
        });

        OnShoot?.Invoke(this, new OnShootEventArgs
        {
            targetUnit = targetUnit,
            shootingUnit = unit
        });

        int baseDamage = 40;
        bool isCriticalHit = false;
        int dodgeChance = 0;
        int critChance = 10;

        bool targetOnGrass = PathFinding.Instance.IsTallGrassAtGridPosition(targetUnit.GetGridPosition());
        if (targetOnGrass)
        {
            dodgeChance += 80;
        }
        bool isDodge = UnityEngine.Random.Range(0, 100) < dodgeChance;
        if (isDodge)
        {
            targetUnit.Damage(0, false, true);
            return;
        }

        bool shooterOnGrass = PathFinding.Instance.IsTallGrassAtGridPosition(unit.GetGridPosition());
        if (shooterOnGrass)
        {
            critChance += 80;
        }

        bool isGuaranteedCrit = targetUnit.IsEnemy() && !targetUnit.IsEngaged();
        bool isRngCrit = UnityEngine.Random.Range(0, 100) < critChance;

        float shakeIntensity = 0.1f;

        if (isGuaranteedCrit || isRngCrit)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
            isCriticalHit = true;
            shakeIntensity = 1f;

            if (CritEffectManager.Instance != null)
            {
                CritEffectManager.Instance.StartHitStop();
            }
        }

        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(shakeIntensity);
        }

        targetUnit.Damage(baseDamage, isCriticalHit);
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

        //Critical hit effect
        if (targetUnit.IsEnemy() && !targetUnit.IsEngaged())
        {
            if (CritEffectManager.Instance != null)
            {
                CritEffectManager.Instance.StartDarkScreen();
            }
        }

        state = State.Aiming;
        float aimingStateTime = 1f;
        stateTimer = aimingStateTime;

        canShootBullet = true;

        ActionStart(onActionComplete);

        OnShootActionStarted?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetTargetUnit()
    {
        return targetUnit;
    }

    public int GetMaxShootDistance()
    {
        return maxShootDistance;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        int offenseValue = 100 + Mathf.RoundToInt((1 - targetUnit.GetHealthNormailized()) * 100f);

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            offenseValue = offenseValue,
            actionValue = offenseValue,
        };
    }

    public int GetTargetCountAtPosition(GridPosition gridPosition)
    {
        return GetValidActionGridPositionList(gridPosition).Count;
    }
}
