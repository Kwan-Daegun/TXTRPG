using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NetworkGameSync : NetworkBehaviour
{
    public static NetworkGameSync Instance { get; private set; }

    public NetworkVariable<GameState> gameState =
        new NetworkVariable<GameState>(GameState.Title,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> gold =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> potions =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> roomIndex =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        gameState.OnValueChanged += OnGameStateChanged;
        gold.OnValueChanged += OnGoldChanged;
        potions.OnValueChanged += OnPotionsChanged;
    }

    public override void OnNetworkDespawn()
    {
        gameState.OnValueChanged -= OnGameStateChanged;
        gold.OnValueChanged -= OnGoldChanged;
        potions.OnValueChanged -= OnPotionsChanged;
    }

    private void OnGameStateChanged(GameState prev, GameState next)
    {
        GameManager.Instance.ChangeState(next);
    }

    private void OnGoldChanged(int prev, int next)
    {
        GameManager.Instance.gold = next;
    }

    private void OnPotionsChanged(int prev, int next)
    {
        GameManager.Instance.potions = next;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGoldServerRpc(int amount)
    {
        gold.Value += amount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePotionsServerRpc(int amount)
    {
        potions.Value += amount;
    }

    // ===== CHAR SELECT SYNC =====

    [ServerRpc(RequireOwnership = false)]
    public void SyncPartySlotsServerRpc(ulong senderClientId, string classNames,
        ServerRpcParams rpcParams = default)
    {
        if (CharSelectManager.Instance != null)
            CharSelectManager.Instance.ReceiveSubmission(senderClientId, classNames);
        SyncPartySlotsClientRpc(senderClientId, classNames);
    }

    [ClientRpc]
    public void SyncPartySlotsClientRpc(ulong senderClientId, string classNames)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId) return;
        if (CharSelectManager.Instance != null)
            CharSelectManager.Instance.ApplyRemoteSlots(senderClientId, classNames);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitSelectionServerRpc(string classNames,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (CharSelectManager.Instance != null)
            CharSelectManager.Instance.ReceiveSubmission(clientId, classNames);
    }

    // ===== PARTY ROSTER SYNC =====

    [ClientRpc]
    public void SyncPartyRosterClientRpc(string payload)
    {
        if (NetworkManager.Singleton.IsHost) return;
        if (string.IsNullOrEmpty(payload)) return;

        GameManager.Instance.party.Clear();

        foreach (string entry in payload.Split('|'))
        {
            string[] parts = entry.Split(',');
            if (parts.Length < 3) continue;

            ulong clientId = ulong.Parse(parts[0]);
            string className = parts[1];
            string playerName = parts[2];

            CharacterClass so = FindCharacterClass(className);
            if (so == null)
            {
                Debug.LogWarning($"[SyncPartyRoster] Could not find class: {className}");
                continue;
            }

            var data = new PlayerRunTimeData();
            data.Initialize(so, playerName, clientId);
            GameManager.Instance.party.Add(data);
        }

        if (DungeonUIManager.Instance != null)
            DungeonUIManager.Instance.RefreshHUD();
    }

    private CharacterClass FindCharacterClass(string className)
    {
        if (CharSelectManager.Instance == null) return null;
        foreach (var so in CharSelectManager.Instance.allClasses)
            if (so.className == className) return so;
        return null;
    }

    // ===== ROOM SYNC =====

    [ClientRpc]
    public void SyncRoomClientRpc(string roomName, string subtitle,
        string narrative, bool showCombat, string enemyRoster)
    {
        if (NetworkManager.Singleton.IsHost) return;
        if (DungeonUIManager.Instance == null) return;

        ApplyRoomSync(roomName, subtitle, narrative, showCombat, enemyRoster);
    }

    // Targeted version — sent to one specific client (used by RequestRoomSyncServerRpc)
    [ClientRpc]
    public void SyncRoomToClientRpc(string roomName, string subtitle,
        string narrative, bool showCombat, string enemyRoster,
        ClientRpcParams clientParams = default)
    {
        if (DungeonUIManager.Instance == null) return;
        ApplyRoomSync(roomName, subtitle, narrative, showCombat, enemyRoster);
    }

    private void ApplyRoomSync(string roomName, string subtitle,
        string narrative, bool showCombat, string enemyRoster)
    {
        DungeonUIManager.Instance.SetRoomTitle(roomName, subtitle);
        DungeonUIManager.Instance.ShowNarrative(narrative);
        DungeonUIManager.Instance.HideNextRoomButton();
        DungeonUIManager.Instance.HideCombatPanel();

        if (showCombat && !string.IsNullOrEmpty(enemyRoster))
        {
            var enemySOs = new List<EnemyClass>();
            foreach (string enemyName in enemyRoster.Split('|'))
            {
                var so = FindEnemySO(enemyName.Trim());
                if (so != null) enemySOs.Add(so);
            }

            if (enemySOs.Count > 0)
            {
                CombatManager.Instance.StartCombat(enemySOs);
                DungeonUIManager.Instance.ShowCombatPanel();
            }
        }

        DungeonUIManager.Instance.RefreshHUD();
    }

    // Client calls this when DungeonScene finishes loading
    [ServerRpc(RequireOwnership = false)]
    public void RequestRoomSyncServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (RoomManager.Instance == null) return;

        GameState state = GameManager.Instance.currentState;
        string roomName = "", subtitle = "", narrative = "";
        bool showCombat = false;
        string enemyRoster = "";

        switch (state)
        {
            case GameState.EntranceHall:
                roomName = "Entrance Hall";
                subtitle = "Room 1 of 6";
                narrative = "You stand at the entrance of the War Goblin's Lair.\n" +
                            "The stench of goblins fills the air.\n" +
                            "Prepare yourselves, adventurers!";
                break;

            case GameState.TrapRoom:
                roomName = "Trap Room";
                subtitle = "Room 2 of 6";
                narrative = "You hear a faint clicking sound...\n" +
                            "Pressure plates are hidden beneath the stones!";
                break;

            case GameState.TreasureChamber:
                roomName = "Treasure Chamber";
                subtitle = "Room 3 of 6";
                narrative = "A massive pile of gold and gems lies ahead.\n" +
                             "But a Guardian Golem blocks your path!";
                showCombat = true;
                enemyRoster = BuildCurrentEnemyRoster();
                break;

            case GameState.GoblinBarracks:
                roomName = "Goblin Barracks";
                subtitle = "Room 4 of 6";
                narrative = "A horde of goblins lies in wait!\n" +
                             "Armed with crude weapons, they charge!";
                showCombat = true;
                enemyRoster = BuildCurrentEnemyRoster();
                break;

            case GameState.Shop:
                roomName = "Goblin Shop";
                subtitle = "Room 5 of 6";
                narrative = "A shady goblin merchant eyes you suspiciously.\n" +
                            "\"Buy something or get out!\"";
                break;

            case GameState.WarchiefThrone:
                roomName = "Warchief's Throne";
                subtitle = "Room 6 of 6 - BOSS";
                narrative = "The Goblin Warchief rises from his throne!\n" +
                             "\"You dare challenge ME?!\"\n" +
                             "The final battle begins!";
                showCombat = true;
                enemyRoster = BuildCurrentEnemyRoster();
                break;

            default: return;
        }

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        SyncRoomToClientRpc(roomName, subtitle, narrative,
            showCombat, enemyRoster, clientParams);

        // Also sync party HP and inventory to this client
        var partyParts = new List<string>();
        foreach (var m in GameManager.Instance.party)
            partyParts.Add(
                $"{m.playerName}:{m.currentHP}:{m.currentArmor}:{(m.isDead ? 1 : 0)}");

        SyncPartyHPToClientRpc(string.Join("|", partyParts), clientParams);
        SyncInventoryToClientRpc(
            GameManager.Instance.potions,
            GameManager.Instance.revivePotions,
            GameManager.Instance.gold,
            clientParams);

        // If in combat, also push current enemy HP
        if (showCombat && CombatManager.Instance != null &&
            CombatManager.Instance.enemies.Count > 0)
        {
            var enemyParts = new List<string>();
            foreach (var e in CombatManager.Instance.enemies)
                enemyParts.Add(
                    $"{e.enemyName}:{e.currentHP}:{e.template.baseHP}" +
                    $":{e.currentArmor}:{(e.isDead ? 1 : 0)}");

            SyncCombatStateToClientRpc(
                string.Join("|", enemyParts),
                string.Join("|", partyParts),
                "", clientParams);
        }
    }

    private string BuildCurrentEnemyRoster()
    {
        if (CombatManager.Instance == null ||
            CombatManager.Instance.enemies.Count == 0)
            return "";
        var parts = new List<string>();
        foreach (var e in CombatManager.Instance.enemies)
            parts.Add(e.enemyName);
        return string.Join("|", parts);
    }

    private EnemyClass FindEnemySO(string enemyName)
    {
        if (RoomManager.Instance == null) return null;

        if (RoomManager.Instance.goblinMinionSO != null &&
            RoomManager.Instance.goblinMinionSO.EnemyName == enemyName)
            return RoomManager.Instance.goblinMinionSO;

        if (RoomManager.Instance.kingGolemSO != null &&
            RoomManager.Instance.kingGolemSO.EnemyName == enemyName)
            return RoomManager.Instance.kingGolemSO;

        if (RoomManager.Instance.goblinWarchiefSO != null &&
            RoomManager.Instance.goblinWarchiefSO.EnemyName == enemyName)
            return RoomManager.Instance.goblinWarchiefSO;

        return null;
    }

    [ClientRpc]
    public void SyncTrapLogClientRpc(string message)
    {
        if (NetworkManager.Singleton.IsHost) return;
        DungeonUIManager.Instance?.LogTrap(message);
        DungeonUIManager.Instance?.RefreshHUD();
    }

    [ClientRpc]
    public void SyncCombatEndClientRpc()
    {
        if (NetworkManager.Singleton.IsHost) return;
        DungeonUIManager.Instance?.HideCombatPanel();
        DungeonUIManager.Instance?.RefreshHUD();
    }

    [ClientRpc]
    public void SyncPartyHPClientRpc(string payload)
    {
        if (NetworkManager.Singleton.IsHost) return;
        ApplyPartyHP(payload);
    }

    // Targeted version
    [ClientRpc]
    public void SyncPartyHPToClientRpc(string payload,
        ClientRpcParams clientParams = default)
    {
        ApplyPartyHP(payload);
    }

    private void ApplyPartyHP(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return;
        foreach (string entry in payload.Split('|'))
        {
            string[] parts = entry.Split(':');
            if (parts.Length < 4) continue;
            var member = GameManager.Instance.party.Find(
                p => p.playerName == parts[0]);
            if (member == null) continue;
            member.currentHP = int.Parse(parts[1]);
            member.currentArmor = int.Parse(parts[2]);
            member.isDead = parts[3] == "1";
        }
        DungeonUIManager.Instance?.RefreshHUD();
        DungeonUIManager.Instance?.RefreshCombatPanel();
    }

    // ===== ITEM USE SYNC =====

    [ServerRpc(RequireOwnership = false)]
    public void UsePotionServerRpc()
    {
        GameManager.Instance.UsePotion();
        GameManager.Instance.SyncPartyAfterItem();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseRevivePotionServerRpc()
    {
        GameManager.Instance.UseRevivePotion();
        GameManager.Instance.SyncPartyAfterItem();
    }

    [ClientRpc]
    public void SyncInventoryClientRpc(int potions, int revivePotions, int gold)
    {
        if (NetworkManager.Singleton.IsHost) return;
        ApplyInventory(potions, revivePotions, gold);
    }

    // Targeted version
    [ClientRpc]
    public void SyncInventoryToClientRpc(int potions, int revivePotions,
        int gold, ClientRpcParams clientParams = default)
    {
        ApplyInventory(potions, revivePotions, gold);
    }

    private void ApplyInventory(int potions, int revivePotions, int gold)
    {
        GameManager.Instance.potions = potions;
        GameManager.Instance.revivePotions = revivePotions;
        GameManager.Instance.gold = gold;
        DungeonUIManager.Instance?.RefreshHUD();
    }

    // ===== COMBAT SYNC =====

    [ClientRpc]
    public void SyncCombatStateClientRpc(
        string enemyPayload, string partyPayload, string combatLog)
    {
        if (NetworkManager.Singleton.IsHost) return;
        ApplyCombatState(enemyPayload, partyPayload, combatLog);
    }

    // Targeted version
    [ClientRpc]
    public void SyncCombatStateToClientRpc(
        string enemyPayload, string partyPayload, string combatLog,
        ClientRpcParams clientParams = default)
    {
        ApplyCombatState(enemyPayload, partyPayload, combatLog);
    }

    private void ApplyCombatState(
        string enemyPayload, string partyPayload, string combatLog)
    {
        if (DungeonUIManager.Instance == null) return;

        if (!string.IsNullOrEmpty(partyPayload))
        {
            foreach (string entry in partyPayload.Split('|'))
            {
                string[] parts = entry.Split(':');
                if (parts.Length < 4) continue;
                var member = GameManager.Instance.party.Find(
                    p => p.playerName == parts[0]);
                if (member == null) continue;
                member.currentHP = int.Parse(parts[1]);
                member.currentArmor = int.Parse(parts[2]);
                member.isDead = parts[3] == "1";
            }
        }

        if (!string.IsNullOrEmpty(enemyPayload) &&
            CombatManager.Instance != null)
        {
            string[] enemyEntries = enemyPayload.Split('|');
            for (int i = 0;
                 i < enemyEntries.Length &&
                 i < CombatManager.Instance.enemies.Count; i++)
            {
                string[] parts = enemyEntries[i].Split(':');
                if (parts.Length < 5) continue;
                var e = CombatManager.Instance.enemies[i];
                e.currentHP = int.Parse(parts[1]);
                e.currentArmor = int.Parse(parts[3]);
                e.isDead = parts[4] == "1";
            }

            // Sync currentEnemyIndex
            CombatManager.Instance.currentEnemyIndex = 0;
            while (CombatManager.Instance.currentEnemyIndex <
                   CombatManager.Instance.enemies.Count &&
                   CombatManager.Instance.enemies[
                       CombatManager.Instance.currentEnemyIndex].isDead)
                CombatManager.Instance.currentEnemyIndex++;
        }

        if (!string.IsNullOrEmpty(combatLog))
            DungeonUIManager.Instance.LogCombat(combatLog);

        DungeonUIManager.Instance.RefreshCombatPanel();
        DungeonUIManager.Instance.RefreshHUD();
    }
    [ClientRpc]
    public void SyncCombatLogClientRpc(string message)
    {
        if (NetworkManager.Singleton.IsHost) return;
        DungeonUIManager.Instance?.LogCombat(message);
    }
}