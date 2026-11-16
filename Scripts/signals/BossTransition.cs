using Godot;

public partial class BossTransition : Node
{
    public static BossTransition Instance { get; private set; }

    private const string BossScenePath = "res://Scenes/boss_scene.tscn"; 
    private Node2D _worldLayer;
    private Node2D _bossLayer;

    public override void _EnterTree() => Instance = this;

    public void StartBossBattle(Player player, Boss boss)
    {
        // CRITICAL FIX: Prevent double instantiation (Memory Leak)
        if (_bossLayer != null && IsInstanceValid(_bossLayer)) return;

        _worldLayer = GetTree().CurrentScene as Node2D;
        
        var worldCamera = _worldLayer.GetViewport().GetCamera2D();
        Vector2 cameraPos = worldCamera != null ? worldCamera.GlobalPosition : Vector2.Zero;

        _worldLayer.Visible = false; 
        _worldLayer.ProcessMode = Node.ProcessModeEnum.Disabled;
        player.CanMove = false;

        var packed = GD.Load<PackedScene>(BossScenePath);
        _bossLayer = packed.Instantiate<Node2D>();
        
        _bossLayer.GlobalPosition = cameraPos;
        GetTree().Root.AddChild(_bossLayer);

        var combatCamera = _bossLayer.GetNodeOrNull<Camera2D>("Camera2D");
        if (combatCamera != null)
        {
            combatCamera.MakeCurrent();
        }

        var manager = _bossLayer.GetNode<CombatManager>("CombatManager");
        manager.Setup(player.State, boss);
        MusicManager.Instance?.PlayCustomMusic();
    }

	 public void EndBossCombat(bool playerWon, Boss mapBoss)
    {
        _worldLayer.ProcessMode = Node.ProcessModeEnum.Inherit;
        _worldLayer.Visible = true; 
        
        var player = _worldLayer.GetNode<Player>("Player");
        if (player != null) player.CanMove = true;
        
        if (playerWon && IsInstanceValid(mapBoss))
        {
            // CHANGED: Call Die() instead of QueueFree()
            mapBoss.Die(); 
        }

        _bossLayer.QueueFree();
        _bossLayer = null; 
        
        var worldCamera = _worldLayer.GetViewport().GetCamera2D();
        if (worldCamera != null) worldCamera.MakeCurrent();

        TurnManager.Instance?.ResetToPlayerTurn();
        MusicManager.Instance?.StopCustomMusic();
    }
}