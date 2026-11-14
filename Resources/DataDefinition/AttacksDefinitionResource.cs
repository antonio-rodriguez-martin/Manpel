using Godot;

[GlobalClass]
public partial class AttacksDefinitionResource : Resource
{
    [Export] Godot.Collections.Array<EnemyAttackResource> attacks;
}