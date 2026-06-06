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
    public Button startGameButton;

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

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(
                NetworkManager.Singleton.IsHost);

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
        if (playerCountText == null) return;

        // ConnectedClients is server-only; clients show a waiting message instead
        if (NetworkManager.Singleton.IsServer)
        {
            int count = NetworkManager.Singleton.ConnectedClients.Count;
            playerCountText.text = $"Players: {count}/4";
        }
        else
        {
            playerCountText.text = "Waiting for host...";
        }
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