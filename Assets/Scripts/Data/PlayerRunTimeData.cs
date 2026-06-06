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

    [Header("Cooldowns")]
    public int skillCooldownLeft;
    public int ultimateCooldownLeft;

    [Header("Status Effects")]
    public bool isShieldedNextHit;   // Ninja shadow strike
    public bool isFrozen;            // Mage frost nova
    public bool isSmokeBombed;       // Thief smoke bomb
    public bool isVined;

    // Initialize from SO template
    public void Initialize(CharacterClass template, string name, ulong clientId)
    {
        classTemplate = template;
        playerName = name;
        currentHP = template.baseHP;
        currentArmor = template.baseArmor;
        isDead = false;
        ownerClientId = clientId;
        skillCooldownLeft = 0;
        ultimateCooldownLeft = 0;
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
        // Smoke bomb — dodge all
        if (isSmokeBombed)
        {
            isSmokeBombed = false;
            return;
        }

        // Ninja shield
        if (isShieldedNextHit)
        {
            isShieldedNextHit = false;
            return;
        }

        int remainingDamage = damage - currentArmor;
        if (currentArmor > 0)
            currentArmor = Mathf.Max(0,
                currentArmor - Mathf.CeilToInt(
                    classTemplate.baseArmor * 0.1f));

        if (remainingDamage > 0)
        {
            currentHP = Mathf.Max(0, currentHP - remainingDamage);
            if (currentHP <= 0) isDead = true;
        }

    }
    public bool CanUseSkill() => skillCooldownLeft <= 0;
    public bool CanUseUltimate() => ultimateCooldownLeft <= 0;

    public void TickCooldowns()
    {
        if (skillCooldownLeft > 0) skillCooldownLeft--;
        if (ultimateCooldownLeft > 0) ultimateCooldownLeft--;
    }
}