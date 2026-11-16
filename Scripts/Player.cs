using Godot;
using System.Collections.Generic;

public partial class Player : GridMover
{
    [Export] public PlayerDefinitionData Definition;
    [Export] public float StepCooldown = 0.15f; // Pause between steps (in seconds)
    [Export] public Camera2D camera;

    public bool CanMove = true;
    public PlayerState State;
    private static readonly StringName[] InputActions =
        { "ui_left", "ui_right", "ui_up", "ui_down" };

    private readonly List<StringName> _heldOrder = new();
    private float _currentCooldown = 0f;

    public override void _Ready()
    {
        RuntimeDataSpace.RuntimeData.player = this;

        if (State == null)
            State = new PlayerState(Definition);

        MoveCompleted += OnMoveCompleted;
        camera.LimitLeft = 600;
        camera.LimitRight = 500;
        camera.LimitTop = 0;
        camera.LimitBottom = 800;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Tick down the cooldown timer
        if (_currentCooldown > 0)
            _currentCooldown -= (float)delta;

        if (!CanMove) return;
        if (IsMoving) return;
        
        // Don't process input if we are still on cooldown
        if (_currentCooldown > 0) return;

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase != TurnManager.Phase.PlayerTurn) 
            return;

        foreach (var a in InputActions)
        {
            if (Input.IsActionJustPressed(a))
            {
                _heldOrder.Remove(a);
                _heldOrder.Add(a);
            }
        }
        _heldOrder.RemoveAll(a => !Input.IsActionPressed(a));

        if (_heldOrder.Count == 0) return;

        Vector2 dir = ActionToDirection(_heldOrder[^1]);
        
        if (TryMove(dir))
        {
            // Reset the cooldown after successfully starting a move
            _currentCooldown = StepCooldown; 
        }
    }

    private void OnMoveCompleted()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.StartEnemyTurn();
        }
    }

     public override bool OnMoveIntercepted(Vector2 targetWorldPos)
    {
        Enemy1 enemy = FindEnemyAt(targetWorldPos);
        if (enemy != null)
        {
            // Check if the enemy is a Boss
            if (enemy is Boss bossEnemy)
            {
                BossTransition.Instance.StartBossBattle(this, bossEnemy);
            }
            else
            {
                CombatTransition.Instance.StartBattle(this, enemy);
            }
            return true; 
        }
        return false;
    } 

    private Enemy1 FindEnemyAt(Vector2 worldPos)
    {
        foreach (var n in GetTree().GetNodesInGroup("enemies"))
            if (n is Enemy1 e && e.GlobalPosition.DistanceTo(worldPos) < TileSize * 0.5f)
                return e;
        return null;
    }

    private static Vector2 ActionToDirection(StringName action)
    {
        if (action == "ui_left")  return Vector2.Left;
        if (action == "ui_right") return Vector2.Right;
        if (action == "ui_up")    return Vector2.Up;
        if (action == "ui_down")  return Vector2.Down;
        return Vector2.Zero;
    }
}