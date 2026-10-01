using System;
using UnityEngine;

public class FireTile : MonoBehaviour
{
    [SerializeField] private int fireDamage = 10;//Burning damage for each turn
    [SerializeField] private float scaleSpeed = 5f; //For fire animations
    [SerializeField] private int durationTurns = 3;//Turns that fire lasts 
    [SerializeField] private ParticleSystem fireParticleSystem;
    [SerializeField] private ParticleSystem smokeParticleSystem;

    [SerializeField] private LayerMask waterLayerMask;
    [SerializeField] private LayerMask grassLayerMask;
    [SerializeField] private LayerMask visualGrassLayerMask;

    private GridPosition gridPosition;

    private Vector3 targetScale;
    private bool isExtinguishing = false;

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        transform.position = LevelGrid.Instance.GetWorldPosition(gridPosition);

        if (CheckElementalReactions())
        {
            return;
        }

        TurnSystem.Instance.onTurnChanged += TurnSystem_OnTurnChanged;

        transform.localScale = Vector3.zero;
        targetScale = Vector3.one;

        if (PathFinding.Instance != null)
        {
            PathFinding.Instance.SetIsFireAtGridPosition(gridPosition, true);
        }
    }

    private bool CheckElementalReactions()
    {
        float raycastHeight = 2f;
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeight;

        //Firetile destroyed by water
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit waterHit, raycastHeight * 2, waterLayerMask))
        {
            WaterReaction();
            return true;
        }

        //Grass tile destroyed by fire
        LayerMask combinedGrassMask = grassLayerMask | visualGrassLayerMask;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, raycastHeight * 2, combinedGrassMask);

        if (hits.Length > 0)
        {
            GrassReaction(hits);
        }

        return false;
    }

    private void WaterReaction()
    {
        isExtinguishing = true;

        if (fireParticleSystem != null)
        {
            fireParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (smokeParticleSystem != null) smokeParticleSystem.Play();

        transform.localScale = Vector3.zero;

        Destroy(gameObject, 2f);
    }

    private void GrassReaction(RaycastHit[] hits)
    {
        if (smokeParticleSystem != null) smokeParticleSystem.Play();

        foreach (RaycastHit hit in hits)
        {
            TileInfo grassTile = hit.collider.GetComponentInParent<TileInfo>();
            if (grassTile != null)
            {
                Destroy(grassTile.gameObject);
            }
            else
            {
                Destroy(hit.collider.gameObject);
            }
        }

        if (PathFinding.Instance != null)
        {
            PathFinding.Instance.SetIsTallGrassAtGridPosition(gridPosition, false);
        }
    }

    private void Update()
    {
        if (!isExtinguishing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if (!TurnSystem.Instance.IsPlayerTurn())
        {
            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition))
            {
                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
                targetUnit.Damage(fireDamage, false, false);
                Debug.Log($"{targetUnit.gameObject.name} is burnt and lost {fireDamage} health!");
            }

            durationTurns--;
            if (durationTurns <= 0)
            {
                Extinguish();
            }
        }
    }

    public void Extinguish()
    {
        if (isExtinguishing) return;

        isExtinguishing = true;

        if (fireParticleSystem != null)
        {
            fireParticleSystem.Stop();
        }

        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.onTurnChanged -= TurnSystem_OnTurnChanged;
        }

        Destroy(gameObject, 1.5f);
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.onTurnChanged -= TurnSystem_OnTurnChanged;
        }

        if (PathFinding.Instance != null)
        {
            PathFinding.Instance.SetIsFireAtGridPosition(gridPosition, false);
        }
    }
}
