// ClassCard.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClassCard : MonoBehaviour
{
    public TextMeshProUGUI classNameText;
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI statsText;
    public Image classIcon;
    public Button selectButton;

    private CharacterClass _classSO;

    public void Setup(CharacterClass so)
    {
        _classSO = so;

        if (classNameText != null)
            classNameText.text = so.className;
        if (weaponNameText != null)
            weaponNameText.text = so.weaponName;
        if (statsText != null)
            statsText.text =
                $"HP:{so.baseHP} ARM:{so.baseArmor}\n" +
                $"ATK:{so.minAttack}-{so.maxAttack}\n" +
                $"DOD:{so.dodgeChance}% BLK:{so.blockChance}%";
        if (classIcon != null && so.classIcon != null)
            classIcon.sprite = so.classIcon;

        selectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        CharSelectManager.Instance.AddToParty(_classSO);
    }
}