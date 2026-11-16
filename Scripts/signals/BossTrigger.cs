using Godot;

public partial class BossTrigger : Area2D
{
    // Drag your Boss node from the Scene Tree into this slot in the Inspector
    [Export] public Boss TargetBoss;

    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_triggered || !body.IsInGroup("player")) return;
        if (TargetBoss == null || !IsInstanceValid(TargetBoss)) return;

        _triggered = true;
        var player = body as Player;
        
        // Freeze the player instantly so they don't move while the scene loads
        if (player != null) player.CanMove = false; 

        GD.Print("Boss Area Triggered!");
        
        // CRITICAL FIX: Defer the scene loading to prevent physics engine crashes
        Callable.From(() => BossTransition.Instance.StartBossBattle(player, TargetBoss)).CallDeferred();
    }
}