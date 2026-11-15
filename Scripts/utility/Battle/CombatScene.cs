using Godot;
using System.Threading.Tasks;
using Attacks;

public partial class CombatScene : Node2D
{
    [Export] public Label MessageLabel;
    [Export] public ProgressBar PlayerHPBar;
    //[Export] public ProgressBar PlayerMPBar;
    [Export] public ProgressBar EnemyHPBar;
    [Export] public Label PlayerHPLabel;
    //[Export] public Label PlayerMPLabel;
    [Export] public Label EnemyHPLabel;
    [Export] public Container AttackButtons;

    [Export] public Control OutcomePanel;
    [Export] public Label OutcomeTitle;
    [Export] public Button ContinueButton;

    [Export] public Node2D PlayerVisual;
    [Export] public Node2D EnemyVisual;
    [Export] public float JumpHeight = 95f;
    [Export] public float JumpAscentTime = 0.22f;
    [Export] public float JumpHangTime = 0.30f;
    [Export] public float JumpDescentTime = 0.22f;
    
    [Export] public AnimatedSprite2D EnemyAttackEffect; 

    private AnimatedSprite2D _enemySprite;
    private Vector2 _playerHomePos;
    private Vector2 _enemyHomePos;

    private int JumpNum;
    private CombatManager _manager;

    // Player Jump-Dodge state
    private bool _canPlayerJump = false;
    private bool _isJumpWindowSweetSpot = false;
    private bool _isPlayerJumping = false;
    private bool _isPlayerAirborne = false;

    //Visual salto
    private Node2D _jumpCueRoot;
    private Label _jumpCueLabel;
    private ColorRect _barBg;
    private ColorRect _barSweetSpot;
    private ColorRect _barFill;
    private const float BarTotalWidth = 110f;
    private const float BarHeight = 12f;

