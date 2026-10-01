using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler OnDead;
    public event EventHandler<OnDamagedEventArgs> OnDamaged;

    public class OnDamagedEventArgs : EventArgs
    {
        public int damageAmount;
        public bool isCriticalHit;
        public bool isDodge;
    }

    [SerializeField] private int health = 100;
    private int healthMax;

    private void Awake()
    {
        healthMax = health;
    }

    public void Damage(int damageAmount, bool isCriticalHit = false, bool isDodge = false)
    {
        health -= damageAmount;

        if(health < 0)
        {
            health = 0;
        }

        OnDamagedEventArgs args = new OnDamagedEventArgs
        {
            damageAmount = damageAmount,
            isCriticalHit = isCriticalHit,
            isDodge = isDodge
        };
        OnDamaged?.Invoke(this, args);

        if(health ==0)
        {
            Die();
        }

        //Debug.Log(health);
    }

    private void Die()
    {
        OnDead?.Invoke(this, EventArgs.Empty);
    }

    public float GetHealthNormalized()
    {
        return (float) health / healthMax;
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetHealthMax()
    {
        return healthMax;
    }
}
