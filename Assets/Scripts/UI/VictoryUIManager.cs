using UnityEngine;
using TMPro;
using Unity.Netcode;
public class VictoryUIManager : MonoBehaviour
{
     public static VictoryUIManager Instance { get; private set; }

    [Header("Victory UI")]
    public TextMeshProUGUI statsText;

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
        ShowStats();
    }

    public void ShowStats()
    {
        if (statsText == null) return;
        string stats = "Surviving Heroes:\n";
        foreach (var member in GameManager.Instance.party)
            if (!member.isDead)
                stats += $"{member.playerName} " +
                        $"({member.classTemplate.className})\n";
        stats += $"\nTotal Gold: {GameManager.Instance.gold}";
        statsText.text = stats;
    }

    public void OnPlayAgainClicked()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsHost)
        {
            NetworkGameSync.Instance.ChangeGameStateServerRpc(
                GameState.Lobby);
            return;
        }

        GameManager.Instance.ReturnToLobby();
    }

    public void OnBackToMenuClicked()
    {
        GameManager.Instance.BackToMenu();
    }
}
