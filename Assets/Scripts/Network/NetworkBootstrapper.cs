// NetworkBootstrapper.cs
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkBootstrapper : MonoBehaviour
{
    public static NetworkBootstrapper Instance { get; private set; }

    [Header("Network Settings")]
    public string ipAddress = "";
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
        StartCoroutine(StartHostRoutine());
    }

    public void StartClient(string ip)
    {
        StartCoroutine(StartClientRoutine(ip));
    }

    private System.Collections.IEnumerator StartHostRoutine()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }

        // Host always binds to 0.0.0.0 to accept all interfaces
        var transport = NetworkManager.Singleton
            .GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", port);

        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            Debug.Log($"[HOST] Started on port {port}");
            GameManager.Instance.ChangeState(GameState.Lobby);
        }
        else
        {
            Debug.LogError("[HOST] Failed to start.");
        }
    }

    private System.Collections.IEnumerator StartClientRoutine(string ip)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }

        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogError("[CLIENT] No host IP provided. Enter the host's hotspot IP.");
            yield break;
        }

        ipAddress = ip;

        // Client connects to the host's hotspot IP
        var transport = NetworkManager.Singleton
            .GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        NetworkManager.Singleton.StartClient();
        Debug.Log($"[CLIENT] Connecting to {ip}:{port}");
    }

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

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.Title);
    }

    public string GetLocalIP()
    {
        foreach (var netInterface in
            System.Net.NetworkInformation.NetworkInterface
                .GetAllNetworkInterfaces())
        {
            if (netInterface.OperationalStatus !=
                System.Net.NetworkInformation.OperationalStatus.Up)
                continue;

            foreach (var addr in netInterface
                .GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily !=
                    System.Net.Sockets.AddressFamily.InterNetwork)
                    continue;

                string ip = addr.Address.ToString();

                if (ip.StartsWith("192.168.43.") ||
                    ip.StartsWith("172.20.10.")   ||
                    ip.StartsWith("192.168.")     ||
                    ip.StartsWith("10."))
                    return ip;
            }
        }
        return "127.0.0.1";
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }
}