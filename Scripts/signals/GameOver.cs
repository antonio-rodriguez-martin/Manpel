using Godot;

public partial class GameOver : CanvasLayer
{
     public override void _Ready()
    {
        // Ensure the script runs even if we pause the game
        ProcessMode = Node.ProcessModeEnum.Always;
        
        // Pause the game so the overworld doesn't keep running in the background
        GetTree().Paused = true;
    }

    public override void _Input(InputEvent @event)
    {
        // Check for Space, Enter, or Left Mouse Click
        if (@event.IsActionPressed("ui_accept") || 
            (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left))
        {
            RestartGame();
        }
    }

    // If you have a Button on the screen, connect its "Pressed" signal to this method
    public void RestartGame()
    {
        // 1. Unpause the game so the new scene starts normally
        GetTree().Paused = false;
        
        // 2. Safely delete this Game Over screen at the end of the frame
        QueueFree();
        
        // 3. CallDeferred prevents tree crashes while reloading
        Callable.From(() => {
            // If you want to reload the exact map you died on:
            GetTree().ReloadCurrentScene(); 
            
            // OR if ReloadCurrentScene fails, you can force the main map path here:
            // GetTree().ChangeSceneToFile("res://Scenes/World.tscn"); 
        }).CallDeferred();
    }
}