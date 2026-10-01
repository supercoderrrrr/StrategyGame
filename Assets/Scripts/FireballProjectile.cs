using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [SerializeField] private Transform fireballExplodeVfxPrefab;
    [SerializeField] private Transform fireTilePrefab;

    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private LayerMask waterLayerMask;

    private GridPosition targetGridPosition;
    private Vector3 targetPosition;
    private Action onFireballBehaviourComplete;

    private float totalDistance;
    private Vector3 positionXZ;

    private bool isSetup = false;
    private bool isExploded = false;

    private Unit shootingUnit;

    public void Setup(GridPosition targetGridPosition,Unit shootingUnit, Action onFireballBehaviourComplete)
    {
        this.targetGridPosition = targetGridPosition;
        this.shootingUnit = shootingUnit;
        this.onFireballBehaviourComplete = onFireballBehaviourComplete;

        targetPosition = LevelGrid.Instance.GetWorldPosition(targetGridPosition);

        positionXZ = transform.position;
        positionXZ.y = 0;
        totalDistance = Vector3.Distance(positionXZ, new Vector3(targetPosition.x, 0, targetPosition.z));


        if (totalDistance == 0) totalDistance = 0.1f;

        isSetup = true;
    }

    private void Update()
    {
        if (!isSetup || isExploded) return;

        Vector3 targetPositionXZ = new Vector3(targetPosition.x, 0, targetPosition.z);
        Vector3 moveDir = (targetPositionXZ - positionXZ).normalized;

        float moveDistance = moveSpeed * Time.deltaTime;
        positionXZ += moveDir * moveDistance;

        float distance = Vector3.Distance(positionXZ, targetPositionXZ);
        float distanceNormalized = 1 - (distance / totalDistance);

        float maxHeight = totalDistance / 4f;
        float positionY = Mathf.Sin(distanceNormalized * Mathf.PI) * maxHeight;

        Vector3 nextPosition = new Vector3(positionXZ.x, positionY, positionXZ.z);

        Vector3 currentMoveDir = (nextPosition - transform.position).normalized;
        if (currentMoveDir != Vector3.zero)
        {
            transform.forward = currentMoveDir;
        }

        transform.position = nextPosition;

        if (Vector3.Distance(positionXZ, targetPositionXZ) < 0.2f)
        {
            isExploded = true;
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        if (fireballExplodeVfxPrefab != null)
        {
            Transform vfx = Instantiate(fireballExplodeVfxPrefab, targetPosition + Vector3.up * 1f, Quaternion.identity);
            Destroy(vfx.gameObject, 2f);
        }

        if (ScreenShake.Instance != null) ScreenShake.Instance.Shake();

        Vector3 centerWorldPos = LevelGrid.Instance.GetWorldPosition(targetGridPosition);
        bool isCenterWater = Physics.Raycast(centerWorldPos + Vector3.up * 2f, Vector3.down, 4f, waterLayerMask);

        List<GridPosition> aoeGridPositions = new List<GridPosition>();
        aoeGridPositions.Add(targetGridPosition);

        if (!isCenterWater)
        {
            aoeGridPositions.Add(targetGridPosition + new GridPosition(1, 0));
            aoeGridPositions.Add(targetGridPosition + new GridPosition(-1, 0));
            aoeGridPositions.Add(targetGridPosition + new GridPosition(0, 1));
            aoeGridPositions.Add(targetGridPosition + new GridPosition(0, -1));
        }

        foreach (GridPosition aoePos in aoeGridPositions)
        {
            if (!LevelGrid.Instance.IsValidGridPosition(aoePos)) continue;

            Vector3 aoeWorldPos = LevelGrid.Instance.GetWorldPosition(aoePos);

            if (!PathFinding.Instance.GetNode(aoePos.x, aoePos.z).IsFire())
            {
                Instantiate(fireTilePrefab, aoeWorldPos, Quaternion.identity);
            }
        }

        if (LevelGrid.Instance.HasAnyUnitOnGridPosition(targetGridPosition))
        {
            Unit centerUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetGridPosition);

            int impactDamage = 20;
            bool isCriticalHit = false;
            int critChance = 10;

            if (shootingUnit != null)
            {
                bool shooterOnGrass = PathFinding.Instance.IsTallGrassAtGridPosition(shootingUnit.GetGridPosition());
                if (shooterOnGrass)
                {
                    critChance += 80;
                }
            }

            bool isRngCrit = UnityEngine.Random.Range(0, 100) < critChance;
            bool isGuaranteedCrit = centerUnit.IsEnemy() && !centerUnit.IsEngaged();

            if (isGuaranteedCrit || isRngCrit)
            {
                impactDamage = Mathf.RoundToInt(impactDamage * 1.5f);
                isCriticalHit = true;
            }

            centerUnit.Damage(impactDamage, isCriticalHit, false);
        }

        yield return new WaitForSeconds(0.15f);

        foreach (GridPosition aoePos in aoeGridPositions)
        {
            if (!LevelGrid.Instance.IsValidGridPosition(aoePos)) continue;

            Vector3 aoeWorldPos = LevelGrid.Instance.GetWorldPosition(aoePos);

            bool isThisTileWater = Physics.Raycast(aoeWorldPos + Vector3.up * 2f, Vector3.down, 4f, waterLayerMask);

            if (!isThisTileWater && LevelGrid.Instance.HasAnyUnitOnGridPosition(aoePos))
            {
                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(aoePos);
                targetUnit.Damage(10, false, false);
            }
        }

        onFireballBehaviourComplete?.Invoke();
        Destroy(gameObject);
    }
}
