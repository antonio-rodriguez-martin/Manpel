using Godot;

public partial class Boss : Enemy1
{
    public override void _Ready()
    {
        // WE DO NOT CALL base._Ready() HERE!
        // This prevents the boss from joining the "enemies" group, 
        // ignoring the TurnManager, and prevents the pathfinding memory leak.
        
        // If you need it to be in a specific group for other scripts, add it here:
        AddToGroup("boss");
    }

    public override bool OnMoveIntercepted(Vector2 targetWorldPos)
    {
        // The Area2D handles the trigger, so the physical body does nothing.
        return false; 
    }
}