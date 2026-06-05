
using UnityEngine;

[CreateAssetMenu(fileName = "CharClass", menuName = "Character Class")]
public class CharacterClass : ScriptableObject
{
    [Header("Identity")]
    public string className;
    public string weaponName;
    public Sprite classIcon;

    [Header("Base Stats")]
    public int baseHP;
    public int baseArmor;
    
    [Header("Attack Range")]
    public int minAttack;
    public int maxAttack;

    [Header("Combat Chances")]
    [Range(0, 100)] public int dodgeChance;
    [Range(0, 100)] public int blockChance;
    [Range(0, 100)] public int counterChance;

    [Header("Skill")]
    public string skillName;
    public string skillDescription;
    public int skillCooldown;

    [Header("Ultimate")]
    public string ultimateName;
    public string ultimateDescription;
    public int ultimateCooldown;
}

