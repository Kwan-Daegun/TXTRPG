using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Enemy SOs")]
    public EnemyClass goblinMinionSO;
    public EnemyClass kingGolemSO;
    public EnemyClass goblinWarchiefSO;

    [Header("Treasure SOs")]
    public TreasureItemSO[] treasurePool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HandleCurrentRoom();
    }

    public void HandleCurrentRoom()
    {
        switch (GameManager.Instance.currentState)
        {
            case GameState.EntranceHall:    HandleEntranceHall();    break;
            case GameState.TrapRoom:        HandleTrapRoom();        break;
            case GameState.TreasureChamber: HandleTreasureChamber(); break;
            case GameState.GoblinBarracks:  HandleGoblinBarracks();  break;
            case GameState.Shop:            HandleShop();            break;
            case GameState.WarchiefThrone:  HandleWarchiefThrone();  break;
        }
    }

    //room 1 entrance hall
    private void HandleEntranceHall()
    {
        UIManager.Instance.ShowNarrative(
            "You stand at the entrance of the War Goblin's Lair.\n" +
            "The stench of goblins fills the air.\n" +
            "Prepare yourselves, adventurers!"
        );
        UIManager.Instance.ShowNextRoomButton();
    }

    //room 2 trap room
    private void HandleTrapRoom()
    {
        UIManager.Instance.ShowNarrative(
            "You hear a faint clicking sound...\n" +
            "Pressure plates are hidden beneath the stones!"
        );

        foreach (var member in GameManager.Instance.party)
        {
            if (member.isDead) continue;

            // spike Pit — 30% chance, 20 damage
            if (Random.Range(0, 100) < 30)
            {
                if (member.IsDodge())
                {
                    UIManager.Instance.LogTrap(
                        $"{member.playerName} dodged the Spike Pit!");
                }
                else
                {
                    member.TakeDamage(20);
                    UIManager.Instance.LogTrap(
                        $"{member.playerName} triggered a Spike Pit! -20 HP");
                }
            }

            // poison Dart — 25% chance, 15 damage
            if (Random.Range(0, 100) < 25)
            {
                if (member.IsDodge())
                {
                    UIManager.Instance.LogTrap(
                        $"{member.playerName} dodged the Poison Dart!");
                }
                else
                {
                    member.TakeDamage(15);
                    UIManager.Instance.LogTrap(
                        $"{member.playerName} was hit by a Poison Dart! -15 HP");
                }
            }
        }

        GameManager.Instance.CheckPartyStatus();
        UIManager.Instance.ShowNextRoomButton();
    }

    //room 3 treasure chamber
    private void HandleTreasureChamber()
    {
        UIManager.Instance.ShowNarrative(
            "A massive pile of gold and gems lies ahead.\n" +
            "But a Guardian Golem blocks your path!"
        );

        //start golem combat
        var enemies = new List<EnemyClass> { kingGolemSO };
        CombatManager.Instance.StartCombat(enemies);
        UIManager.Instance.ShowCombatPanel();
    }

    public void OnTreasureChamberCombatEnd()
    {
        //generate 3 random treasures
        GenerateTreasures();
        UIManager.Instance.ShowNextRoomButton();
    }

    private void GenerateTreasures()
    {
        for (int i = 0; i < 3; i++)
        {
            int roll = Random.Range(0, 3);

            switch (roll)
            {
                case 0: // Gold
                    int gold = Random.Range(10, 101);
                    GameManager.Instance.AddGold(gold);
                    UIManager.Instance.LogLoot($"Found {gold} gold!");
                    break;
                case 1: // Potion
                    GameManager.Instance.AddPotion();
                    UIManager.Instance.LogLoot("Found a Health Potion!");
                    break;
                case 2: // Weapon boost (temp attack buff)
                    UIManager.Instance.LogLoot("Found a weapon enhancement! +5 ATK");
                    break;
            }
        }
    }

    //room 4 goblin barracks
    private void HandleGoblinBarracks()
    {
       UIManager.Instance.ShowNarrative(
            "A horde of goblins lies in wait!\n" +
            "Armed with crude weapons, they charge!"
        );

        // 4-6 goblins
        int goblinCount = Random.Range(4, 7);
        var enemies     = new List<EnemyClass>();

        for (int i = 0; i < goblinCount; i++)
            enemies.Add(goblinMinionSO);

        CombatManager.Instance.StartCombat(enemies);
        UIManager.Instance.ShowCombatPanel();
    }

    //room 5 shop
    private void HandleShop()
    {
        UIManager.Instance.ShowNarrative(
            "A shady goblin merchant eyes you suspiciously.\n" +
            "\"Buy something or get out!\""
        );
         UIManager.Instance.ShowShopPanel();
    }

    public void BuyPotion()
    {
        if (GameManager.Instance.potions >= 5)
        {
            UIManager.Instance.LogShop("You can't carry more than 5 potions!");
            return;
        }
        if (GameManager.Instance.gold < 50)
        {
            UIManager.Instance.LogShop("Not enough gold! (50g required)");
            return;
        }
        GameManager.Instance.gold -= 50;
        GameManager.Instance.AddPotion();
        UIManager.Instance.LogShop("Bought a Health Potion for 50g!");
        UIManager.Instance.RefreshHUD();
    }

    public void RepairArmor(PlayerRunTimeData member)
    {
        if (member.currentArmor <= 0)
        {
            UIManager.Instance.LogShop(
                $"{member.playerName}'s armor is destroyed and cannot be repaired!");
            return;
        }
        if (GameManager.Instance.gold < 150)
        {
            UIManager.Instance.LogShop("Not enough gold! (150g required)");
            return;
        }
        GameManager.Instance.gold -= 150;
        member.currentArmor = member.classTemplate.baseArmor;
        UIManager.Instance.LogShop(
            $"{member.playerName}'s armor fully repaired for 150g!");
        UIManager.Instance.RefreshHUD();
    }

    // room 6 
    private void HandleWarchiefThrone()
    {
        UIManager.Instance.ShowNarrative(
            "The Goblin Warchief rises from his throne!\n" +
            "\"You dare challenge ME?!\"\n" +
            "The final battle begins!");

        var enemies = new List<EnemyClass> { goblinWarchiefSO };
        CombatManager.Instance.StartCombat(enemies);
        UIManager.Instance.ShowCombatPanel();
    }
}
