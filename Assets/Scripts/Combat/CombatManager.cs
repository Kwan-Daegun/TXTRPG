using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
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
    public void NextEnemy()
    {
        if (IsCombatOver())
        {
            OnCombatEnd?.Invoke();
            DungeonUIManager.Instance.HideCombatPanel();
            RoomManager.Instance.OnCombatEnded();
        }
        else
        {
            DungeonUIManager.Instance.RefreshCombatPanel();
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
        int rawDamage = attacker.RollAttack();
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
            result.damage = rawDamage;
            result.message = $"{attacker.playerName} unleashes a SPECIAL ATTACK on " +
                               $"{enemy.enemyName} for {rawDamage} damage!";
        }
        else
        {

            if (enemy.IsDodge())
            {
                result.eventType = CombatEventType.Dodge;
                result.damage = 0;
                result.message = $"{enemy.enemyName} dodged {attacker.playerName}'s attack!";
            }

            else if (enemy.IsBlock())
            {
                int blocked = rawDamage / 2;
                enemy.TakeDamage(blocked);
                result.eventType = CombatEventType.Block;
                result.damage = blocked;
                result.message = $"{enemy.enemyName} blocked! Only {blocked} damage taken.";
            }
            else
            {

                if (enemy.currentArmor >= rawDamage)
                {
                    enemy.TakeDamage(rawDamage);
                    result.eventType = CombatEventType.ArmorAbsorb;
                    result.damage = 0;
                    result.message = $"{enemy.enemyName}'s armor absorbed the attack!";
                }
                else
                {
                    enemy.TakeDamage(rawDamage);
                    result.eventType = CombatEventType.Hit;
                    result.damage = rawDamage;
                    result.message = $"{attacker.playerName} hits {enemy.enemyName} " +
                                      $"for {rawDamage} damage!";


                    if (enemy.IsCounter())
                    {
                        attacker.TakeDamage(rawDamage);
                        var counter = new CombatResult
                        {
                            eventType = CombatEventType.Counter,
                            attackerName = enemy.enemyName,
                            defenderName = attacker.playerName,
                            damage = rawDamage,
                            message = $"{enemy.enemyName} COUNTERS for {rawDamage} damage!"
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
                message = $"{enemy.enemyName} has been defeated!",
                defenderName = enemy.enemyName
            });
            HandleEnemyDeath(enemy);
        }

        if (!IsCombatOver())
            EnemyTurnPhase();

        GameManager.Instance.CheckPartyStatus();
    }


    private void EnemyAttack(EnemyRuntimeData enemy)
    {
        // Check if enemy is frozen or vined
        if (enemy.isFrozen)
        {
            enemy.isFrozen = false;
            DungeonUIManager.Instance.LogCombat(
                $"{enemy.enemyName} is frozen and cannot attack!");
            return;
        }

        if (enemy.isVined)
        {
            enemy.isVined = false;
            DungeonUIManager.Instance.LogCombat(
                $"{enemy.enemyName} is vined and cannot attack!");
            return;
        }
        // enemy will oick a random living party member
        var living = GameManager.Instance.party.FindAll(p => !p.isDead);
        if (living.Count == 0) return;

        var target = living[Random.Range(0, living.Count)];

        bool isSpecial = enemy.IsSpecial();
        int rawDamage = isSpecial ? enemy.RollSpecialAttack() : enemy.RollAttack();
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
            result.damage = rawDamage;
            result.message = $"{enemy.enemyName} uses SPECIAL ATTACK on " +
                               $"{target.playerName} for {rawDamage} damage!";
        }
        else
        {

            if (target.IsDodge())
            {
                result.eventType = CombatEventType.Dodge;
                result.damage = 0;
                result.message = $"{target.playerName} dodged {enemy.enemyName}'s attack!";
            }

            else if (target.IsBlock())
            {
                int blocked = rawDamage / 2;
                target.TakeDamage(blocked);
                result.eventType = CombatEventType.Block;
                result.damage = blocked;
                result.message = $"{target.playerName} blocked! Only {blocked} damage taken.";
            }
            else
            {
                target.TakeDamage(rawDamage);
                result.eventType = CombatEventType.Hit;
                result.damage = rawDamage;
                result.message = $"{enemy.enemyName} hits {target.playerName} " +
                                  $"for {rawDamage} damage!";


                if (target.IsCounter())
                {
                    enemy.TakeDamage(rawDamage);
                    var counter = new CombatResult
                    {
                        eventType = CombatEventType.Counter,
                        attackerName = target.playerName,
                        defenderName = enemy.enemyName,
                        damage = rawDamage,
                        message = $"{target.playerName} COUNTERS for {rawDamage} damage!"
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
                eventType = CombatEventType.PlayerDied,
                defenderName = target.playerName,
                message = $"{target.playerName} has fallen!"
            });
        }
    }


    private void HandleEnemyDeath(EnemyRuntimeData enemy)
    {
        // Loot
        int gold = Random.Range(
            enemy.template.goldDropMin,
            enemy.template.goldDropMax + 1);
        GameManager.Instance.AddGold(gold);

        if (Random.Range(0, 100) < enemy.template.PotionDropChance)
            GameManager.Instance.AddPotion();

        DungeonUIManager.Instance.RefreshHUD();

        // Only advance the current enemy pointer when the active enemy died.
        if (CurrentEnemy == enemy)
        {
            currentEnemyIndex++;
            while (currentEnemyIndex < enemies.Count &&
                   enemies[currentEnemyIndex].isDead)
            {
                currentEnemyIndex++;
            }

            if (currentEnemyIndex >= enemies.Count)
            {
                // All enemies dead
                OnCombatEnd?.Invoke();
                DungeonUIManager.Instance.HideCombatPanel();
                RoomManager.Instance.OnCombatEnded();
                return;
            }

            // Auto advance to next enemy
            DungeonUIManager.Instance.LogCombat(
                $"Next enemy appears!");
            DungeonUIManager.Instance.RefreshCombatPanel();
        }
    }

    // debug lang ini
    private void LogResult(CombatResult result)
    {
        combatLog.Add(result);
        OnCombatEvent?.Invoke(result);
        DungeonUIManager.Instance.LogCombat(result.message);
        DungeonUIManager.Instance.RefreshCombatPanel();
    }


    public bool IsCombatOver()
    {
        return currentEnemyIndex >= enemies.Count;
    }
    // Any player clicks Attack → tells server → server runs combat → 
    // ClientRpc shows result on all screens

    public void PlayerAttackAll()
    {
        ulong requesterId = 0;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            requesterId = NetworkManager.Singleton.LocalClientId;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            RunAttackServer(requesterId);
        }
        else
        {
            RequestAttackServerRpc(requesterId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAttackServerRpc(ulong requesterId)
    {
        RunAttackServer(requesterId);
    }

    private void RunAttackServer(ulong requesterId)
    {
        foreach (var member in GameManager.Instance.party)
        {
            if (member.ownerClientId != requesterId) continue;
            if (member.isDead || CurrentEnemy == null || CurrentEnemy.isDead) continue;

            PlayerAttack(member);
            member.TickCooldowns();
        }

        string enemyStats = CurrentEnemy != null
            ? $"{CurrentEnemy.enemyName}|{CurrentEnemy.currentHP}|{CurrentEnemy.currentArmor}"
            : "dead";

        SyncCombatStateClientRpc(enemyStats);
    }

    [ClientRpc]
    private void SyncCombatStateClientRpc(string enemyData)
    {
        DungeonUIManager.Instance?.RefreshHUD();
        DungeonUIManager.Instance?.RefreshCombatPanel();
    }
    public void PlayerSkill(PlayerRunTimeData attacker)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            RunSkill(attacker.ownerClientId, attacker.classTemplate.className);
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            RequestSkillServerRpc(attacker.ownerClientId,
                attacker.classTemplate.className);
        }
        else
        {
            RunSkill(attacker.ownerClientId, attacker.classTemplate.className);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSkillServerRpc(ulong ownerClientId, string className)
    {
        RunSkill(ownerClientId, className);
    }

    private void RunSkill(ulong ownerClientId, string className)
    {
        var attacker = GameManager.Instance.party.Find(p =>
            p.ownerClientId == ownerClientId &&
            p.classTemplate.className == className &&
            !p.isDead);

        if (attacker == null)
            return;

        if (!attacker.CanUseSkill())
        {
            DungeonUIManager.Instance.LogCombat(
                $"{attacker.playerName}'s skill is on cooldown! " +
                $"({attacker.skillCooldownLeft} turns left)");
            return;
        }

        var enemy = CurrentEnemy;
        if (enemy == null || enemy.isDead) return;

        string log = "";

        switch (className)
        {
            case "Warrior": // Cleave — hits all enemies
                log = $"{attacker.playerName} uses CLEAVE!";
                foreach (var e in enemies)
                {
                    if (!e.isDead)
                    {
                        int dmg = attacker.RollAttack();
                        e.TakeDamage(dmg);
                        log += $"\n{e.enemyName} takes {dmg} damage!";
                        if (e.isDead) HandleEnemyDeath(e);
                    }
                }
                attacker.skillCooldownLeft =
                    attacker.classTemplate.skillCooldown;
                break;

            case "Mage": // Frost Nova — skip enemy turn
                log = $"{attacker.playerName} uses FROST NOVA!\n" +
                     $"{enemy.enemyName} is frozen!";
                enemy.isFrozen = true;
                int frostDmg = attacker.RollAttack();
                enemy.TakeDamage(frostDmg);
                log += $"\n{enemy.enemyName} takes {frostDmg} damage!";
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.skillCooldownLeft =
                    attacker.classTemplate.skillCooldown;
                break;

            case "Ranger": // Multi Shot — hits 2x
                log = $"{attacker.playerName} uses MULTI SHOT!";
                for (int i = 0; i < 2; i++)
                {
                    if (!enemy.isDead)
                    {
                        int dmg = attacker.RollAttack();
                        enemy.TakeDamage(dmg);
                        log += $"\nHit {i + 1}: {dmg} damage!";
                    }
                }
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.skillCooldownLeft =
                    attacker.classTemplate.skillCooldown;
                break;

            case "Ninja": // Shadow Strike — shield next hit
                log = $"{attacker.playerName} uses SHADOW STRIKE!\n" +
                     $"Next attack will be dodged!";
                attacker.isShieldedNextHit = true;
                int ninjaSkillDmg = attacker.RollAttack();
                enemy.TakeDamage(ninjaSkillDmg);
                log += $"\n{enemy.enemyName} takes {ninjaSkillDmg} damage!";
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.skillCooldownLeft =
                    attacker.classTemplate.skillCooldown;
                break;

            case "Thief": // Steal gold
                int stolenGold = Random.Range(10, 30);
                GameManager.Instance.AddGold(stolenGold);
                log = $"{attacker.playerName} uses STEAL!\n" +
                     $"Stole {stolenGold} gold!";
                attacker.skillCooldownLeft =
                    attacker.classTemplate.skillCooldown;
                break;

            case "Druid": // Nature Heal — heal all
                int healAmount = 20;
                log = $"{attacker.playerName} uses NATURE HEAL!";
                foreach (var member in GameManager.Instance.party)
                {
                    if (!member.isDead)
                    {
                        member.currentHP = Mathf.Min(
                            member.classTemplate.baseHP,
                            member.currentHP + healAmount);
                        log += $"\n{member.playerName} healed " +
                               $"{healAmount} HP!";
                    }
                }
                attacker.skillCooldownLeft =
                    attacker.classTemplate.skillCooldown;
                break;
        }

        DungeonUIManager.Instance.LogCombat(log);
        DungeonUIManager.Instance.RefreshCombatPanel();
        DungeonUIManager.Instance.RefreshHUD();

        if (!IsCombatOver()) EnemyTurnPhase();
        GameManager.Instance.CheckPartyStatus();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            string enemyStats = CurrentEnemy != null
                ? $"{CurrentEnemy.enemyName}|{CurrentEnemy.currentHP}|{CurrentEnemy.currentArmor}"
                : "dead";
            SyncCombatStateClientRpc(enemyStats);
        }
    }

    // ===== ULTIMATE =====

    public void PlayerUltimate(PlayerRunTimeData attacker)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            RunUltimate(attacker.ownerClientId, attacker.classTemplate.className);
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            RequestUltimateServerRpc(attacker.ownerClientId,
                attacker.classTemplate.className);
        }
        else
        {
            RunUltimate(attacker.ownerClientId, attacker.classTemplate.className);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestUltimateServerRpc(ulong ownerClientId, string className)
    {
        RunUltimate(ownerClientId, className);
    }

    private void RunUltimate(ulong ownerClientId, string className)
    {
        var attacker = GameManager.Instance.party.Find(p =>
            p.ownerClientId == ownerClientId &&
            p.classTemplate.className == className &&
            !p.isDead);

        if (attacker == null)
            return;

        if (!attacker.CanUseUltimate())
        {
            DungeonUIManager.Instance.LogCombat(
                $"{attacker.playerName}'s ultimate is on cooldown! " +
                $"({attacker.ultimateCooldownLeft} turns left)");
            return;
        }

        var enemy = CurrentEnemy;
        if (enemy == null || enemy.isDead) return;

        string log = "";

        switch (className)
        {
            case "Warrior": // Berserker Rage — ATK x3
                int berserkDmg = attacker.RollAttack() * 3;
                enemy.TakeDamage(berserkDmg);
                log = $"{attacker.playerName} uses BERSERKER RAGE!\n" +
                     $"{enemy.enemyName} takes {berserkDmg} damage!";
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.ultimateCooldownLeft =
                    attacker.classTemplate.ultimateCooldown;
                break;

            case "Mage": // Blizzard — massive AoE
                log = $"{attacker.playerName} uses BLIZZARD!";
                foreach (var e in enemies)
                {
                    if (!e.isDead)
                    {
                        int dmg = attacker.RollAttack() * 2;
                        e.TakeDamage(dmg);
                        log += $"\n{e.enemyName} takes {dmg} damage!";
                        if (e.isDead) HandleEnemyDeath(e);
                    }
                }
                attacker.ultimateCooldownLeft =
                    attacker.classTemplate.ultimateCooldown;
                break;

            case "Ranger": // Rain of Arrows — ATK x2.5
                int rainDmg = (int)(attacker.RollAttack() * 2.5f);
                enemy.TakeDamage(rainDmg);
                log = $"{attacker.playerName} uses RAIN OF ARROWS!\n" +
                     $"{enemy.enemyName} takes {rainDmg} damage!";
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.ultimateCooldownLeft =
                    attacker.classTemplate.ultimateCooldown;
                break;

            case "Ninja": // Assassinate — instant kill if HP < 30%
                float hpPercent = (float)enemy.currentHP /
                                  enemy.template.baseHP;
                if (hpPercent < 0.3f)
                {
                    enemy.currentHP = 0;
                    enemy.isDead = true;
                    log = $"{attacker.playerName} uses ASSASSINATE!\n" +
                         $"INSTANT KILL!";
                    HandleEnemyDeath(enemy);
                }
                else
                {
                    int ninjaUltDmg = attacker.RollAttack() * 2;
                    enemy.TakeDamage(ninjaUltDmg);
                    log = $"{attacker.playerName} uses ASSASSINATE!\n" +
                         $"Enemy HP too high! Deals {ninjaUltDmg} instead.";
                    if (enemy.isDead) HandleEnemyDeath(enemy);
                }
                attacker.ultimateCooldownLeft =
                    attacker.classTemplate.ultimateCooldown;
                break;

            case "Thief": // Smoke Bomb — dodge all attacks
                attacker.isSmokeBombed = true;
                int smokeDmg = attacker.RollAttack();
                enemy.TakeDamage(smokeDmg);
                log = $"{attacker.playerName} uses SMOKE BOMB!\n" +
                     $"Will dodge next attack!\n" +
                     $"{enemy.enemyName} takes {smokeDmg} damage!";
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.ultimateCooldownLeft =
                    attacker.classTemplate.ultimateCooldown;
                break;

            case "Druid": // Summon Vines — enemy can't attack
                enemy.isVined = true;
                int vineDmg = attacker.RollAttack();
                enemy.TakeDamage(vineDmg);
                log = $"{attacker.playerName} uses SUMMON VINES!\n" +
                     $"{enemy.enemyName} is vined! Can't attack!\n" +
                     $"Takes {vineDmg} damage!";
                if (enemy.isDead) HandleEnemyDeath(enemy);
                attacker.ultimateCooldownLeft =
                    attacker.classTemplate.ultimateCooldown;
                break;
        }

        DungeonUIManager.Instance.LogCombat(log);
        DungeonUIManager.Instance.RefreshCombatPanel();
        DungeonUIManager.Instance.RefreshHUD();

        if (!IsCombatOver()) EnemyTurnPhase();
        GameManager.Instance.CheckPartyStatus();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            string enemyStats = CurrentEnemy != null
                ? $"{CurrentEnemy.enemyName}|{CurrentEnemy.currentHP}|{CurrentEnemy.currentArmor}"
                : "dead";
            SyncCombatStateClientRpc(enemyStats);
        }
    }

    private void EnemyTurnPhase()
    {
        if (IsCombatOver()) return;

        var livingEnemies = enemies.FindAll(e => !e.isDead);
        foreach (var enemy in livingEnemies)
        {
            if (!GameManager.Instance.IsPartyAlive())
                break;

            if (enemy.isFrozen)
            {
                enemy.isFrozen = false;
                DungeonUIManager.Instance.LogCombat(
                    $"{enemy.enemyName} is frozen and cannot act!");
                continue;
            }

            if (enemy.isVined)
            {
                enemy.isVined = false;
                DungeonUIManager.Instance.LogCombat(
                    $"{enemy.enemyName} is entangled and cannot act!");
                continue;
            }

            var livingParty = GameManager.Instance.party.FindAll(p => !p.isDead);
            if (livingParty.Count == 0) break;

            var target = livingParty[Random.Range(0, livingParty.Count)];
            bool isSpecial = enemy.IsSpecial();
            int rawDamage = isSpecial ? enemy.RollSpecialAttack() : enemy.RollAttack();
            if (isSpecial && enemy.template.minSpecialAtk == 0)
                rawDamage = enemy.RollAttack() * 2;

            CombatResult result = new CombatResult
            {
                attackerName = enemy.enemyName,
                defenderName = target.playerName
            };

            if (isSpecial)
            {
                target.TakeDamage(rawDamage);
                result.eventType = CombatEventType.Special;
                result.damage = rawDamage;
                result.message = $"{enemy.enemyName} uses SPECIAL ATTACK on " +
                                 $"{target.playerName} for {rawDamage} damage!";
            }
            else if (target.IsDodge())
            {
                result.eventType = CombatEventType.Dodge;
                result.damage = 0;
                result.message = $"{target.playerName} dodged {enemy.enemyName}'s attack!";
            }
            else if (target.IsBlock())
            {
                int blocked = rawDamage / 2;
                target.TakeDamage(blocked);
                result.eventType = CombatEventType.Block;
                result.damage = blocked;
                result.message = $"{target.playerName} blocked! Only {blocked} damage taken.";
            }
            else
            {
                target.TakeDamage(rawDamage);
                result.eventType = CombatEventType.Hit;
                result.damage = rawDamage;
                result.message = $"{enemy.enemyName} hits {target.playerName} " +
                                 $"for {rawDamage} damage!";

                if (target.IsCounter())
                {
                    enemy.TakeDamage(rawDamage);
                    var counter = new CombatResult
                    {
                        eventType = CombatEventType.Counter,
                        attackerName = target.playerName,
                        defenderName = enemy.enemyName,
                        damage = rawDamage,
                        message = $"{target.playerName} COUNTERS for {rawDamage} damage!"
                    };
                    LogResult(counter);
                }
            }

            LogResult(result);
        }

        if (GameManager.Instance.IsPartyAlive() && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            string enemyStats = CurrentEnemy != null
                ? $"{CurrentEnemy.enemyName}|{CurrentEnemy.currentHP}|{CurrentEnemy.currentArmor}"
                : "dead";
            SyncCombatStateClientRpc(enemyStats);
        }
    }
}