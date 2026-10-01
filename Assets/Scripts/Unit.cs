using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{

    public static event EventHandler OnAnyActionPointsChanged;
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;
    public event EventHandler OnAggroStatusChanged;

    [SerializeField] private bool isEnemy;
    [SerializeField] private bool isEngaged = false;
    [SerializeField] private int detectionRange = 5; //For enemy to detect player incoming
    [SerializeField] private int chainReactionRange = 7; //Chain reaction between enemies
    [SerializeField] private int maxActionPoints = 3;
    [SerializeField] private TacticalAIProfile tacticalAIProfile;

    [Header("Unit Info")]
    [SerializeField] private string unitName = "Standard Unit";
    [TextArea][SerializeField] private string unitDescription = "A unit that can shoot and move on the grid.";

    public bool IsEngaged() => isEngaged;

    private GridPosition gridPosition;
    private HealthSystem healthSystem;
    private BaseAction[] baseActionArray;
    private int actionPoints;
    private bool isHiddenInFog = false;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        BaseAction[] attachedActions = GetComponents<BaseAction>();
        int gameplayActionCount = 0;
        for (int i = 0; i < attachedActions.Length; i++)
        {
            if (attachedActions[i] is SpinAction)
            {
                attachedActions[i].enabled = false;
                continue;
            }

            gameplayActionCount++;
        }

        baseActionArray = new BaseAction[gameplayActionCount];
        int actionIndex = 0;
        for (int i = 0; i < attachedActions.Length; i++)
        {
            if (attachedActions[i] is SpinAction) continue;
            baseActionArray[actionIndex++] = attachedActions[i];
        }

        actionPoints = maxActionPoints;
    }

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);

        TurnSystem.Instance.onTurnChanged += TurnSystem_OnTurnChanged;

        healthSystem.OnDead += healthSystem_OnDead;

        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if(newGridPosition != gridPosition)
        {
            //unit changed grid position
            GridPosition oldGridPosition = gridPosition;

            gridPosition = newGridPosition;

            LevelGrid.Instance.UnitMoveGridPosition(this, oldGridPosition, newGridPosition);
        }

        if(isEnemy && !isEngaged)
        {
            CheckForPlayersNearby();
        }
    }

    public void SetIsHidden(bool isHidden)
    {
        this.isHiddenInFog = isHidden;
    }

    public bool IsHiddenInFog()
    {
        return isHiddenInFog;
    }

    public T GetAction<T>() where T: BaseAction
    {
        foreach(BaseAction baseAction in baseActionArray)
        {
            if(baseAction is T)
            {
                return (T)baseAction;
            }
        }
        return null;
    }


    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public BaseAction[] GetBaseActionArray()
    {
        return baseActionArray;
    }

    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (CanSpendActionPointsToTakeAction(baseAction))
        {
            SpendActionPoints(baseAction.GetActionPointsCost());
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CanSpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if(actionPoints >= baseAction.GetActionPointsCost())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SpendActionPoints(int amount)
    {
        actionPoints -= amount;

        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetActionPoints()
    {
        return actionPoints;
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if ((IsEnemy() && !TurnSystem.Instance.IsPlayerTurn()) || (!IsEnemy() && TurnSystem.Instance.IsPlayerTurn())) {
            actionPoints = maxActionPoints;

            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsEnemy()
    {
        return isEnemy;
    }

    public void Damage(int damageAmount, bool isCriticalHit = false, bool isDodge = false)
    {
        if(isHiddenInFog) return; 

        healthSystem.Damage(damageAmount, isCriticalHit, isDodge);

        if(isEnemy)
        {
            TriggerEngagement();
        }
    }

    private void healthSystem_OnDead(object sender, EventArgs e)
    {
        LevelGrid.Instance.RemoveUnitAtGridPosition(gridPosition, this);
        Destroy(gameObject);

        OnAnyUnitDead?.Invoke(this, EventArgs.Empty);
    }

    public float GetHealthNormailized()
    {
        return healthSystem.GetHealthNormalized();
    }

    public void TriggerEngagement()
    {
        if (isEngaged || !isEnemy) return;

        isEngaged = true;

        OnAggroStatusChanged?.Invoke(this, EventArgs.Empty);

        Debug.Log($"{gameObject.name} discovered player!");

        //Chain reaction: Noticing nearby teammates
        List<Unit> enemyList = UnitManager.Instance.GetEnemyUnitList();
        foreach (Unit enemy in enemyList)
        {
            if (enemy == this || enemy.IsEngaged()) continue;

            if (enemy.isHiddenInFog)
            {
                continue;
            }

            //grid distance
            int distance = GetGridDistance(this.gridPosition, enemy.GetGridPosition());

            if (distance <= chainReactionRange)
            {
                enemy.TriggerEngagement();
            }
        }
    }

    private int GetGridDistance(GridPosition posA, GridPosition posB)
    {
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.z - posB.z);
    }

    private void CheckForPlayersNearby()
    {
        List<Unit> playerUnits = UnitManager.Instance.GetFriendlyUnitList();
        foreach (Unit player in playerUnits)
        {
            float distance = Vector3.Distance(transform.position, player.GetWorldPosition());
            if (distance <= detectionRange)
            {
                TriggerEngagement();
                break;
            }
        }
    }

    public string GetUnitName() => unitName;
    public string GetUnitDescription() => unitDescription;
    public int GetHealth() => healthSystem.GetHealth();
    public int GetHealthMax() => healthSystem.GetHealthMax();
    public TacticalAIProfile GetTacticalAIProfile() => tacticalAIProfile;

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.onTurnChanged -= TurnSystem_OnTurnChanged;
        }

        if (healthSystem != null)
        {
            healthSystem.OnDead -= healthSystem_OnDead;
        }
    }
}
