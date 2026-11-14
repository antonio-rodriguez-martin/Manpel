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
        var player = RuntimeDataSpace.RuntimeData.player;
        if (player == null) { Finish(); return; }

        var myCell     = GridMath.WorldToCell(GlobalPosition);
        var playerCell = GridMath.WorldToCell(player.GlobalPosition);

        var delta = playerCell - myCell;
        if (Mathf.Abs(delta.X) + Mathf.Abs(delta.Y) == 1)
        {
            // TODO: transicion a batalla
            Finish();
            return;
        }

        var step = Pathfinder.FindNextStep(myCell, playerCell, IsBlockedForPathing);
        if (step == Vector2I.Zero) { Finish(); return; }

        var dest = myCell + step;

        if (dest == playerCell)
        {
            // TODO(ullr): transicion a batalla
            Finish();
            return;
        }

        if (!TurnManager.Instance.ReservedCells.Add(dest))
        {
            Finish(); 
            return;
        }

        if (!TryMove(new Vector2(step.X, step.Y)))
            Finish();
    }

    private bool IsBlockedForPathing(Vector2I cell)
    {
        if (TurnManager.Instance.ReservedCells.Contains(cell))
            return true;

        return IsTileBlocked(GridMath.CellToWorld(cell));
    }

    private void Finish() => TurnManager.Instance.EnemyFinishedMoving();
    private void OnMoveCompleted() => Finish();

}