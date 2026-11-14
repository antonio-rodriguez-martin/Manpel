using Godot;

public enum CombatState { PlayerTurn, EnemyTurn, Busy, Victory, Defeat }

public class CombatAction
{
    public string AttackName { get; set; }
    public int Damage { get; set; }
    public bool IsPlayerAction { get; set; }
}