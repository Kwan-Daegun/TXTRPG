using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartySlot : MonoBehaviour
{
    public TextMeshProUGUI slotNameText;
    public TextMeshProUGUI slotClassText;
    public Button removeButton;

    private int _index;
    private CharSelectManager _manager;

    public void Setup(CharacterClass so, int index, CharSelectManager manager)
    {
        _index   = index;
        _manager = manager;

        if (slotNameText != null) slotNameText.text = so.className;
        if (slotClassText != null) slotClassText.text = so.weaponName;
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(true);
            removeButton.onClick.AddListener(OnRemove);
        }
    }

    // Other players' picks — shown but not removable
    public void SetupReadOnly(CharacterClass so)
    {
        if (slotNameText != null) slotNameText.text = so.className;
        if (slotClassText != null) slotClassText.text = "[Other Player]";
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
    }

    public void SetEmpty()
    {
        if (slotNameText != null) slotNameText.text = "Empty Slot";
        if (slotClassText != null) slotClassText.text = "";
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
    }

    private void OnRemove()
    {
        _manager.RemoveFromParty(_index);
    }
}