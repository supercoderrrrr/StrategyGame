using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRagdollSpawner : MonoBehaviour
{
    [SerializeField] private Transform ragdollPrefab;
    [SerializeField] private Transform originalRootBone;

    private HealthSystem healthSystem;

    private bool isKilledByExplosion = false;
    private Vector3 explosionSourcePos;
    private float explosionForceAmount;
    private float explosionRangeAmount;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();

        healthSystem.OnDead += HealthSystem_OnDead;
    }

    public void SetExplosionContext(Vector3 position, float force, float range)
    {
        isKilledByExplosion = true;
        explosionSourcePos = position;
        explosionForceAmount = force;
        explosionRangeAmount = range;
    }

    private void HealthSystem_OnDead(object sender, EventArgs e)
    {
        Transform ragdollTransform = Instantiate(ragdollPrefab, transform.position, transform.rotation);
        UnitRagdoll unitRagdoll = ragdollTransform.GetComponent<UnitRagdoll>();
        if (isKilledByExplosion)
        {
            unitRagdoll.Setup(originalRootBone, explosionSourcePos, explosionForceAmount, explosionRangeAmount);
        }
        else
        {
            unitRagdoll.Setup(originalRootBone);
        }

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDead -= HealthSystem_OnDead;
        }
    }
}
