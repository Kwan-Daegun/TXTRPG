using UnityEngine;
using TMPro;

public class TitleSceneManager : MonoBehaviour
{
    public TMP_InputField ipInputField;

    public void OnHostClicked()
    {
        NetworkBootstrapper.Instance.StartHost();
    }

    public void OnJoinClicked()
    {
        string ip = ipInputField.text;
        if (string.IsNullOrEmpty(ip))
            ip = "127.0.0.1";
        NetworkBootstrapper.Instance.StartClient(ip);
    }
}