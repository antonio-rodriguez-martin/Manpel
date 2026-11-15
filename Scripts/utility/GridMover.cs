using Godot;
using System;

public partial class GridMover : CharacterBody2D
{
    [Signal] public delegate void MoveCompletedEventHandler();

    public const float TileSize = 128f;
    public const float MoveDuration = 0.2f;

    private bool _isMoving;
    private Vector2 _targetPosition;

    public bool IsMoving => _isMoving;

    // Purely optional. If you don't override this, it does nothing.
    public virtual bool OnMoveIntercepted(Vector2 targetWorldPos) => false;

    public bool TryMove(Vector2 direction)
    {
        if (_isMoving) return false;
        if (direction == Vector2.Zero) return false;

        _targetPosition = GlobalPosition + direction * TileSize;

        // 1. Check for interactions (e.g., bumping into an enemy)
        if (OnMoveIntercepted(_targetPosition)) return false;

        // 2. Check for physical walls
        if (IsTileBlocked(_targetPosition)) return false;

        _isMoving = true;
        return true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isMoving) return;

        float speed = TileSize / MoveDuration;
        GlobalPosition = GlobalPosition.MoveToward(_targetPosition, speed * (float)delta);

        if (GlobalPosition.DistanceTo(_targetPosition) < 0.1f)
        {
            GlobalPosition = _targetPosition;
            _isMoving = false;
            EmitSignal(SignalName.MoveCompleted);
        }
    }

    public bool IsTileBlocked(Vector2 worldPosition)
    {
        var space = GetWorld2D().DirectSpaceState;
        var query = new PhysicsPointQueryParameters2D
        {
            Position = worldPosition,
            CollisionMask = 1,
            CollideWithAreas = false // Only check for solid bodies (walls), not areas
        };
        
        var hits = space.IntersectPoint(query);

        foreach (var hit in hits)
        {
            var collider = hit["collider"].As<Node>();
            
            // Ignore the player itself
            if (collider == this) continue;
            
            // Ignore any children of the player (Hitboxes, Shadows, etc.)
            if (IsAncestorOf(collider)) continue;

            // If it's anything else (like a TileMap wall), block the move
            return true;
        }
        return false;
    }
}