using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombObject : MonoBehaviour, IInteractable
{
    public static event EventHandler OnAnyBombDefused;

    [SerializeField] private GameObject visualModel;
    [SerializeField] private ShockWaveEffect bombExplosionVisual;

    private GridPosition gridPosition;
    private bool isDefused = false;

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);

        LevelGrid.Instance.SetInteractableAtGridPosition(gridPosition, this); 
    }

    public void Interact(Action onInteractionComplete)
    {
        Defuse();

        onInteractionComplete();
    }

    public void Defuse()
    {
        if (isDefused) return;

        isDefused = true;

        OnAnyBombDefused?.Invoke(this, EventArgs.Empty);

        Debug.Log("Bomb defused!");
        if (visualModel != null) visualModel.GetComponent<MeshRenderer>().material.color = Color.green;
    }

    public void Explode()
    {
        if (isDefused) return;

        if (bombExplosionVisual != null)
        {
            bombExplosionVisual.TriggerExplosionVisual();
        }
        else
        {
            Debug.LogWarning("BombExplosionVisual is missing!");
        }

        Debug.Log("Explode£¡");
        if (visualModel != null) visualModel.SetActive(false);

        List<Unit> allUnits = new List<Unit>(UnitManager.Instance.GetUnitList());

        float hugeExplosionForce = 2000f;
        float hugeExplosionRange = 100f;

        foreach (Unit unit in allUnits)
        {
            if (unit == null || unit.GetHealthNormailized() <= 0) continue;

            if (unit.TryGetComponent<UnitRagdollSpawner>(out UnitRagdollSpawner spawner))
            {
                spawner.SetExplosionContext(transform.position, hugeExplosionForce, hugeExplosionRange);
            }

            unit.Damage(9999);
        }

        //Destroy all crate
        Collider[] colliderArray = Physics.OverlapSphere(transform.position, hugeExplosionRange);

        foreach (Collider collider in colliderArray)
        {
            if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate crate))
            {
                crate.Damage();
            }
        }
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }
}