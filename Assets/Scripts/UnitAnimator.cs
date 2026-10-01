using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UnitAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform bulletProjectilePrefab;
    [SerializeField] private Transform shootPointTransform;
    [SerializeField] private Transform magicSpawnPointTransform;
    [SerializeField] private Transform rifleTransform;
    [SerializeField] private Transform swordTransform;
    [SerializeField] private Transform fireballProjectilePrefab;

    private MoveAction moveAction;
    private ShootAction shootAction;
    private SwordAction swordAction;
    private ThrowFireballAction fireballAction;

    private void Awake()
    {
        if(TryGetComponent(out moveAction)){
            moveAction.OnStartMoving += moveAction_OnStartMoving;
            moveAction.OnStopMoving += moveAction_OnStopMoving;
        }

        if (TryGetComponent(out shootAction))
        {
            shootAction.OnShoot += ShootAction_OnShoot;
        }

        if (TryGetComponent(out swordAction))
        {
            swordAction.OnSwordActionStarted += swordAction_OnSwordActionStarted;
            swordAction.OnSwordActionCompleted += swordAction_OnSwordActionCompleted;
        }

        if (TryGetComponent(out fireballAction))
        {
            fireballAction.OnFireballActionStarted += FireballAction_OnFireballActionStarted;
            fireballAction.OnFireballActionCompleted += FireballAction_OnFireballActionCompleted;
            fireballAction.OnFireballSpawned += FireballAction_OnFireballSpawned;
        }
    }

    private void Start()
    {
        EquipRifle();
    }

    private void swordAction_OnSwordActionCompleted(object sender, EventArgs e)
    {
        EquipRifle();
    }

    private void swordAction_OnSwordActionStarted(object sender, EventArgs e)
    {
        EquipSword();
        animator.SetTrigger("SwordSlash");
    }

    private void moveAction_OnStartMoving(object sender, EventArgs e)
    {
        animator.SetBool("isWalking", true);
    }

    private void moveAction_OnStopMoving(object sender, EventArgs e)
    {
        animator.SetBool("isWalking", false);
    }

    private void ShootAction_OnShoot(object sender, ShootAction.OnShootEventArgs e)
    {
        animator.SetTrigger("Shoot");

        Transform bulletProjectileTransform = 
            Instantiate(bulletProjectilePrefab, shootPointTransform.position, Quaternion.identity);
        BulletProjectile bulletProjectile = bulletProjectileTransform.GetComponent<BulletProjectile>();

        Vector3 targetUnitShootAtPosition = e.targetUnit.GetWorldPosition();

        targetUnitShootAtPosition.y = shootPointTransform.position.y;

        bulletProjectile.Setup(targetUnitShootAtPosition);
    }

    private void FireballAction_OnFireballActionStarted(object sender, EventArgs e)
    {
        EquipEmptyHands();
        animator.SetTrigger("ThrowFireball");
    }

    private void FireballAction_OnFireballSpawned(object sender, ThrowFireballAction.OnFireballSpawnEventArgs e)
    {
        Transform fireballTransform = Instantiate(fireballProjectilePrefab, magicSpawnPointTransform.position, Quaternion.identity);

        FireballProjectile fireballProjectile = fireballTransform.GetComponent<FireballProjectile>();
        fireballProjectile.Setup(e.targetGridPosition,e.shootingUnit, () => { });
    }

    private void FireballAction_OnFireballActionCompleted(object sender, EventArgs e)
    {
        EquipRifle();
    }

    private void EquipEmptyHands()
    {
        swordTransform.gameObject.SetActive(false);
        rifleTransform.gameObject.SetActive(false);
    }

    private void EquipSword()
    {
        swordTransform.gameObject.SetActive(true);
        rifleTransform.gameObject.SetActive(false);
    }

    private void EquipRifle()
    {
        swordTransform.gameObject.SetActive(false);
        rifleTransform.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (moveAction != null)
        {
            moveAction.OnStartMoving -= moveAction_OnStartMoving;
            moveAction.OnStopMoving -= moveAction_OnStopMoving;
        }

        if (shootAction != null)
        {
            shootAction.OnShoot -= ShootAction_OnShoot;
        }

        if (swordAction != null)
        {
            swordAction.OnSwordActionStarted -= swordAction_OnSwordActionStarted;
            swordAction.OnSwordActionCompleted -= swordAction_OnSwordActionCompleted;
        }

        if (fireballAction != null)
        {
            fireballAction.OnFireballActionStarted -= FireballAction_OnFireballActionStarted;
            fireballAction.OnFireballActionCompleted -= FireballAction_OnFireballActionCompleted;
            fireballAction.OnFireballSpawned -= FireballAction_OnFireballSpawned;
        }
    }
}
