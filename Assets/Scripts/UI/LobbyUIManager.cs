// LobbyUIManager.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [Header("Lobby UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI localIPText;
    public TextMeshProUGUI playerCountText;
    public Button startGameButton; // only host sees this enabled

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
        ShowLocalIP();
        UpdatePlayerCount();

        // Only host can start
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(
                NetworkManager.Singleton.IsHost);

        // Listen for new connections
        NetworkManager.Singleton.OnClientConnectedCallback
            += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback
            += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback
                -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback
                -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        UpdatePlayerCount();
        SetStatus($"Player {clientId} joined!");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UpdatePlayerCount();
        SetStatus($"Player {clientId} left.");
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void ShowLocalIP()
    {
        string ip = NetworkBootstrapper.Instance.GetLocalIP();
        if (localIPText != null)
            localIPText.text = $"Your IP: {ip}";
    }

    public void UpdatePlayerCount()
    {
        int count = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCountText != null)
            playerCountText.text = $"Players: {count}/4";
    }

    public void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        NetworkGameSync.Instance.ChangeGameStateServerRpc(
            GameState.CharacterSelect);
    }

    public void OnDisconnectClicked()
    {
        NetworkBootstrapper.Instance.Disconnect();
    }
}