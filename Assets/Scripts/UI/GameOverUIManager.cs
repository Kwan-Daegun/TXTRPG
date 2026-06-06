using UnityEngine;
using TMPro;
using Unity.Netcode;
public class GameOverUIManager : MonoBehaviour
{
    public static GameOverUIManager Instance { get; private set; }

    [Header("GameOver UI")]
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
        string stats = "Fallen Heroes:\n";
        foreach (var member in GameManager.Instance.party)
            stats += $"{member.playerName} " +
                    $"({member.classTemplate.className})\n";
        stats += $"\nGold collected: {GameManager.Instance.gold}";
        statsText.text = stats;
    }

    public void OnTryAgainClicked()
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
