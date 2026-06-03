using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Current Combat")]
    public List<EnemyRuntimeData> enemies = new List<EnemyRuntimeData>();
    public int currentEnemyIndex = 0;

    // Combat log for UI
    public List<CombatResult> combatLog = new List<CombatResult>();

    // Events for UI
    public System.Action<CombatResult> OnCombatEvent;
    public System.Action OnCombatEnd;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    
    public void StartCombat(List<EnemyClass> enemySOs)
    {
        enemies.Clear();
        combatLog.Clear();
        currentEnemyIndex = 0;

        foreach (var so in enemySOs)
        {
            var enemy = new EnemyRuntimeData();
            enemy.Initialize(so);
            enemies.Add(enemy);
        }
    }

    public EnemyRuntimeData CurrentEnemy =>
        currentEnemyIndex < enemies.Count ? enemies[currentEnemyIndex] : null;

    
    public void PlayerAttack(PlayerRunTimeData attacker)
    {
        var enemy = CurrentEnemy;
        if (enemy == null || enemy.isDead) return;

        // chech special atk(20% lang)
        bool isSpecial = attacker.IsSpecialAttack();
        int rawDamage  = attacker.RollAttack();
        if (isSpecial) rawDamage *= 2;

        CombatResult result = new CombatResult
        {
            attackerName = attacker.playerName,
            defenderName = enemy.enemyName
        };

        if (isSpecial)
        {
            // special cannot be dodged/blocked/countered
            enemy.TakeDamage(rawDamage);
            result.eventType = CombatEventType.Special;
            result.damage    = rawDamage;
            result.message   = $"{attacker.playerName} unleashes a SPECIAL ATTACK on " +
                               $"{enemy.enemyName} for {rawDamage} damage!";
        }
        else
        {
            
            if (enemy.IsDodge())
            {
                result.eventType = CombatEventType.Dodge;
                result.damage    = 0;
                result.message   = $"{enemy.enemyName} dodged {attacker.playerName}'s attack!";
            }
            
            else if (enemy.IsBlock())
            {
                int blocked = rawDamage / 2;
                enemy.TakeDamage(blocked);
                result.eventType = CombatEventType.Block;
                result.damage    = blocked;
                result.message   = $"{enemy.enemyName} blocked! Only {blocked} damage taken.";
            }
            else
            {
                
                if (enemy.currentArmor >= rawDamage)
                {
                    enemy.TakeDamage(rawDamage);
                    result.eventType = CombatEventType.ArmorAbsorb;
                    result.damage    = 0;
                    result.message   = $"{enemy.enemyName}'s armor absorbed the attack!";
                }
                else
                {
                    enemy.TakeDamage(rawDamage);
                    result.eventType = CombatEventType.Hit;
                    result.damage    = rawDamage;
                    result.message   = $"{attacker.playerName} hits {enemy.enemyName} " +
                                      $"for {rawDamage} damage!";

                    
                    if (enemy.IsCounter())
                    {
                        attacker.TakeDamage(rawDamage);
                        var counter = new CombatResult
                        {
                            eventType    = CombatEventType.Counter,
                            attackerName = enemy.enemyName,
                            defenderName = attacker.playerName,
                            damage       = rawDamage,
                            message      = $"{enemy.enemyName} COUNTERS for {rawDamage} damage!"
                        };
                        LogResult(counter);
                    }
                }
            }
        }

        LogResult(result);

        
        if (enemy.isDead)
        {
            LogResult(new CombatResult
            {
                eventType = CombatEventType.EnemyDied,
                message   = $"{enemy.enemyName} has been defeated!",
                defenderName = enemy.enemyName
            });
            HandleEnemyDeath(enemy);
        }

        
        if (!enemy.isDead)
            EnemyAttack(enemy);

        GameManager.Instance.CheckPartyStatus();
    }

    
    private void EnemyAttack(EnemyRuntimeData enemy)
    {
        // enemy will oick a random living party member
        var living = GameManager.Instance.party.FindAll(p => !p.isDead);
        if (living.Count == 0) return;

        var target = living[Random.Range(0, living.Count)];

        bool isSpecial = enemy.IsSpecial();
        int rawDamage  = isSpecial ? enemy.RollSpecialAttack() : enemy.RollAttack();
        if (isSpecial && enemy.template.minSpecialAtk == 0) 
        {
            rawDamage = enemy.RollAttack() * 2;
        }

        CombatResult result = new CombatResult
        {
            attackerName = enemy.enemyName,
            defenderName = target.playerName
        };

        if (isSpecial)
        {
            target.TakeDamage(rawDamage);
            result.eventType = CombatEventType.Special;
            result.damage    = rawDamage;
            result.message   = $"{enemy.enemyName} uses SPECIAL ATTACK on " +
                               $"{target.playerName} for {rawDamage} damage!";
        }
        else
        {
            
            if (target.IsDodge())
            {
                result.eventType = CombatEventType.Dodge;
                result.damage    = 0;
                result.message   = $"{target.playerName} dodged {enemy.enemyName}'s attack!";
            }
            
            else if (target.IsBlock())
            {
                int blocked = rawDamage / 2;
                target.TakeDamage(blocked);
                result.eventType = CombatEventType.Block;
                result.damage    = blocked;
                result.message   = $"{target.playerName} blocked! Only {blocked} damage taken.";
            }
            else
            {
                target.TakeDamage(rawDamage);
                result.eventType = CombatEventType.Hit;
                result.damage    = rawDamage;
                result.message   = $"{enemy.enemyName} hits {target.playerName} " +
                                  $"for {rawDamage} damage!";

                
                if (target.IsCounter())
                {
                    enemy.TakeDamage(rawDamage);
                    var counter = new CombatResult
                    {
                        eventType    = CombatEventType.Counter,
                        attackerName = target.playerName,
                        defenderName = enemy.enemyName,
                        damage       = rawDamage,
                        message      = $"{target.playerName} COUNTERS for {rawDamage} damage!"
                    };
                    LogResult(counter);
                }
            }
        }

        LogResult(result);

        if (target.isDead)
        {
            LogResult(new CombatResult
            {
                eventType    = CombatEventType.PlayerDied,
                defenderName = target.playerName,
                message      = $"{target.playerName} has fallen!"
            });
        }
    }

    
    private void HandleEnemyDeath(EnemyRuntimeData enemy)
    {
        // Gold drop
        int gold = Random.Range(enemy.template.goldDropMin, enemy.template.goldDropMax + 1);
        GameManager.Instance.AddGold(gold);

        // Potion drop
        if (Random.Range(0, 100) < enemy.template.PotionDropChance)
            GameManager.Instance.AddPotion();

        // asdvance to next stupid enemy
        currentEnemyIndex++;
        if (currentEnemyIndex >= enemies.Count)
            OnCombatEnd?.Invoke();
    }

    // debug lang ini
    private void LogResult(CombatResult result)
    {
        combatLog.Add(result);
        OnCombatEvent?.Invoke(result);
    }

    public bool IsCombatOver()
    {
        return currentEnemyIndex >= enemies.Count;
    }
}