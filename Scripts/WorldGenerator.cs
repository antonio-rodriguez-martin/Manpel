using Godot;
using RuntimeDataSpace;
using System;

public partial class WorldGenerator : Node2D
{
    [Export] MapRegionResource[] regions;
    [Export] AudioStreamPlayer SoundPlayer;

    private int _currentRegionIndex = -1;

    /*
    public void EnterRegion(int index)
    {
        if (index == _currentRegionIndex) return;
        _currentRegionIndex = index;

         for (int i = 0; i < regions.Length; i++)
        {
            regions[i].Visible = (i == index);
        }

        var transition = GetNode<Transition>("/root/Transition");
        GD.Print($"Transition found: {transition != null}");
        GD.Print($"Overlay found: {transition.Overlay != null}");


        _ = transition.TransitionToAsync(() =>
        {
            if (regions[index].Music != null)
            {
                SoundPlayer.Stream = regions[index].Music;
                SoundPlayer.Play();
            }
        });   
    }   
    */
    public void EnterRegion(int index)
    {
        if (index == _currentRegionIndex) return;
        _currentRegionIndex = index;

        var transition = GetNode<Transition>("/root/Transition");
        GD.Print($"Transition found: {transition != null}");
        GD.Print($"Overlay found: {transition.Overlay != null}");


        _ = transition.TransitionToAsync(() =>
        {
            if (regions[index].Music != null)
            {
                SoundPlayer.Stream = regions[index].Music;
                SoundPlayer.Play();
            }
        });   
    }   
    
}