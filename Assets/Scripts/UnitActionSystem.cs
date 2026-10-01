using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using System;
using UnityEngine.EventSystems;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }

    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler OnSelectedActionChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionStarted;

    [SerializeField] private Unit selectedUnit;
    [SerializeField] private LayerMask unitLayerMask;

    private BaseAction selectedAction;
    private bool isBusy;
    public bool IsBusy => isBusy;

    private bool isGameActive = true;

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.LogError("There's more than one UnitActionSystem!" + transform + "-" + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetSelectedUnit(selectedUnit);

        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionVictory += MissionManager_OnGameEnded;
            MissionManager.Instance.OnMissionDefeat += MissionManager_OnGameEnded;
        }
    }

    private void MissionManager_OnGameEnded(object sender, EventArgs e)
    {
        isGameActive = false;

        //SetSelectedUnit(null); 
    }

    private void Update()
    {
        if (!isGameActive)
        {
            return;
        }
        if (isBusy)
        {
            return;
        }

        if (!TurnSystem.Instance.IsPlayerTurn())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (TryHandleUnitSelection())
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.LeftAlt))
        {
            SelectNextUnit();
        }

        HandleSelectedAction();
    }

    private void SelectNextUnit()
    {
        List<Unit> friendlyUnitList = UnitManager.Instance.GetFriendlyUnitList();

        if(friendlyUnitList.Count == 0)
        {
            SetSelectedUnit(null);
            return;
        }

        if (friendlyUnitList.Count == 1)
        {
            SetSelectedUnit(friendlyUnitList[0]);
            return;
        }

        int currentIndex = friendlyUnitList.IndexOf(selectedUnit);

        int nextIndex = (currentIndex + 1) % friendlyUnitList.Count;

        Unit nextUnit = friendlyUnitList[nextIndex];

        SetSelectedUnit(nextUnit);
    }

    public void HandleSelectedAction()
    {
        if (selectedUnit == null || selectedAction == null)
        {
            return;
        }

        if (InputManager.Instance.isMouseButtonDownThisFrame())
        {
            GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());

            if (!selectedAction.IsValidActionGridPosition(mouseGridPosition))
            {
                return;
            }

            if (!selectedUnit.TrySpendActionPointsToTakeAction(selectedAction))
            {
                return;
            }

            SetBusy();
            selectedAction.TakeAction(mouseGridPosition, ClearBusy);

            OnActionStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SetBusy()
    {
        isBusy = true;

        OnBusyChanged?.Invoke(this, isBusy);
    }

    private void ClearBusy()
    {
        isBusy = false;

        OnBusyChanged?.Invoke(this, isBusy);
    }

    private bool TryHandleUnitSelection()
    {
        if (InputManager.Instance.isMouseButtonDownThisFrame()) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, unitLayerMask))
            {
                if (raycastHit.transform.TryGetComponent<Unit>(out Unit unit))
                {
                    if (unit == selectedUnit)
                    {
                        //Unit already selected
                        return false;
                    }

                    if (unit.IsEnemy())
                    {
                        //If the player click on enemy
                        return false;
                    }

                    SetSelectedUnit(unit);
                    return true;
                }
            }
        }
        return false;
    }

    private void SetSelectedUnit(Unit unit)
    {
        selectedUnit = unit;
        SetSelectedAction(unit != null ? unit.GetAction<MoveAction>() : null);

        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        if (!(sender is Unit deadUnit) || deadUnit != selectedUnit)
        {
            return;
        }

        Unit nextUnit = null;
        if (UnitManager.Instance != null)
        {
            List<Unit> friendlyUnitList = UnitManager.Instance.GetFriendlyUnitList();
            if (friendlyUnitList.Count > 0)
            {
                nextUnit = friendlyUnitList[0];
            }
        }

        SetSelectedUnit(nextUnit);
    }

    public void SetSelectedAction(BaseAction baseAction)
    {
        selectedAction = baseAction;

        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetSelectedUnit()
    {
        return selectedUnit;
    }

    public BaseAction GetSelectedAction()
    {
        return selectedAction;
    }

    private void OnDestroy()
    {
        Unit.OnAnyUnitDead -= Unit_OnAnyUnitDead;

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionVictory -= MissionManager_OnGameEnded;
            MissionManager.Instance.OnMissionDefeat -= MissionManager_OnGameEnded;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
