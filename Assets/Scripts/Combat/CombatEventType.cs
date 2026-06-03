public enum CombatEventType
{
    Hit,
    Dodge,
    Block,
    Counter,
    Special,
    ArmorAbsorb,
    EnemyDied,
    PlayerDied
}

[System.Serializable]
public class CombatResult
{
    public CombatEventType eventType;
    public string attackerName;
    public string defenderName;
    public int damage;
    public string message;
}
// this is where we store the combat event types and the combat result class, which will be used to store the results of each combat actions ok?.