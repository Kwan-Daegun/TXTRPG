// DungeonUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DungeonUIManager : MonoBehaviour
{
    public static DungeonUIManager Instance { get; private set; }

    [Header("Room UI")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomSubtitleText;

    [Header("Narrative")]
    public Transform narrativeLogParent;
    public GameObject logEntryPrefab;

    [Header("Party HUD")]
    public Transform partyHUDParent;
    public GameObject partyHUDCardPrefab;

    [Header("Inventory")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI potionsText;
    public TextMeshProUGUI revivePotionsText;

    [Header("Action Area")]
    public GameObject nextRoomButton;
    public GameObject combatPanel;
    public GameObject shopPanel;


    [Header("Combat Panel")]
    public TextMeshProUGUI cmPartyStatsText;  // ← add this
    public TextMeshProUGUI cmEnemyStatsText;  // ← add this
    public Transform cmLogParent;
    public GameObject cmLogEntryPrefab;
    public Button cmAttackButton;
    public Button cmPotionButton;

    public TextMeshProUGUI combatTitleText;

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
        // Wire proceed button
        if (nextRoomButton != null)
        {
            var btn = nextRoomButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() =>
                    GameManager.Instance.GoToNextRoom());
        }

        // Wire attack button
        if (cmAttackButton != null)
            cmAttackButton.onClick.AddListener(() =>
                CombatManager.Instance.PlayerAttackAll());

        // Wire potion button
        if (cmPotionButton != null)
            cmPotionButton.onClick.AddListener(() =>
                GameManager.Instance.UsePotion());

        if (cmPotionButton != null)
            cmPotionButton.onClick.AddListener(() =>
                GameManager.Instance.UsePotion());



        // Refresh HUD
        RefreshHUD();
    }

    // ===== ROOM =====
    public void SetRoomTitle(string name, string subtitle)
    {
        if (roomNameText != null) roomNameText.text = name;
        if (roomSubtitleText != null) roomSubtitleText.text = subtitle;
    }

    // ===== NARRATIVE =====
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

    // ===== HUD =====
    public void RefreshHUD()
    {
        if (goldText != null)
            goldText.text = GameManager.Instance.gold.ToString();
        if (potionsText != null)
            potionsText.text = GameManager.Instance.potions.ToString();
        if (revivePotionsText != null)
            revivePotionsText.text =
                $"Revive: {GameManager.Instance.revivePotions}";
        RefreshPartyHUD();
    }

    public void RefreshPartyHUD()
    {
        if (partyHUDParent == null) return;
        foreach (Transform child in partyHUDParent)
            Destroy(child.gameObject);
        foreach (var member in GameManager.Instance.party)
        {
            var go = Instantiate(partyHUDCardPrefab, partyHUDParent);
            var card = go.GetComponent<PartyHUDCard>();
            if (card != null) card.Setup(member);
        }
    }

    // ===== ACTION AREA =====
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

    // ===== COMBAT PANEL =====
    public void RefreshCombatPanel()
    {
        var enemy = CombatManager.Instance.CurrentEnemy;
        if (enemy == null) return;

        if (combatTitleText != null)
            combatTitleText.text = $"Combat! vs {enemy.enemyName}";

        // Enemy stats - single TMP text
        if (cmEnemyStatsText != null)
            cmEnemyStatsText.text =
                $"{enemy.enemyName}\n" +
                $"HP: {enemy.currentHP}/{enemy.template.baseHP}\n" +
                $"Armor: {enemy.currentArmor}";

        // Party stats - single TMP text
        string partyStats = "";
        foreach (var member in GameManager.Instance.party)
            partyStats +=
                $"{member.playerName}\n" +
                $"HP: {member.currentHP}/{member.classTemplate.baseHP}\n" +
                $"Armor: {member.currentArmor}\n\n";

        if (cmPartyStatsText != null)
            cmPartyStatsText.text = partyStats;
    }

}