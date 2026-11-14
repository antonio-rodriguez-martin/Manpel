using Godot;

public static class BattleTransition
{
    public static void StartBattle(Node source, Enemy1 enemy, Player player)
    {
        var tree = source.GetTree();
        var map  = (Node2D) tree.CurrentScene;

        // 1. Instantiate WITHOUT adding to tree — so _Ready hasn't run.
        var packed = GD.Load<PackedScene>("res://Battle/Battle.tscn");
        var battle = packed.Instantiate<BattleScene>();

        // 2. Hand over everything the battle needs.
        battle.Initialize(player.State, enemy.enemyData, enemy, map);

        // 3. Freeze the map in place (keeps its state perfectly intact).
        TurnManager.Instance.ResetToPlayerTurn();
        map.ProcessMode = Node.ProcessModeEnum.Disabled;
        map.Visible = false;

        // 4. Bring the battle online.
        tree.Root.AddChild(battle);
        tree.CurrentScene = battle;
    }
}