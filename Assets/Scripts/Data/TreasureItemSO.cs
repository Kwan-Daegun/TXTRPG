using UnityEngine;

[CreateAssetMenu(fileName = "NewTreasure", 
menuName = "FantasyRPG/Treasure Item")]
public class TreasureItemSO : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;

    public enum TreasureType { Gold, Potion, WeaponBoost }
    public TreasureType treasureType;
    public int value;
}