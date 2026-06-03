using UnityEngine;
using Unity.Netcode;

public class NetworkGameSync : NetworkBehaviour
{
    public static NetworkGameSync Instance { get; private set; }

    // synced game state
    public NetworkVariable<GameState> gameState =
        new NetworkVariable<GameState>(GameState.Title,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //synced gold
    public NetworkVariable<int> gold =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //synced potions
    public NetworkVariable<int> potions =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    //synced room index
    public NetworkVariable<int> roomIndex =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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

    public override void OnNetworkSpawn()
    {
        gameState.OnValueChanged += OnGameStateChanged;
        gold.OnValueChanged      += OnGoldChanged;
        potions.OnValueChanged   += OnPotionsChanged;
    }

    public override void OnNetworkDespawn()
    {
        gameState.OnValueChanged -= OnGameStateChanged;
        gold.OnValueChanged      -= OnGoldChanged;
        potions.OnValueChanged   -= OnPotionsChanged;
    }

    //change value 
    private void OnGameStateChanged(GameState prev, GameState next)
    {
        GameManager.Instance.ChangeState(next);
    }

    private void OnGoldChanged(int prev, int next)
    {
        GameManager.Instance.gold = next;
        // UIManager.Instance.RefreshHUD();
    }

    private void OnPotionsChanged(int prev, int next)
    {
        GameManager.Instance.potions = next;
        // UIManager.Instance.RefreshHUD();
    }

    //server rpc
    [ServerRpc(RequireOwnership = false)]
    public void ChangeGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGoldServerRpc(int amount)
    {
        gold.Value += amount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePotionsServerRpc(int amount)
    {
        potions.Value += amount;
    }

    // client rpc
    [ClientRpc]
    public void ShowNarrativeClientRpc(string message)
    {
        // UIManager.Instance.ShowNarrative(message);
    }
}