    public override void _Ready()
    {
        _manager = GetNode<CombatManager>("CombatManager");
        _manager.RegisterScene(this);

        _manager.StateChanged += OnStateChanged;
        _manager.HealthUpdated += OnHealthUpdated;
        _manager.MessageUpdated += OnMessageUpdated;

        PlayerVisual ??= GetNodeOrNull<Node2D>("Player") ?? FindChild("Player", true, false) as Node2D;
        EnemyVisual  ??= GetNodeOrNull<Node2D>("EnemyAttackReference") ?? GetNodeOrNull<Node2D>("Enemy") ?? FindChild("Enemy*", true, false) as Node2D;

        // Cache the exact starting positions so we always return to them
        if (PlayerVisual != null) _playerHomePos = PlayerVisual.Position;
        if (EnemyVisual != null) _enemyHomePos = EnemyVisual.Position;

        // Find the enemy body sprite (for playing idle/attack animations)
        if (EnemyVisual is AnimatedSprite2D sprite)
            _enemySprite = sprite;
        else if (EnemyVisual != null)
            _enemySprite = EnemyVisual.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D") ?? EnemyVisual.FindChild("*", true, false) as AnimatedSprite2D;

        // Hide the attack effect until it's needed
        if (EnemyAttackEffect != null) 
            EnemyAttackEffect.Visible = false;

        CreateJumpTimingIndicator();

        WireButton("SeedButton", PlayerAttacksType.Seed);
        WireButton("FireButton", PlayerAttacksType.Fire);
        WireButton("WaterButton", PlayerAttacksType.Water);
        WireButton("FleshButton", PlayerAttacksType.Flesh);

        ContinueButton.Pressed += () => _manager.CloseCombat();
        OutcomePanel.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_canPlayerJump && !_isPlayerJumping && Input.IsActionJustPressed("ui_accept") && JumpNum > 0)
        {
            if (!_isJumpWindowSweetSpot)
                ShowCueFeedback("TOO EARLY!", new Color(1f, 0.45f, 0.2f));
            else
                ShowCueFeedback("JUMP!", new Color(0.3f, 1f, 0.9f));

            _ = PerformPlayerJumpAsync();
            JumpNum = 0;
        }
    }


    private void CreateJumpTimingIndicator()
    {
        _jumpCueRoot = new Node2D
        {
            Name = "JumpTimingIndicator",
            ZIndex = 20,
            Visible = false
        };
        AddChild(_jumpCueRoot);

        // Background of the timing bar
        _barBg = new ColorRect
        {
            Color = new Color(0.12f, 0.12f, 0.15f, 0.9f),
            Position = new Vector2(-BarTotalWidth * 0.5f, 0),
            Size = new Vector2(BarTotalWidth, BarHeight)
        };
        _jumpCueRoot.AddChild(_barBg);

        // Green "Sweet Spot" zone on the right side of the bar
        float sweetSpotWidth = BarTotalWidth * 0.34f;
        _barSweetSpot = new ColorRect
        {
            Color = new Color(0.15f, 0.85f, 0.3f, 0.85f),
            Position = new Vector2((BarTotalWidth * 0.5f) - sweetSpotWidth, 0),
            Size = new Vector2(sweetSpotWidth, BarHeight)
        };
        _jumpCueRoot.AddChild(_barSweetSpot);

        // Moving progress fill bar
        _barFill = new ColorRect
        {
            Color = new Color(1f, 0.85f, 0.2f, 0.95f),
            Position = new Vector2(-BarTotalWidth * 0.5f, 2),
            Size = new Vector2(0, BarHeight - 4)
        };
        _jumpCueRoot.AddChild(_barFill);

        _jumpCueLabel = new Label
        {
            Text = "! WAIT...",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-90, -28),
            CustomMinimumSize = new Vector2(180, 24)
        };
        _jumpCueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        _jumpCueLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _jumpCueLabel.AddThemeConstantOverride("outline_size", 5);
        _jumpCueRoot.AddChild(_jumpCueLabel);

        _jumpCueRoot = new Node2D { Name = "JumpTimingIndicator", ZIndex = 20, Visible = false };
        AddChild(_jumpCueRoot);
        _barBg = new ColorRect { Color = new Color(0.12f, 0.12f, 0.15f, 0.9f), Position = new Vector2(-BarTotalWidth * 0.5f, 0), Size = new Vector2(BarTotalWidth, BarHeight) };
        _jumpCueRoot.AddChild(_barBg);
        _barSweetSpot = new ColorRect { Color = new Color(0.15f, 0.85f, 0.3f, 0.85f), Position = new Vector2((BarTotalWidth * 0.5f) - sweetSpotWidth, 0), Size = new Vector2(sweetSpotWidth, BarHeight) };
        _jumpCueRoot.AddChild(_barSweetSpot);
        _barFill = new ColorRect { Color = new Color(1f, 0.85f, 0.2f, 0.95f), Position = new Vector2(-BarTotalWidth * 0.5f, 2), Size = new Vector2(0, BarHeight - 4) };
        _jumpCueRoot.AddChild(_barFill);
        _jumpCueLabel = new Label { Text = "! WAIT...", HorizontalAlignment = HorizontalAlignment.Center, Position = new Vector2(-90, -28), CustomMinimumSize = new Vector2(180, 24) };
        _jumpCueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        _jumpCueLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _jumpCueLabel.AddThemeConstantOverride("outline_size", 5);
        _jumpCueRoot.AddChild(_jumpCueLabel);
    }

     private void ShowCueFeedback(string text, Color color)
    {
        if (_jumpCueLabel == null) return;
        _jumpCueLabel.Text = text;
        _jumpCueLabel.AddThemeColorOverride("font_color", color);
    }

    // ─── PLAYER ATTACKS ENEMY ─────────────────────────────────────────────

    public async Task<bool> PlayAttackSequenceAsync(CombatAction action)
        {
            if (action.IsPlayerAction)
                return await PlayPlayerAttackSequenceAsync(action);
            else
                return await PlayEnemyAttackSequenceAsync(action);
        }
    public async Task<bool> PlayPlayerAttackSequenceAsync(CombatAction action)
    {
        // Use the cached home position, not the live position!
        Vector2 enemyTarget = _enemyHomePos;

        if (PlayerVisual != null && EnemyVisual != null)
        {
            Vector2 attackSpot = enemyTarget + new Vector2(-70f, 0f);
            var lungeTween = CreateTween();
            lungeTween.TweenProperty(PlayerVisual, "position", attackSpot, action.StrikeSeconds)
                      .SetTrans(Tween.TransitionType.Sine)
                      .SetEase(Tween.EaseType.In);

            if (action.EnemyWillJump)
            {
                double jumpDelay = Mathf.Max(0.05, action.StrikeSeconds - JumpAscentTime);
                // Use the cached _enemyHomePos to guarantee it lands back on the ground
                _ = TriggerDelayedNodeJumpAsync(EnemyVisual, _enemyHomePos, jumpDelay);
            }

            await ToSignal(lungeTween, Tween.SignalName.Finished);
        }

        bool dodged = action.EnemyWillJump;

        // Return player to exact starting spot
        if (PlayerVisual != null)
        {
            var returnTween = CreateTween();
            returnTween.TweenProperty(PlayerVisual, "position", _playerHomePos, 0.35f)
                       .SetTrans(Tween.TransitionType.Quad)
                       .SetEase(Tween.EaseType.Out);
            await ToSignal(returnTween, Tween.SignalName.Finished);
        }

        return dodged;
    }

    // ─── ENEMY ATTACKS PLAYER ─────────────────────────────────────────────
    private async Task<bool> PlayEnemyAttackSequenceAsync(CombatAction action)
    {
        if (_jumpCueRoot != null)
        {
            _jumpCueRoot.Position = _playerHomePos + new Vector2(0, -110f);
            _jumpCueRoot.Scale = Vector2.One;
            _barFill.Size = new Vector2(0, BarHeight - 4);
            _barSweetSpot.Visible = action.CanBeJumped;
            _jumpCueRoot.Visible = true;
        }

        _canPlayerJump = action.CanBeJumped;
        _isJumpWindowSweetSpot = false;

        float windup = action.WindupSeconds;
        float strike = action.StrikeSeconds;

        Tween barTween = null;
        if (_barFill != null)
        {
            float windupWidth = BarTotalWidth * 0.66f;
            barTween = CreateTween();
            barTween.TweenProperty(_barFill, "size:x", windupWidth, windup).SetTrans(Tween.TransitionType.Linear);
            barTween.TweenProperty(_barFill, "size:x", BarTotalWidth, strike).SetTrans(Tween.TransitionType.Linear);
        }

        if (action.CanBeJumped)
        {
            ShowCueFeedback("! WAIT...", new Color(1f, 0.85f, 0.2f));
            _barFill.Color = new Color(1f, 0.85f, 0.2f, 0.95f);
        }
        else
        {
            ShowCueFeedback("UNJUMPABLE!", new Color(1f, 0.3f, 0.3f));
            _barFill.Color = new Color(0.9f, 0.2f, 0.2f, 0.95f);
        }

        // Play the enemy's body animation (windup)
        string animName = "attack_" + action.EnemyAttack.ToString().ToLower();
        PlayEnemyAnim(animName);

        // Make the enemy flash yellow for the windup
        if (EnemyVisual != null)
        {
            var flashTween = CreateTween();
            flashTween.TweenProperty(EnemyVisual, "modulate", new Color(1.4f, 1.3f, 0.5f), windup * 0.5f);
            flashTween.TweenProperty(EnemyVisual, "modulate", Colors.White, windup * 0.5f);
            await ToSignal(GetTree().CreateTimer(windup), SceneTreeTimer.SignalName.Timeout);
        }
        else
        {
            await ToSignal(GetTree().CreateTimer(windup), SceneTreeTimer.SignalName.Timeout);
        }

        _isJumpWindowSweetSpot = true;

        if (action.CanBeJumped && !_isPlayerJumping)
        {
            ShowCueFeedback("★ JUMP! [SPACE] ★", new Color(0.2f, 1f, 0.35f));
            _barFill.Color = new Color(0.2f, 1f, 0.35f, 1f);

            var pulseTween = CreateTween();
            pulseTween.TweenProperty(_jumpCueRoot, "scale", new Vector2(1.18f, 1.18f), 0.1f);
            pulseTween.TweenProperty(_jumpCueRoot, "scale", Vector2.One, 0.1f);
        }

        // ─── THE ATTACK EFFECT MOVES TOWARD THE PLAYER ───
        if (EnemyAttackEffect != null)
        {
            // 1. Determine which animation to play based on the attack type
            // (e.g., EnemyAttacksType.Fire becomes "attack_fire")
            string effectAnimName = "attack_" + action.EnemyAttack.ToString().ToLower();
            
            // 2. Play the animation safely
            if (EnemyAttackEffect.SpriteFrames != null && EnemyAttackEffect.SpriteFrames.HasAnimation(effectAnimName))
            {
                EnemyAttackEffect.Play(effectAnimName);
            }
            else
            {
                GD.PrintErr($"Missing animation '{effectAnimName}' on EnemyAttackEffect!");
            }

            // 3. Spawn slightly to the LEFT of the enemy (adjust -40f as needed)
            Vector2 spawnOffset = new Vector2(-90f, 0f); 
            EnemyAttackEffect.Position = _enemyHomePos + spawnOffset;
            EnemyAttackEffect.Visible = true;
            
            // 4. Target slightly to the RIGHT of the player's center (adjust +20f as needed)
            Vector2 targetOffset = new Vector2(20f, 0f);
            Vector2 targetPos = _playerHomePos + targetOffset;
            
            // 5. Tween the EFFECT toward the player
            var effectTween = CreateTween();
            effectTween.TweenProperty(EnemyAttackEffect, "position", targetPos, strike)
                       .SetTrans(Tween.TransitionType.Back)
                       .SetEase(Tween.EaseType.In);

            await ToSignal(effectTween, Tween.SignalName.Finished);
            
            // 6. Hide the effect when it hits
            EnemyAttackEffect.Visible = false;
        } 
        else
        {
            // Fallback if no effect is assigned
            await ToSignal(GetTree().CreateTimer(strike), SceneTreeTimer.SignalName.Timeout);
        }

        _canPlayerJump = false;
        _isJumpWindowSweetSpot = false;
        barTween?.Kill();

        bool playerDodged = action.CanBeJumped && _isPlayerAirborne;

        if (playerDodged)
        {
            ShowCueFeedback("DODGED!", new Color(0.2f, 1f, 0.5f));
        }
        else
        {
            ShowCueFeedback("HIT!", new Color(1f, 0.25f, 0.25f));
            if (PlayerVisual != null)
            {
                var hitFlash = CreateTween();
                hitFlash.TweenProperty(PlayerVisual, "modulate", new Color(1.6f, 0.4f, 0.4f), 0.12f);
                hitFlash.TweenProperty(PlayerVisual, "modulate", Colors.White, 0.15f);
            }
        }

        await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);

        // Reset enemy body animation back to idle
        PlayEnemyAnim("idle");

        if (_jumpCueRoot != null)
            _jumpCueRoot.Visible = false;

        JumpNum = 1;
        return playerDodged;
    }

    private async Task TriggerDelayedNodeJumpAsync(Node2D actor, Vector2 groundPos, double delaySeconds)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
        await AnimateJumpArcAsync(actor, groundPos);
    }

    private async Task PerformPlayerJumpAsync()
    {
        _isPlayerJumping = true;
        _isPlayerAirborne = true;

        if (PlayerVisual != null)
        {
            await AnimateJumpArcAsync(PlayerVisual, _playerHomePos);
        }
        else
        {
            double totalAirTime = JumpAscentTime + JumpHangTime + JumpDescentTime;
            await ToSignal(GetTree().CreateTimer(totalAirTime), SceneTreeTimer.SignalName.Timeout);
        }

        _isPlayerAirborne = false;
        _isPlayerJumping = false;
    }

    private async Task AnimateJumpArcAsync(Node2D actor, Vector2 groundPos)
    {
        if (!IsInstanceValid(actor)) return;

        Vector2 peakPos = groundPos + new Vector2(0f, -JumpHeight);

        var upTween = CreateTween();
        upTween.TweenProperty(actor, "position", peakPos, JumpAscentTime)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.Out);
        await ToSignal(upTween, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(JumpHangTime), SceneTreeTimer.SignalName.Timeout);

        var downTween = CreateTween();
        downTween.TweenProperty(actor, "position", groundPos, JumpDescentTime)
                 .SetTrans(Tween.TransitionType.Quad)
                 .SetEase(Tween.EaseType.In);
        await ToSignal(downTween, Tween.SignalName.Finished);
    }

    private void PlayEnemyAnim(string animName)
    {
        if (_enemySprite == null || _enemySprite.SpriteFrames == null) return;
        
        if (_enemySprite.SpriteFrames.HasAnimation(animName))
            _enemySprite.Play(animName);
        else if (_enemySprite.SpriteFrames.HasAnimation("idle"))
            _enemySprite.Play("idle");
    }

    private void WireButton(string nodeName, PlayerAttacksType type)
    {
        if (AttackButtons == null) return;
        var btn = AttackButtons.GetNodeOrNull<Button>(nodeName);
        if (btn != null)
        {
            btn.Pressed += () =>
            {
                var action = new CombatAction { IsPlayerAction = true, AttackName = type.ToString(), PlayerAttack = type };
                _manager.PlayerActionSelected(action);
            };
        }
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
        //if (PlayerMPBar != null) { PlayerMPBar.MaxValue = pMaxMp; PlayerMPBar.Value = pMp; }
        EnemyHPBar.MaxValue = eMax;
        EnemyHPBar.Value = e;
        EnemyHPLabel.Text = $"Enemy: {e} / {eMax}";
    }

    private void OnMessageUpdated(string msg) => MessageLabel.Text = msg;

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
        if (enabled && firstButton != null) firstButton.CallDeferred("grab_focus");
    }
}