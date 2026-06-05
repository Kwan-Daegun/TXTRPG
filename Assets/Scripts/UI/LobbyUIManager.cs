using UnityEngine;
using TMPro;
public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance { get; private set; }

    [Header("Lobby UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI localIPText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
}
