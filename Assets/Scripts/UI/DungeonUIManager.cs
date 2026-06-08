// DungeonUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

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
    public TextMeshProUGUI cmPartyStatsText;
    public TextMeshProUGUI cmEnemyStatsText;
    public Transform cmLogParent;
    public GameObject cmLogEntryPrefab;
    public Button cmAttackButton;
    public Button cmPotionButton;
    public Button cmReviveButton;
    public Button cmSkillButton;
    public Button cmUltimateButton;
    public Button cmCycleCharacterButton;

    public TextMeshProUGUI combatTitleText;
    private int _activeCharacterIndex = 0;

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
        if (cmSkillButton != null)
            cmSkillButton.onClick.AddListener(() =>
            {
                var active = GetActivePartyMember();
                if (active != null)
                    CombatManager.Instance.PlayerSkill(active);
            });

        if (cmUltimateButton != null)
            cmUltimateButton.onClick.AddListener(() =>
            {
                var active = GetActivePartyMember();
                if (active != null)
                    CombatManager.Instance.PlayerUltimate(active);
            });

        if (nextRoomButton != null)
        {
            var btn = nextRoomButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() =>
                    GameManager.Instance.GoToNextRoom());
        }

        if (cmAttackButton != null)
            cmAttackButton.onClick.AddListener(() =>
                CombatManager.Instance.PlayerAttackAll());

        if (cmPotionButton != null)
            cmPotionButton.onClick.AddListener(() =>
                GameManager.Instance.TryUsePotion());

        if (cmReviveButton != null)
            cmReviveButton.onClick.AddListener(() =>
                GameManager.Instance.TryUseRevivePotion());

        if (cmCycleCharacterButton != null)
            cmCycleCharacterButton.onClick.AddListener(CycleCharacter);

        RefreshHUD();

        // Client requests current room state from host once scene is loaded
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost)
        {
            NetworkGameSync.Instance.RequestRoomSyncServerRpc();
        }
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
        if (nextRoomButton == null) return;
        bool isHost = NetworkManager.Singleton == null
            || NetworkManager.Singleton.IsHost;
        nextRoomButton.SetActive(isHost);
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
        if (cmEnemyStatsText != null)
        {
            if (CombatManager.Instance != null &&
                CombatManager.Instance.enemies.Count > 0)
            {
                if (combatTitleText != null)
                    combatTitleText.text =
                        CombatManager.Instance.enemies.Count > 1
                        ? "Combat! vs Goblin Horde"
                        : $"Combat! vs {CombatManager.Instance.enemies[0].enemyName}";

                string enemyStats = "";
                foreach (var e in CombatManager.Instance.enemies)
                {
                    enemyStats +=
                        $"{e.enemyName}{(e.isDead ? " [DEAD]" : "")}\n" +
                        $"HP: {e.currentHP}/{e.template.baseHP}\n" +
                        $"Armor: {e.currentArmor}\n\n";
                }
                cmEnemyStatsText.text = enemyStats.TrimEnd('\n');
            }
        }

        string partyStats = "";
        var active = GetActivePartyMember();
        var myChars = GetMyCharacters();

        var displayList = myChars.Count > 0
            ? myChars
            : GameManager.Instance.party;

        foreach (var member in displayList)
        {
            bool isActive = active != null && member == active;
            string skillStatus = member.skillCooldownLeft > 0
                ? $"{member.classTemplate.skillName} ({member.skillCooldownLeft})"
                : member.classTemplate.skillName + " Ready";
            string ultStatus = member.ultimateCooldownLeft > 0
                ? $"{member.classTemplate.ultimateName} ({member.ultimateCooldownLeft})"
                : member.classTemplate.ultimateName + " Ready";

            if (isActive)
            {
                skillStatus = $"<color=#00ff00>{skillStatus}</color>";
                ultStatus   = $"<color=#00ff00>{ultStatus}</color>";
            }

            partyStats +=
                $"{member.playerName}" +
                $"{(member.isDead ? " [DEAD]" : "")}\n" +
                $"HP: {member.currentHP}/{member.classTemplate.baseHP}\n" +
                $"Skill: {skillStatus}\n" +
                $"Ult: {ultStatus}\n\n";
        }

        if (cmPartyStatsText != null)
            cmPartyStatsText.text = partyStats;
    }

    // ===== CHARACTER OWNERSHIP =====
    private ulong GetLocalClientId()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsConnectedClient)
            return NetworkManager.Singleton.LocalClientId;
        return ulong.MaxValue;
    }

    public System.Collections.Generic.List<PlayerRunTimeData> GetMyCharacters()
    {
        ulong myId = GetLocalClientId();

        if (myId == ulong.MaxValue)
            return GameManager.Instance.party.FindAll(p => !p.isDead);

        var mine = GameManager.Instance.party.FindAll(
            p => p.ownerClientId == myId && !p.isDead);

        if (mine.Count == 0)
            return GameManager.Instance.party.FindAll(p => !p.isDead);

        return mine;
    }

    public PlayerRunTimeData GetActivePartyMember()
    {
        var mine = GetMyCharacters();
        if (mine.Count == 0) return null;
        if (_activeCharacterIndex >= mine.Count)
            _activeCharacterIndex = 0;
        return mine[_activeCharacterIndex];
    }

    public void CycleCharacter()
    {
        var mine = GetMyCharacters();
        if (mine.Count <= 1) return;
        _activeCharacterIndex = (_activeCharacterIndex + 1) % mine.Count;
        RefreshCombatPanel();
    }
}