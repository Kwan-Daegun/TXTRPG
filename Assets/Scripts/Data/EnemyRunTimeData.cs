// EnemyRuntimeData.cs
using UnityEngine;

[System.Serializable]
public class EnemyRuntimeData
{
    public EnemyClass template;
    public string enemyName;
    public int currentHP;
    public int currentArmor;
    public bool isDead;
    public bool isFrozen; 
    public bool isVined;   

    public void Initialize(EnemyClass so)
    {
        template    = so;
        enemyName   = so.EnemyName;
        currentHP   = so.baseHP;
        currentArmor = so.baseArmor;
        isDead      = false;
    }

    public bool IsDodge()   => Random.Range(0, 100) < template.dodgeChance;
    public bool IsBlock()   => Random.Range(0, 100) < template.blockChance;
    public bool IsCounter() => Random.Range(0, 100) < template.counterChance;
    public bool IsSpecial() => Random.Range(0, 100) < 20;

    public int RollAttack()
    {
        return Random.Range(template.minAtk, template.maxAtk + 1);
    }

    public int RollSpecialAttack()
    {
        return Random.Range(template.minSpecialAtk, template.maxSpecialAtk + 1);
    }

    public void TakeDamage(int damage)
    {
        if (currentArmor > 0)
        {
            if (currentArmor >= damage)
            {
                currentArmor = Mathf.Max(0,
                    currentArmor - Mathf.CeilToInt(template.baseArmor * 0.1f));
                return;
            }
        }
        currentHP = Mathf.Max(0, currentHP - damage);
        if (currentHP <= 0) isDead = true;
    }
}