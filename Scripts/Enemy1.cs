using Godot;

public partial class Enemy1 : GridMover
{
    public override void _Ready()
    {
        AddToGroup("enemies");
        MoveCompleted += OnMoveCompleted;
        TurnManager.Instance.EnemyTurnStarted += OnEnemyTurn;
    }

    private void OnEnemyTurn()
    {
        if (RuntimeDataSpace.RuntimeData.player == null)
        {
            TurnManager.Instance.EnemyFinishedMoving();
            return;
        }

        Vector2 toPlayer = RuntimeDataSpace.RuntimeData.player.GlobalPosition - GlobalPosition;

        // ¿Ya está sobre el jugador? (bump attack)
        if (Mathf.Abs(toPlayer.X) < TileSize * 0.5f &&
            Mathf.Abs(toPlayer.Y) < TileSize * 0.5f)
        {
            // Aquí iría el ataque. Por ahora, no hacemos nada.
            TurnManager.Instance.EnemyFinishedMoving();
            return;
        }

        Vector2 dir;
        if (Mathf.Abs(toPlayer.X) >= Mathf.Abs(toPlayer.Y))
            dir = new Vector2(Mathf.Sign(toPlayer.X), 0);
        else
            dir = new Vector2(0, Mathf.Sign(toPlayer.Y));

        if (!TryMove(dir))
        {
            // Bloqueado (pared, etc.): termina su turno igualmente.
            TurnManager.Instance.EnemyFinishedMoving();
        }
    }

    private void OnMoveCompleted()
    {
        TurnManager.Instance.EnemyFinishedMoving();
    }
}