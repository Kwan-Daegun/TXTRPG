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
        gold.OnValueChanged      += OnGoldChanged;
        potions.OnValueChanged   += OnPotionsChanged;
    }

    public override void OnNetworkDespawn()
    {
        gameState.OnValueChanged -= OnGameStateChanged;
        gold.OnValueChanged      -= OnGoldChanged;
        potions.OnValueChanged   -= OnPotionsChanged;
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

    // ===== ROOM SYNC =====

    [ClientRpc]
    public void SyncRoomClientRpc(string roomName, string subtitle,
        string narrative, bool showCombat)
    {
        if (NetworkManager.Singleton.IsHost) return;
        if (DungeonUIManager.Instance == null) return;

        DungeonUIManager.Instance.SetRoomTitle(roomName, subtitle);
        DungeonUIManager.Instance.ShowNarrative(narrative);
        DungeonUIManager.Instance.HideNextRoomButton();

        if (showCombat)
            DungeonUIManager.Instance.ShowCombatPanel();
        else
            DungeonUIManager.Instance.HideCombatPanel();

        DungeonUIManager.Instance.RefreshHUD();
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
        if (string.IsNullOrEmpty(payload)) return;

        foreach (string entry in payload.Split('|'))
        {
            string[] parts = entry.Split(':');
            if (parts.Length < 4) continue;

            string name = parts[0];
            int hp      = int.Parse(parts[1]);
            int armor   = int.Parse(parts[2]);
            bool dead   = parts[3] == "1";

            var member = GameManager.Instance.party.Find(
                p => p.playerName == name);
            if (member == null) continue;

            member.currentHP    = hp;
            member.currentArmor = armor;
            member.isDead       = dead;
        }

        DungeonUIManager.Instance?.RefreshHUD();
    }

    // ===== COMBAT SYNC =====

    // Host calls this after every attack to push full combat state to clients
    // enemyPayload: "name:hp:maxhp:armor|name:hp:maxhp:armor"
    // partyPayload: same as SyncPartyHP — "name:hp:armor:dead"
    // combatLog: the latest combat message to display
    [ClientRpc]
    public void SyncCombatStateClientRpc(
        string enemyPayload, string partyPayload, string combatLog)
    {
        if (NetworkManager.Singleton.IsHost) return;
        if (DungeonUIManager.Instance == null) return;

        // Update party HP
        if (!string.IsNullOrEmpty(partyPayload))
        {
            foreach (string entry in partyPayload.Split('|'))
            {
                string[] parts = entry.Split(':');
                if (parts.Length < 4) continue;
                var member = GameManager.Instance.party.Find(
                    p => p.playerName == parts[0]);
                if (member == null) continue;
                member.currentHP    = int.Parse(parts[1]);
                member.currentArmor = int.Parse(parts[2]);
                member.isDead       = parts[3] == "1";
            }
        }

        // Update enemy data in CombatManager
        if (!string.IsNullOrEmpty(enemyPayload) &&
            CombatManager.Instance != null)
        {
            string[] enemies = enemyPayload.Split('|');
            for (int i = 0; i < enemies.Length &&
                 i < CombatManager.Instance.enemies.Count; i++)
            {
                string[] parts = enemies[i].Split(':');
                if (parts.Length < 4) continue;
                var e = CombatManager.Instance.enemies[i];
                e.currentHP    = int.Parse(parts[1]);
                e.currentArmor = int.Parse(parts[3]);
                e.isDead       = e.currentHP <= 0;
            }
        }

        // Show the combat log message
        if (!string.IsNullOrEmpty(combatLog))
            DungeonUIManager.Instance.LogCombat(combatLog);

        DungeonUIManager.Instance.RefreshCombatPanel();
        DungeonUIManager.Instance.RefreshHUD();
    }
}