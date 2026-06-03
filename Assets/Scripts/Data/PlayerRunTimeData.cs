// PlayerRunTimeData.cs
using UnityEngine;

[System.Serializable]
public class PlayerRunTimeData
{
    [Header("Reference")]
    public CharacterClass classTemplate;

    [Header("Runtime Stats")]
    public string playerName;
    public int currentHP;
    public int currentArmor;
    public bool isDead;

    [Header("Network")]
    public ulong ownerClientId;

    // Initialize from SO template
    public void Initialize(CharacterClass template, string name, ulong clientId)
    {
        classTemplate     = template;
        playerName        = name;
        currentHP         = template.baseHP;
        currentArmor      = template.baseArmor;
        isDead            = false;
        ownerClientId     = clientId;
    }

    // Combat helpers
    public bool IsDodge() => Random.Range(0, 100) < classTemplate.dodgeChance;
    public bool IsBlock() => Random.Range(0, 100) < classTemplate.blockChance;
    public bool IsCounter() => Random.Range(0, 100) < classTemplate.counterChance;
    public bool IsSpecialAttack() => Random.Range(0, 100) < 20;

    public int RollAttack()
    {
        return Random.Range(classTemplate.minAttack, classTemplate.maxAttack + 1);
    }

    public void TakeDamage(int damage)
    {
        if (currentArmor > 0)
        {
            // Armor absorbs damge hoho
            if (currentArmor >= damage)
            {
                // Reduce armor by 10%
                currentArmor = Mathf.Max(0, currentArmor - Mathf.CeilToInt(classTemplate.baseArmor * 0.1f));
                return;
            }
        }
        currentHP = Mathf.Max(0, currentHP - damage);
        if (currentHP <= 0) isDead = true;
    }
}