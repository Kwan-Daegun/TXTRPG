
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyClass", menuName = "Enemy Class")]
public class EnemyClass : ScriptableObject
{
    [Header("Identity")]
    public string EnemyName;
    public string EnemyType;
    public Sprite EnemyIcon;

    [Header("Base Stats")]
    public int baseHP;
    public int baseArmor;

    [Header("AttackRange")]
    public int minAtk;
    public int maxAtk;

    [Header("SpecialAttackRange(Warchief Only)")]
    public int minSpecialAtk;
    public int maxSpecialAtk;

    [Header("Combat Chances")]
    [Range(0, 100)] public int dodgeChance;
    [Range(0, 100)] public int blockChance;
    [Range(0, 100)] public int counterChance;

    [Header("Loot")]
    public int goldDropMin;
    public int goldDropMax;
    [Range(0, 100)] public int PotionDropChance;
}
