using Godot;

public partial class WorldGenerator : Node2D
{
    // Add this static instance
    public static WorldGenerator Instance { get; private set; }

    [Export] public MapRegionResource[] Regions;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void ApplyRegionSettings(int index, Player player, Vector2? spawnPosition = null)
    {
        if (index < 0 || index >= Regions.Length)
        {
            GD.PrintErr($"Region index {index} is out of bounds!");
            return;
        }

        var region = Regions[index];
        
        if (spawnPosition.HasValue)
        {
            player.GlobalPosition = spawnPosition.Value;
        }

        var camera = player.GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            camera.LimitLeft = region.LimitLeft;
            camera.LimitRight = region.LimitRight;
            camera.LimitTop = region.LimitTop;
            camera.LimitBottom = region.LimitBottom;
            camera.ResetSmoothing(); 
        }
    }
}