using UnityEngine;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    public TMP_InputField ipInputField;

    private void Start()
    {
        // Auto-fill with this device's local IP as a hint
        if (ipInputField != null)
            ipInputField.text = NetworkBootstrapper.Instance.GetLocalIP();
    }

    public void OnHostClicked()
    {
        NetworkBootstrapper.Instance.StartHost();
    }

    public void OnJoinClicked()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
            ip = NetworkBootstrapper.Instance.GetLocalIP();
        NetworkBootstrapper.Instance.StartClient(ip);
    }
}