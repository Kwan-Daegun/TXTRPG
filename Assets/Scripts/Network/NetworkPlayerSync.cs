// NetworkPlayerSync.cs
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerSync : NetworkBehaviour
{
    // synced player index in party list
    public NetworkVariable<int> partyIndex = 
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    // Synced HP
    public NetworkVariable<int> currentHP =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Synced Armor
    public NetworkVariable<int> currentArmor =
        new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Synced Dead state
    public NetworkVariable<bool> isDead =
        new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        currentHP.OnValueChanged    += OnHPChanged;
        currentArmor.OnValueChanged += OnArmorChanged;
        isDead.OnValueChanged       += OnDeadChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHP.OnValueChanged    -= OnHPChanged;
        currentArmor.OnValueChanged -= OnArmorChanged;
        isDead.OnValueChanged       -= OnDeadChanged;
    }

    
    private void OnHPChanged(int prev, int next)
    {
        // UIManager will refresh HUD
        // UIManager.Instance.RefreshHUD();
    }

    private void OnArmorChanged(int prev, int next)
    {
        // UIManager.Instance.RefreshHUD();
    }

    private void OnDeadChanged(bool prev, bool next)
    {
        if (next) Debug.Log($"Player {OwnerClientId} has died!");
    }

    // server rpc
    [ServerRpc]
    public void SyncStatsServerRpc(int hp, int armor, bool dead)
    {
        currentHP.Value    = hp;
        currentArmor.Value = armor;
        isDead.Value       = dead;
    }

    // client rpc
    [ClientRpc]
    public void NotifyCombatEventClientRpc(string message)
    {
        Debug.Log($"[COMBAT] {message}");
        // UIManager.Instance.LogCombat(message);
    }
}