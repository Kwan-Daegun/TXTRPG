// NetworkBootstrapper.cs
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkBootstrapper : MonoBehaviour
{
    public static NetworkBootstrapper Instance { get; private set; }

    [Header("Network Settings")]
    public string ipAddress = "192.168.100.1";
    public ushort port = 7777;

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


    public void StartHost()
    {
        SetTransport();
        NetworkManager.Singleton.StartHost();
        Debug.Log($"[HOST] Started on port {port}");
        GameManager.Instance.ChangeState(GameState.Lobby);
    }

    // client joins the game using teh hot ip addrews
    public void StartClient(string ip)
    {
        ipAddress = ip;
        SetTransport();
        NetworkManager.Singleton.StartClient();
        Debug.Log($"[CLIENT] Connecting to {ip}:{port}");
    }

    //transport set up
    private void SetTransport()
    {
        var transport = NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        transport.SetConnectionData(ipAddress, port);
    }

    // callbacks for client connections and disconnections mostly for debug purposes
    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback
            += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback
            += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback
            -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback
            -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NETWORK] Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NETWORK] Client disconnected: {clientId}");

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Title);
        }
    }

    // disconnects the network session and return to the title screen
    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Title);
    }
    public string GetLocalIP()
    {
        var host = System.Net.Dns.GetHostEntry(
            System.Net.Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily ==
                System.Net.Sockets.AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }
}