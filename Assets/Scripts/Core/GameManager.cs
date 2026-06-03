// GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //state management shit
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        OnStateChanged(newState);
    }

    private void OnStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Title:
                LoadScene("TitleScene");
                break;
            case GameState.Lobby:
                LoadScene("LobbyScene");
                break;
            case GameState.CharacterSelect:
                LoadScene("CharSelectScene");
                break;
            case GameState.EntranceHall:
            case GameState.TrapRoom:
            case GameState.TreasureChamber:
            case GameState.GoblinBarracks:
            case GameState.Shop:
            case GameState.WarchiefThrone:
                LoadScene("DungeonScene");
                break;
            case GameState.GameOver:
                LoadScene("GameOverScene");
                break;
            case GameState.Victory:
                LoadScene("VictoryScene");
                break;
        }
    }


    public void GoToNextRoom()
    {
        currentRoomIndex++;

        switch (currentRoomIndex)
        {
            case 0: ChangeState(GameState.EntranceHall); break;
            case 1: ChangeState(GameState.TrapRoom); break;
            case 2: ChangeState(GameState.TreasureChamber); break;
            case 3: ChangeState(GameState.GoblinBarracks); break;
            case 4: ChangeState(GameState.Shop); break;
            case 5: ChangeState(GameState.WarchiefThrone); break;
            default: ChangeState(GameState.Victory); break;
        }
    }

    public void StartDungeon()
    {
        currentRoomIndex = -1;
        GoToNextRoom();
    }


    public bool IsPartyAlive()
    {
        foreach (var member in party)
        {
            if (!member.isDead) return true;
        }
        return false;
    }

    public void CheckPartyStatus()
    {
        if (!IsPartyAlive())
            ChangeState(GameState.GameOver);
    }

    //inventry
    public void AddGold(int amount) => gold += amount;
    public void AddPotion() => potions++;

    public bool UsePotion()
    {
        if (potions <= 0) return false;

        // Heal lowest HP living member
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

        int healAmount = 30;
        target.currentHP = Mathf.Min(
            target.classTemplate.baseHP,
            target.currentHP + healAmount
        );
        potions--;
        return true;
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