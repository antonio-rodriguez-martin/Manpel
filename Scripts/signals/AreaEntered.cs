using Godot;
using System;


public partial class AreaEntered : Area2D
{
      [Export] int RegionIndex;
    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_triggered) return;
        _triggered = true;

        var worldGen = GetNode<WorldGenerator>("../..");
        worldGen.EnterRegion(RegionIndex);
    }

    private void OnBodyExited(Node2D body)
    {
        _triggered = false;
    }
}