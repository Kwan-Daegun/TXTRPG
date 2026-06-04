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

    public void Setup(CharacterClass so, int index,
        CharSelectManager manager)
    {
        _index   = index;
        _manager = manager;

        if (slotNameText != null)
            slotNameText.text = so.className;
        if (slotClassText != null)
            slotClassText.text = so.weaponName;
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(true);
            removeButton.onClick.AddListener(OnRemove);
        }
    }

    public void SetEmpty()
    {
        if (slotNameText != null)
            slotNameText.text = "Empty Slot";
        if (slotClassText != null)
            slotClassText.text = "";
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);
    }

    private void OnRemove()
    {
        _manager.RemoveFromParty(_index);
    }
}