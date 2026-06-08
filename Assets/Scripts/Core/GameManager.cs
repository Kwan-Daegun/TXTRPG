using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    public GameState currentState;

    [Header("Party Data")]
    public List<PlayerRunTimeData> party = new List<PlayerRunTimeData>();

    [Header("Inventory")]
    public int gold = 0;
    public int potions = 0;
    public int revivePotions = 0;

    public void AddRevivePotion() => revivePotions++;

    [Header("Room Tracking")]
    public int currentRoomIndex = 0;
    public string[] roomNames = new string[]
    {
        "Entrance Hall",
        "Trap Room",
        "Treasure Chamber",
        "Goblin Barracks",
        "Shop",
        "Warchief's Throne"
    };

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
   
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        OnStateChanged(newState);
    }

    private void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Title: LoadScene("TitleScene"); break;
            case GameState.Lobby: LoadScene("LobbyScene"); break;
            case GameState.CharacterSelect: LoadScene("CharSelectScene"); break;
            case GameState.EntranceHall:
            case GameState.TrapRoom:
            case GameState.TreasureChamber:
            case GameState.GoblinBarracks:
            case GameState.Shop:
            case GameState.WarchiefThrone: LoadScene("DungeonScene"); break;
            case GameState.GameOver: LoadScene("GameOverScene"); break;
            case GameState.Victory: LoadScene("VictoryScene"); break;
        }
    }

    // Centralized network-aware state change — always use this for game flow
    public void ChangeStateNetworked(GameState newState)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.ChangeGameStateServerRpc(newState);
        else if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            ChangeState(newState); // offline/solo
    }

    public void GoToNextRoom()
    {
        currentRoomIndex++;
        DungeonUIManager.Instance?.HideNextRoomButton();
        DungeonUIManager.Instance?.HideShopPanel();
        DungeonUIManager.Instance?.HideCombatPanel();

        switch (currentRoomIndex)
        {
            case 0: ChangeStateNetworked(GameState.EntranceHall); break;
            case 1: ChangeStateNetworked(GameState.TrapRoom); break;
            case 2: ChangeStateNetworked(GameState.TreasureChamber); break;
            case 3: ChangeStateNetworked(GameState.GoblinBarracks); break;
            case 4: ChangeStateNetworked(GameState.Shop); break;
            case 5: ChangeStateNetworked(GameState.WarchiefThrone); break;
            default: ChangeStateNetworked(GameState.Victory); break;
        }
    }

    public void StartDungeon()
    {
        currentRoomIndex = -1;
        GoToNextRoom();
    }

    public void ResetRunState()
    {
        party.Clear();
        currentRoomIndex = -1;
    }

    public void ReturnToLobby()
    {
        ResetRunState();
        ChangeStateNetworked(GameState.Lobby);
    }

    public void BackToMenu()
    {
        ResetRunState();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkBootstrapper.Instance != null)
            {
                NetworkBootstrapper.Instance.Disconnect();
                return;
            }
        }
        ChangeState(GameState.Title);
    }

    public bool IsPartyAlive()
    {
        foreach (var member in party)
            if (!member.isDead) return true;
        return false;
    }

    public void CheckPartyStatus()
    {
        if (!IsPartyAlive())
            ChangeStateNetworked(GameState.GameOver);
    }

    public void AddGold(int amount) => gold += amount;
    public void AddPotion() => potions++;

    // ===== POTION =====
    public void TryUsePotion()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient
            && !NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.UsePotionServerRpc();
        else
        {
            UsePotion();
            SyncPartyAfterItem();
        }
    }

    public bool UsePotion()
    {
        if (potions <= 0)
        {
            DungeonUIManager.Instance?.ShowNarrative("No potions left!");
            return false;
        }

        PlayerRunTimeData target = null;
        int lowestHP = int.MaxValue;
        foreach (var member in party)
        {
            if (!member.isDead && member.currentHP < lowestHP)
            {
                lowestHP = member.currentHP;
                target = member;
            }
        }

        if (target == null) return false;

        target.currentHP = Mathf.Min(
            target.classTemplate.baseHP,
            target.currentHP + 30);
        potions--;

        DungeonUIManager.Instance?.RefreshHUD();
        DungeonUIManager.Instance?.ShowNarrative(
            $"Used a potion! {target.playerName} restored 30 HP.");
        return true;
    }

    // ===== REVIVE POTION =====
    public void TryUseRevivePotion()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient
            && !NetworkManager.Singleton.IsServer)
            NetworkGameSync.Instance.UseRevivePotionServerRpc();
        else
        {
            UseRevivePotion();
            SyncPartyAfterItem();
        }
    }

    public bool UseRevivePotion()
    {
        if (revivePotions <= 0)
        {
            DungeonUIManager.Instance?.ShowNarrative("No revive potions left!");
            return false;
        }

        PlayerRunTimeData target = null;
        foreach (var member in party)
        {
            if (member.isDead) { target = member; break; }
        }

        if (target == null)
        {
            DungeonUIManager.Instance?.ShowNarrative("No fallen ally to revive!");
            return false;
        }

        target.isDead = false;
        target.currentHP = target.classTemplate.baseHP / 2;
        revivePotions--;

        DungeonUIManager.Instance?.ShowNarrative(
            $"{target.playerName} has been revived with {target.currentHP} HP!");
        DungeonUIManager.Instance?.RefreshHUD();
        return true;
    }

    public void SyncPartyAfterItem()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        var parts = new List<string>();
        foreach (var m in party)
            parts.Add(
                $"{m.playerName}:{m.currentHP}:{m.currentArmor}:{(m.isDead ? 1 : 0)}");
        string payload = string.Join("|", parts);

        NetworkGameSync.Instance.SyncPartyHPClientRpc(payload);
        NetworkGameSync.Instance.SyncInventoryClientRpc(potions, revivePotions, gold);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void Restart()
    {
        party.Clear();
        gold = 0;
        potions = 0;
        currentRoomIndex = -1;
        ChangeState(GameState.Title);
    }
}