using Godot;
using System;

public partial class GridMover : CharacterBody2D
{
    [Signal] public delegate void MoveCompletedEventHandler();

    public const float TileSize = 42f;       
    public const float MoveDuration = 0.07f;

    // ADD THIS: A reference to your AnimatedSprite2D
    [Export] public AnimatedSprite2D Sprite;

    private bool _isMoving;
    private Vector2 _startPosition;
    private Vector2 _targetPosition;
    private float _moveElapsed; 

    public bool IsMoving => _isMoving;
    public Vector2 LastMoveDirection { get; private set; } = Vector2.Down;

    public override void _Ready()
    {
        // If you forgot to assign it in the Inspector, try to find it automatically
        Sprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        
        UpdateAnimation(LastMoveDirection, false); // Start in Idle
    }

    public virtual bool OnMoveIntercepted(Vector2 targetWorldPos) => false;

    public bool TryMove(Vector2 direction)
    {
        if (_isMoving) return false;
        if (direction == Vector2.Zero) return false;

        LastMoveDirection = direction.Normalized();

        GlobalPosition = new Vector2(
            Mathf.Round(GlobalPosition.X / TileSize) * TileSize,
            Mathf.Round(GlobalPosition.Y / TileSize) * TileSize
        );

        _startPosition = GlobalPosition;
        _targetPosition = _startPosition + direction.Normalized() * TileSize;

        if (OnMoveIntercepted(_targetPosition)) return false;
        if (IsTileBlocked(_targetPosition)) return false;

        _isMoving = true;
        _moveElapsed = 0f;

        // ADD THIS: Play the walking animation
        UpdateAnimation(LastMoveDirection, true);

        return true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isMoving) return;

        _moveElapsed += (float)delta;
        float t = _moveElapsed / MoveDuration;

        if (t >= 1f)
        {
            GlobalPosition = _targetPosition;
            _isMoving = false;
            
            // ADD THIS: Play the idle animation when we arrive
            UpdateAnimation(LastMoveDirection, false);
            
            EmitSignal(SignalName.MoveCompleted);
        }
        else
        {
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            GlobalPosition = _startPosition.Lerp(_targetPosition, smoothT);
        }
    }

    // ADD THIS: The logic that decides which animation to play
    private void UpdateAnimation(Vector2 direction, bool isMoving)
    {
        if (Sprite == null) return;

        // If you don't have walking animations yet, we can just pause on idle
        if (!isMoving && Sprite.SpriteFrames != null)
        {
            string idleAnim = GetAnimName(direction, "idle");
            if (Sprite.SpriteFrames.HasAnimation(idleAnim))
            {
                Sprite.Play(idleAnim);
            }
        }
        else if (isMoving && Sprite.SpriteFrames != null)
        {
            string walkAnim = GetAnimName(direction, "caminar");
            if (Sprite.SpriteFrames.HasAnimation(walkAnim))
            {
                Sprite.Play(walkAnim);
            }
        }

        // Flip the sprite for Left/Right movement
        if (direction.X < 0) Sprite.FlipH = true;
        else if (direction.X > 0) Sprite.FlipH = false;
    }

    // ADD THIS: Maps the direction to a string (e.g., "walk_left")
    private string GetAnimName(Vector2 direction, string prefix)
    {
        // For left/right, we use "side" so you don't have to draw 4 unique animations.
        // The FlipH logic above handles the mirroring.
        if (direction == Vector2.Up) return $"{prefix}_up";
        if (direction == Vector2.Down) return $"{prefix}_down";
        if (direction.X != 0) return $"{prefix}_side";
        
        return $"{prefix}_down"; // Default fallback
    }

    public void ForceTeleport(Vector2 newPosition)
    {
        _isMoving = false;
        GlobalPosition = new Vector2(
            Mathf.Round(newPosition.X / TileSize) * TileSize,
            Mathf.Round(newPosition.Y / TileSize) * TileSize
        );
        _startPosition = GlobalPosition;
        _targetPosition = GlobalPosition;
        UpdateAnimation(LastMoveDirection, false);
    }
	public bool IsTileBlocked(Vector2 worldPosition)
	{
		var space = GetWorld2D().DirectSpaceState;
		var query = new PhysicsPointQueryParameters2D
		{
			Position = worldPosition,
			CollisionMask = 1,
			CollideWithBodies = true,
			CollideWithAreas = true
		};
		var hits = space.IntersectPoint(query);

		foreach (var hit in hits)
		{
			if (hit.TryGetValue("collider", out var colliderVar) && colliderVar.Obj is GodotObject colliderObj)
			{
				// Cast to Node to check hierarchy
				var body = colliderObj as Node;
				if (body != null)
				{
					// Ignore this node, its parent, its children, or its ancestors
					if (body != this && body != GetParent() && !IsAncestorOf(body) && !body.IsAncestorOf(this))
						return true;
				}
			}
		}
		return false;
	}
}
