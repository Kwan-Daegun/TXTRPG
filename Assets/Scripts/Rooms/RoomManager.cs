using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
        // Only the host drives room logic
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsHost)
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

    private void HandleEntranceHall()
    {
        DungeonUIManager.Instance.ShowNarrative(
            "You stand at the entrance of the War Goblin's Lair.\n" +
            "The stench of goblins fills the air.\n" +
            "Prepare yourselves, adventurers!"
        );
        DungeonUIManager.Instance.SetRoomTitle("Entrance Hall", "Room 1 of 6");
        DungeonUIManager.Instance.ShowNextRoomButton();

        // Tell clients: show entrance hall narrative, no combat
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncRoomClientRpc(
                "Entrance Hall", "Room 1 of 6",
                "You stand at the entrance of the War Goblin's Lair.\n" +
                "The stench of goblins fills the air.\n" +
                "Prepare yourselves, adventurers!",
                false);
    }

    private void HandleTrapRoom()
    {
        DungeonUIManager.Instance.SetRoomTitle("Trap Room", "Room 2 of 6");
        DungeonUIManager.Instance.ShowNarrative(
            "You hear a faint clicking sound...\n" +
            "Pressure plates are hidden beneath the stones!"
        );

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncRoomClientRpc(
                "Trap Room", "Room 2 of 6",
                "You hear a faint clicking sound...\n" +
                "Pressure plates are hidden beneath the stones!",
                false);

        foreach (var member in GameManager.Instance.party)
        {
            if (member.isDead) continue;

            if (Random.Range(0, 100) < 30)
            {
                if (member.IsDodge())
                {
                    string msg = $"{member.playerName} dodged the Spike Pit!";
                    DungeonUIManager.Instance.LogTrap(msg);
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        NetworkGameSync.Instance.SyncTrapLogClientRpc(msg);
                }
                else
                {
                    member.TakeDamage(20);
                    string msg = $"{member.playerName} triggered a Spike Pit! -20 HP";
                    DungeonUIManager.Instance.LogTrap(msg);
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        NetworkGameSync.Instance.SyncTrapLogClientRpc(msg);
                }
            }

            if (Random.Range(0, 100) < 25)
            {
                if (member.IsDodge())
                {
                    string msg = $"{member.playerName} dodged the Poison Dart!";
                    DungeonUIManager.Instance.LogTrap(msg);
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        NetworkGameSync.Instance.SyncTrapLogClientRpc(msg);
                }
                else
                {
                    member.TakeDamage(15);
                    string msg = $"{member.playerName} was hit by a Poison Dart! -15 HP";
                    DungeonUIManager.Instance.LogTrap(msg);
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        NetworkGameSync.Instance.SyncTrapLogClientRpc(msg);
                }
            }
        }

        GameManager.Instance.CheckPartyStatus();
        DungeonUIManager.Instance.RefreshHUD();

        // Sync final HP to clients
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncPartyHPClientRpc(BuildPartyHPPayload());

        DungeonUIManager.Instance.ShowNextRoomButton();
    }

    private void HandleTreasureChamber()
    {
        DungeonUIManager.Instance.HideNextRoomButton();
        DungeonUIManager.Instance.SetRoomTitle("Treasure Chamber", "Room 3 of 6");
        DungeonUIManager.Instance.ShowNarrative(
            "A massive pile of gold and gems lies ahead.\n" +
            "But a Guardian Golem blocks your path!"
        );

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncRoomClientRpc(
                "Treasure Chamber", "Room 3 of 6",
                "A massive pile of gold and gems lies ahead.\n" +
                "But a Guardian Golem blocks your path!",
                true);

        var enemies = new List<EnemyClass> { kingGolemSO };
        CombatManager.Instance.StartCombat(enemies);
        DungeonUIManager.Instance.ShowCombatPanel();
    }

    public void OnTreasureChamberCombatEnd()
    {
        GenerateTreasures();
        DungeonUIManager.Instance.HideCombatPanel();
        DungeonUIManager.Instance.ShowNextRoomButton();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncCombatEndClientRpc();
    }

    private void GenerateTreasures()
    {
        for (int i = 0; i < 3; i++)
        {
            int roll = Random.Range(0, 3);
            switch (roll)
            {
                case 0:
                    int gold = Random.Range(10, 101);
                    GameManager.Instance.AddGold(gold);
                    DungeonUIManager.Instance.LogLoot($"Found {gold} gold!");
                    break;
                case 1:
                    GameManager.Instance.AddPotion();
                    DungeonUIManager.Instance.LogLoot("Found a Health Potion!");
                    break;
                case 2:
                    DungeonUIManager.Instance.LogLoot(
                        "Found a weapon enhancement! +5 ATK");
                    break;
            }
        }
    }

    private void HandleGoblinBarracks()
    {
        DungeonUIManager.Instance.SetRoomTitle("Goblin Barracks", "Room 4 of 6");
        DungeonUIManager.Instance.ShowNarrative(
            "A horde of goblins lies in wait!\n" +
            "Armed with crude weapons, they charge!"
        );
        DungeonUIManager.Instance.HideNextRoomButton();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncRoomClientRpc(
                "Goblin Barracks", "Room 4 of 6",
                "A horde of goblins lies in wait!\n" +
                "Armed with crude weapons, they charge!",
                true);

        int goblinCount = Random.Range(4, 7);
        var enemies = new List<EnemyClass>();
        for (int i = 0; i < goblinCount; i++)
            enemies.Add(goblinMinionSO);
        CombatManager.Instance.StartCombat(enemies);
        DungeonUIManager.Instance.ShowCombatPanel();
    }

    private void HandleShop()
    {
        DungeonUIManager.Instance.SetRoomTitle("Goblin Shop", "Room 5 of 6");
        DungeonUIManager.Instance.ShowNarrative(
            "A shady goblin merchant eyes you suspiciously.\n" +
            "\"Buy something or get out!\""
        );

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncRoomClientRpc(
                "Goblin Shop", "Room 5 of 6",
                "A shady goblin merchant eyes you suspiciously.\n" +
                "\"Buy something or get out!\"",
                false);

        DungeonUIManager.Instance.ShowShopPanel();
    }

    public void BuyPotion()
    {
        if (GameManager.Instance.potions >= 5)
        {
            DungeonUIManager.Instance.LogShop("You can't carry more than 5 potions!");
            return;
        }
        if (GameManager.Instance.gold < 50)
        {
            DungeonUIManager.Instance.LogShop("Not enough gold! (50g required)");
            return;
        }
        GameManager.Instance.gold -= 50;
        GameManager.Instance.AddPotion();
        DungeonUIManager.Instance.LogShop("Bought a Health Potion for 50g!");
        DungeonUIManager.Instance.RefreshHUD();
    }

    public void RepairArmor(PlayerRunTimeData member)
    {
        if (member.currentArmor <= 0)
        {
            DungeonUIManager.Instance.LogShop(
                $"{member.playerName}'s armor is destroyed!");
            return;
        }
        if (GameManager.Instance.gold < 150)
        {
            DungeonUIManager.Instance.LogShop("Not enough gold! (150g required)");
            return;
        }
        GameManager.Instance.gold -= 150;
        member.currentArmor = member.classTemplate.baseArmor;
        DungeonUIManager.Instance.LogShop(
            $"{member.playerName}'s armor repaired for 150g!");
        DungeonUIManager.Instance.RefreshHUD();
    }

    public void RepairArmorAll()
    {
        foreach (var member in GameManager.Instance.party)
            RepairArmor(member);
    }

    public void BuyRevivePotion()
    {
        if (GameManager.Instance.revivePotions >= 3)
        {
            DungeonUIManager.Instance.LogShop("Can't carry more than 3 revive potions!");
            return;
        }
        if (GameManager.Instance.gold < 50)
        {
            DungeonUIManager.Instance.LogShop("Not enough gold! (100g required)");
            return;
        }
        GameManager.Instance.gold -= 50;
        GameManager.Instance.AddRevivePotion();
        DungeonUIManager.Instance.LogShop("Bought a Revive Potion for 100g!");
        DungeonUIManager.Instance.RefreshHUD();
    }

    private void HandleWarchiefThrone()
    {
        DungeonUIManager.Instance.SetRoomTitle(
            "Warchief's Throne", "Room 6 of 6 - BOSS");
        DungeonUIManager.Instance.ShowNarrative(
            "The Goblin Warchief rises from his throne!\n" +
            "\"You dare challenge ME?!\"\n" +
            "The final battle begins!"
        );
        DungeonUIManager.Instance.HideNextRoomButton();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.SyncRoomClientRpc(
                "Warchief's Throne", "Room 6 of 6 - BOSS",
                "The Goblin Warchief rises from his throne!\n" +
                "\"You dare challenge ME?!\"\n" +
                "The final battle begins!",
                true);

        var enemies = new List<EnemyClass> { goblinWarchiefSO };
        CombatManager.Instance.StartCombat(enemies);
        DungeonUIManager.Instance.ShowCombatPanel();
    }

    public void OnCombatEnded()
    {
        switch (GameManager.Instance.currentState)
        {
            case GameState.TreasureChamber:
                OnTreasureChamberCombatEnd();
                break;
            case GameState.GoblinBarracks:
                DungeonUIManager.Instance.ShowNarrative(
                    "The goblins are defeated! Loot their remains...");
                DungeonUIManager.Instance.ShowNextRoomButton();
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    NetworkGameSync.Instance.SyncCombatEndClientRpc();
                break;
            case GameState.WarchiefThrone:
                DungeonUIManager.Instance.ShowNarrative(
                    "The Goblin Warchief has been defeated!\nVICTORY!");
                GameManager.Instance.ChangeState(GameState.Victory);
                break;
        }
    }

    // Builds a pipe-separated payload of all party members' HP for syncing
    private string BuildPartyHPPayload()
    {
        var parts = new List<string>();
        foreach (var m in GameManager.Instance.party)
            parts.Add($"{m.playerName}:{m.currentHP}:{m.currentArmor}:{(m.isDead ? 1 : 0)}");
        return string.Join("|", parts);
    }
}