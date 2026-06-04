
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyHUDCard : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public Image hpBarFill;

    public void Setup(PlayerRunTimeData member)
    {
        if (nameText != null)
            nameText.text = member.playerName;

        UpdateHP(member);
    }

    public void UpdateHP(PlayerRunTimeData member)
    {
        int maxHP = member.classTemplate.baseHP;
        float pct = (float)member.currentHP / maxHP;

        if (hpText != null)
            hpText.text = $"{member.currentHP}/{maxHP}";

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = pct;

            
            if (pct > 0.5f)
                hpBarFill.color = Color.green;
            else if (pct > 0.25f)
                hpBarFill.color = new Color(1f, 0.6f, 0f); // orange
            else
                hpBarFill.color = Color.red;
        }

        //grey out if dead
        if (member.isDead)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0.4f;
        }
    }
}