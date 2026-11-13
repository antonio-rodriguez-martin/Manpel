using Godot;

public partial class TurnManager : Node
{
    public enum Phase { PlayerTurn, EnemyTurn }

    public static TurnManager Instance { get; private set; }

    public Phase CurrentPhase { get; private set; } = Phase.PlayerTurn;

    private int _pendingEnemies;

    [Signal] public delegate void EnemyTurnStartedEventHandler();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void StartEnemyTurn()
    {
        _pendingEnemies = GetTree().GetNodesInGroup("enemies").Count;

        if (_pendingEnemies == 0)
        {
            // No hay enemigos: devolvemos el turno al jugador de inmediato.
            CurrentPhase = Phase.PlayerTurn;
            return;
        }

        CurrentPhase = Phase.EnemyTurn;
        EmitSignal(SignalName.EnemyTurnStarted);
    }

    public void EnemyFinishedMoving()
    {
        _pendingEnemies--;
        if (_pendingEnemies <= 0)
            CurrentPhase = Phase.PlayerTurn;
    }
}