using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CharSelectUIManager : MonoBehaviour
{
     public static CharSelectUIManager Instance { get; private set; }

    [Header("CharSelect UI")]
    public Transform partySlotsParent;
    public Transform classCardsParent;
    public Button beginAdventureButton;
    public GameObject classCardPrefab;
    public GameObject partySlotPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UpdateBeginButton(bool interactable)
    {
        if (beginAdventureButton != null)
            beginAdventureButton.interactable = interactable;
    }
    
}
