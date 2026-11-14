using Godot;

public partial class BattleScene : Node2D
{
    private PlayerState _player;
    private EnemyDefinitionData _enemyDef;
    private Enemy1 _mapEnemy;
    private Node2D _mapScene;

    public void Initialize(
        PlayerState player,
        EnemyDefinitionData enemyDef,
        Enemy1 mapEnemy,
        Node2D mapScene)
    {
        _player   = player;
        _enemyDef = enemyDef;
        _mapEnemy = mapEnemy;
        _mapScene = mapScene;
    }

    public override void _Ready()
    {
        // Safe to use _player, _enemyDef, etc. here.
        GD.Print($"Battle vs {_enemyDef.DisplayName} " +
                 $"({_enemyDef.MaxHP} HP, {_enemyDef.GoldReward} gold)");
    }

    /// Call this from your battle UI when the fight is over.
    public void EndBattle(bool playerWon)
    {
        // Restore the map
        _mapScene.ProcessMode = Node.ProcessModeEnum.Inherit;
        _mapScene.Visible     = true;
        GetTree().CurrentScene = _mapScene;

        // Remove the defeated enemy from the overworld
        if (playerWon && IsInstanceValid(_mapEnemy))
            _mapEnemy.QueueFree();

        QueueFree();
    }
}