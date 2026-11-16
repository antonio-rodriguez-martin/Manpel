using Godot;

[GlobalClass]
public partial class MapRegionResource : Resource
{
    [ExportGroup("Camera Limits")]
    [Export] public int LimitLeft = 0;
    [Export] public int LimitRight = 1000;
    [Export] public int LimitTop = 0;
    [Export] public int LimitBottom = 1000;
}