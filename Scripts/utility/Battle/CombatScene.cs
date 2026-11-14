using Godot;
using Attacks;

public partial class CombatScene : Node2D
{
    [Export] public Label MessageLabel;
    [Export] public ProgressBar PlayerHPBar;
    [Export] public ProgressBar PlayerMPBar;
    [Export] public ProgressBar EnemyHPBar;
    [Export] public Label PlayerHPLabel;
    [Export] public Label PlayerMPLabel;
    [Export] public Label EnemyHPLabel;
    [Export] public Container AttackButtons;
    
    [Export] public Control OutcomePanel;
    [Export] public Label OutcomeTitle;
    [Export] public Button ContinueButton;

    private CombatManager _manager;

    public override void _Ready()
    {
        _manager = GetNode<CombatManager>("CombatManager");

        _manager.StateChanged += OnStateChanged;
        _manager.HealthUpdated += OnHealthUpdated;
        _manager.MessageUpdated += OnMessageUpdated;

        // Wire buttons based on their string names
        WireButton("SeedButton", PlayerAttacksType.Seed);
        WireButton("FireButton", PlayerAttacksType.Fire);
        WireButton("WaterButton", PlayerAttacksType.Water);

        ContinueButton.Pressed += () => _manager.CloseCombat();
        OutcomePanel.Visible = false;
    }

    private void OnStateChanged(int state)
    {
        var combatState = (CombatState)state;
        SetButtonsEnabled(combatState == CombatState.PlayerTurn);

        if (combatState == CombatState.Victory || combatState == CombatState.Defeat)
        {
            OutcomeTitle.Text = combatState == CombatState.Victory ? "VICTORY!" : "DEFEAT...";
            OutcomePanel.Visible = true;
            ContinueButton.CallDeferred("grab_focus");
        }
    }

    private void OnHealthUpdated(int p, int pMax, int pMp, int pMaxMp, int e, int eMax)
    {
        PlayerHPBar.MaxValue = pMax; 
        PlayerHPBar.Value = p;
        
        if (PlayerMPBar != null) 
        {
            PlayerMPBar.MaxValue = pMaxMp;
            PlayerMPBar.Value = pMp;
            PlayerMPLabel.Text = $"MP: {pMp} / {pMaxMp}";
        }
        
        EnemyHPBar.MaxValue = eMax; 
        EnemyHPBar.Value = e;
        
        PlayerHPLabel.Text = $"HP: {p} / {pMax}";
        EnemyHPLabel.Text = $"Enemy: {e} / {eMax}";
    }

    private void OnMessageUpdated(string msg) => MessageLabel.Text = msg;

    private void WireButton(string nodeName, PlayerAttacksType type)
    {
        if (AttackButtons == null) return;
        
        var btn = AttackButtons.GetNodeOrNull<Button>(nodeName);
        if (btn != null)
        {
            btn.Pressed += () => {
                // 1. Fallback damage based on the PlayerAttacksType
                int baseDamage = type switch
                {
                    PlayerAttacksType.Seed => 18,
                    PlayerAttacksType.Fire => 24,
                    PlayerAttacksType.Water => 5,
                    _ => 1
                };

                // 2. Try to override with Resource damage if it exists
                var playerAttacks = _manager.GetPlayerState().Definition.Attacks?.attacks;
                if (playerAttacks != null)
                {
                    foreach (var atk in playerAttacks)
                    {
                        if (atk.attackType.ToString() == type.ToString())
                        {
                            baseDamage = atk.Damage;
                            break;
                        }
                    }
                }

                // 3. Create the action
                var action = new CombatAction { 
                    IsPlayerAction = true, 
                    AttackName = type.ToString(), 
                    Damage = baseDamage 
                };
                
                _manager.ExecuteAction(action);
            };
        }
        else
        {
            GD.PrintErr($"Could not find button named {nodeName} inside AttackButtons!");
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (AttackButtons == null) return;
        
        Button firstButton = null;
        foreach (var c in AttackButtons.GetChildren())
        {
            if (c is Button b)
            {
                b.Disabled = !enabled;
                if (enabled && firstButton == null) firstButton = b;
            }
        }
        
        if (enabled && firstButton != null)
            firstButton.CallDeferred("grab_focus");
    }
}