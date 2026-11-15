using Godot;
using System;

[GlobalClass]
public partial class MapRegionResource : Resource
{
    [Export] public int Id;
    [Export] public AudioStream Music;
}
