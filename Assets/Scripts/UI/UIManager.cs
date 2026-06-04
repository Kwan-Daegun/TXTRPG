using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    public GameObject titleScreen;
    public GameObject lobbyScreen;
    public GameObject charSelectScreen;
    public GameObject dungeonScreen;
    public GameObject gameOverScreen;
    public GameObject victoryScreen;

    [Header("Lobby UI")]
    public TMP_InputField ipInputField;
    public TextMeshProUGUI lobbyStatusText;
    public TextMeshProUGUI localIPText;

    [Header("CharSelect UI")]
    public Transform partySlotsParent;
    public Transform classCardsParent;
    public Button beginAdventureButton;
    public GameObject classCardPrefab;
    public GameObject partySlotPrefab;

    [Header("Dungeon UI")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomSubtitleText;
    public Transform narrativeLogParent;
    public GameObject logEntryPrefab;
    public Transform partyHUDParent;
    public GameObject partyHUDCardPrefab;

    [Header("Inventory UI")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI potionsText;

    [Header("Action Area")]
    public GameObject actionArea;
    public GameObject nextRoomButton;
    public GameObject combatPanel;
    public GameObject shopPanel;

    [Header("Combat Panel")]
    public Transform cmPartyStatsParent;
    public Transform cmEnemyStatsParent;
    public Transform cmLogParent;
    public GameObject cmLogEntryPrefab;
    public Button cmAttackButton;
    public Button cmPotionButton;
    public Button cmNextButton;
    public TextMeshProUGUI combatTitleText;

    [Header("GameOver/Victory UI")]
    public TextMeshProUGUI gameOverStatsText;
    public TextMeshProUGUI victoryStatsText;

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


    public void ShowScreen(string screenName)
    {
        titleScreen.SetActive(false);
        lobbyScreen.SetActive(false);
        charSelectScreen.SetActive(false);
        dungeonScreen.SetActive(false);
        gameOverScreen.SetActive(false);
        victoryScreen.SetActive(false);

        switch (screenName)
        {
            case "Title": titleScreen.SetActive(true); break;
            case "Lobby": lobbyScreen.SetActive(true); break;
            case "CharSelect": charSelectScreen.SetActive(true); break;
            case "Dungeon": dungeonScreen.SetActive(true); break;
            case "GameOver": gameOverScreen.SetActive(true); break;
            case "Victory": victoryScreen.SetActive(true); break;
        }
    }

    //lobby
    public void SetLobbyStatus(string message)
    {
        if (lobbyStatusText != null)
            lobbyStatusText.text = message;
    }

    public void ShowLocalIP()
    {
        string ip = NetworkBootstrapper.Instance.GetLocalIP();
        if (localIPText != null)
            localIPText.text = $"Your IP: {ip}";
    }


    public void ShowNarrative(string message)
    {
        if (narrativeLogParent == null) return;
        var go = Instantiate(logEntryPrefab, narrativeLogParent);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = message;
    }

    public void LogTrap(string message)
    {
        ShowNarrative($"<color=#ff4444>[TRAP] {message}</color>");
    }

    public void LogLoot(string message)
    {
        ShowNarrative($"<color=#ffd700>[LOOT] {message}</color>");
    }

    public void LogShop(string message)
    {
        ShowNarrative($"<color=#88ff88>[SHOP] {message}</color>");
    }

    public void LogCombat(string message)
    {
        if (cmLogParent == null) return;
        var go = Instantiate(cmLogEntryPrefab, cmLogParent);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = message;
    }


    public void RefreshHUD()
    {
        if (goldText != null)
            goldText.text = GameManager.Instance.gold.ToString();
        if (potionsText != null)
            potionsText.text = GameManager.Instance.potions.ToString();

        RefreshPartyHUD();
    }

    public void RefreshPartyHUD()
    {
        if (partyHUDParent == null) return;


        foreach (Transform child in partyHUDParent)
            Destroy(child.gameObject);

        // Rebuild
        foreach (var member in GameManager.Instance.party)
        {
            var go = Instantiate(partyHUDCardPrefab, partyHUDParent);
            var card = go.GetComponent<PartyHUDCard>();
            if (card != null) card.Setup(member);
        }
    }

    //actions
    public void ShowNextRoomButton()
    {
        if (nextRoomButton != null)
            nextRoomButton.SetActive(true);
    }

    public void HideNextRoomButton()
    {
        if (nextRoomButton != null)
            nextRoomButton.SetActive(false);
    }

    public void ShowCombatPanel()
    {
        if (combatPanel != null)
            combatPanel.SetActive(true);
        RefreshCombatPanel();
    }

    public void HideCombatPanel()
    {
        if (combatPanel != null)
            combatPanel.SetActive(false);
    }

    public void ShowShopPanel()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    public void HideShopPanel()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }


    public void RefreshCombatPanel()
    {
        var enemy = CombatManager.Instance.CurrentEnemy;
        if (enemy == null) return;

        if (combatTitleText != null)
            combatTitleText.text = $"Combat! vs {enemy.enemyName}";


        foreach (Transform child in cmEnemyStatsParent)
            Destroy(child.gameObject);

        var enemyGO = Instantiate(cmLogEntryPrefab, cmEnemyStatsParent);
        var enemyTMP = enemyGO.GetComponentInChildren<TextMeshProUGUI>();
        if (enemyTMP != null)
            enemyTMP.text = $"{enemy.enemyName}\n" +
                           $"HP: {enemy.currentHP}\n" +
                           $"Armor: {enemy.currentArmor}";


        foreach (Transform child in cmPartyStatsParent)
            Destroy(child.gameObject);

        foreach (var member in GameManager.Instance.party)
        {
            var memberGO = Instantiate(cmLogEntryPrefab, cmPartyStatsParent);
            var memberTMP = memberGO.GetComponentInChildren<TextMeshProUGUI>();
            if (memberTMP != null)
                memberTMP.text = $"{member.playerName}\n" +
                                $"HP: {member.currentHP}\n" +
                                $"Armor: {member.currentArmor}";
        }
    }


    public void SetRoomTitle(string name, string subtitle)
    {
        if (roomNameText != null) roomNameText.text = name;
        if (roomSubtitleText != null) roomSubtitleText.text = subtitle;
    }


    public void ShowGameOverStats()
    {
        if (gameOverStatsText == null) return;
        string stats = "Fallen Heroes:\n";
        foreach (var member in GameManager.Instance.party)
            stats += $"{member.playerName} ({member.classTemplate.className})\n";
        stats += $"\nGold collected: {GameManager.Instance.gold}";
        gameOverStatsText.text = stats;
    }

    public void ShowVictoryStats()
    {
        if (victoryStatsText == null) return;
        string stats = "Surviving Heroes:\n";
        foreach (var member in GameManager.Instance.party)
            if (!member.isDead)
                stats += $"{member.playerName} ({member.classTemplate.className})\n";
        stats += $"\nTotal Gold: {GameManager.Instance.gold}";
        victoryStatsText.text = stats;
    }

}
