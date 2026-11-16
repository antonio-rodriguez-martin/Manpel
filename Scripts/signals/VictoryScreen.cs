using Godot;

public partial class VictoryScreen : CanvasLayer
{
    public override void _Ready()
    {
        ProcessMode = Node.ProcessModeEnum.Always;
        GetTree().Paused = true;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") || 
            (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left))
        {
            GetTree().Paused = false;
            QueueFree();
            
            // Return to your main menu, or just reload the map
            Callable.From(() => {
                // Change this to your Main Menu scene!
                GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn"); 
                
                // OR if you don't have a menu, just reload the world:
                // GetTree().ReloadCurrentScene();
            }).CallDeferred();
        }
    }
}