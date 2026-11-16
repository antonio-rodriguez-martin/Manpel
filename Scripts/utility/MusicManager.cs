using Godot;
using System;

public partial class MusicManager : AudioStreamPlayer
{
    public static MusicManager Instance { get; private set; }

    // Ruta directa a tu archivo de audio en la carpeta
    private const string MusicPath = "res://Assets/Sound/Trap Majestic Beat.wav";

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            // Opcional: Si quieres que persista entre cambios de escena
            // ProcessMode = ProcessModeEnum.Always;
        }
        else
        {
            QueueFree();
            return;
        }

        // Cargamos el archivo .wav automáticamente al iniciar
        var audioStream = GD.Load<AudioStream>(MusicPath);
        if (audioStream != null)
        {
            Stream = audioStream;
        }
        else
        {
            GD.PrintErr("No se pudo cargar la música en la ruta: " + MusicPath);
        }
    }

    public void PlayCustomMusic()
    {
        if (Stream != null && !Playing)
        {
            Play();
        }
    }

    public void StopCustomMusic()
    {
        if (Playing)
        {
            Stop();
        }
    }
}