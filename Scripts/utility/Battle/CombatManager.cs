using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using Attacks;

public partial class CombatManager : Node
{
    public static CombatManager Instance { get; private set; }

    [Signal] public delegate void StateChangedEventHandler(int newState);
    [Signal] public delegate void HealthUpdatedEventHandler(int p, int pMax, int pMp, int pMaxMp, int e, int eMax);
    [Signal] public delegate void MessageUpdatedEventHandler(string message);

    [Export] public double EnemyThinkDuration = 0.8;
    [Export] public double PostAttackDelay = 1.0;

    public CombatState CurrentState { get; private set; } = CombatState.Busy;

    private CombatScene _combatScene;
    private PlayerState _playerState;
    private EnemyDefinitionData _enemyDef;
    private Enemy1 _mapEnemy;

    private int _currentPlayerHp;
    private int _currentEnemyHp;

    private readonly Dictionary<PlayerAttacksType, int> _attackUsage = new();

    // --- ROUND BASED VARIABLES ---
    private CombatAction _cachedPlayerAction = null;
    private CombatAction _cachedEnemyAction = null;

    public override void _EnterTree() => Instance = this;
    public void RegisterScene(CombatScene scene) => _combatScene = scene;
    public PlayerState GetPlayerState() => _playerState;

    public void Setup(PlayerState playerState, Enemy1 enemy)
    {
        _playerState = playerState;
        _enemyDef = enemy.enemyData;
        _mapEnemy = enemy;

        _currentPlayerHp = _playerState.CurrentHP;
        _currentEnemyHp = _enemyDef.MaxHP;
        _attackUsage.Clear();

        StartRound();
    }

    // ─── PHASE 1: ACTION SELECTION ───────────────────────────────────────
    
    private async void StartRound()
    {
        _cachedPlayerAction = null;
        _cachedEnemyAction = null;

        // 1. Enemy (AI) picks its action
        await SelectEnemyAction();

        // 2. Player picks its action
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        CurrentState = CombatState.PlayerTurn;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);
        EmitSignal(SignalName.MessageUpdated, "What will you do?");
        EmitHealthUpdate();
    }

    private async Task SelectEnemyAction()
    {
        EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} is watching you...");
        await ToSignal(GetTree().CreateTimer(EnemyThinkDuration), SceneTreeTimer.SignalName.Timeout);

        var enemyAttacks = _enemyDef.Attacks?.attacks;
        if (enemyAttacks == null || enemyAttacks.Count == 0)
        {
            _cachedEnemyAction = new CombatAction { IsPlayerAction = false, AttackName = "Struggle", EnemyAttack = EnemyAttacksType.None };
            return;
        }

        var chosenAttack = enemyAttacks[GD.RandRange(0, enemyAttacks.Count - 1)];
        _cachedEnemyAction = new CombatAction
        {
            IsPlayerAction = false,
            AttackName = chosenAttack.attackType.ToString(),
            EnemyAttack = chosenAttack.attackType
        };
    }

    // Called by CombatScene when the player clicks a button
    public void PlayerActionSelected(CombatAction action)
    {
        if (CurrentState != CombatState.PlayerTurn) return;
        
        _cachedPlayerAction = action;
        
        // Both actions are selected! Move to execution phase.
        ExecuteRound();
    }

    // ─── PHASE 2: ACTION EXECUTION ───────────────────────────────────────

    private async void ExecuteRound()
    {
        CurrentState = CombatState.Busy;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);

        // 1. Sort by Speed (Higher speed goes first. Player wins ties.)
        bool playerGoesFirst = _playerState.Definition.Speed >= _enemyDef.Speed;

        if (playerGoesFirst)
        {
            if (_cachedPlayerAction != null) await ExecuteActionAsync(_cachedPlayerAction);
            if (CheckCombatEnd()) return;
            
            if (_cachedEnemyAction != null) await ExecuteActionAsync(_cachedEnemyAction);
            if (CheckCombatEnd()) return;
        }
        else
        {
            if (_cachedEnemyAction != null) await ExecuteActionAsync(_cachedEnemyAction);
            if (CheckCombatEnd()) return;
            
            if (_cachedPlayerAction != null) await ExecuteActionAsync(_cachedPlayerAction);
            if (CheckCombatEnd()) return;
        }

        // Both sides survived, start the next round
        await ToSignal(GetTree().CreateTimer(PostAttackDelay), SceneTreeTimer.SignalName.Timeout);
        StartRound();
    }

    private async Task ExecuteActionAsync(CombatAction action)
    {
        if (action.IsPlayerAction)
        {
            var profile = CombatDatabase.GetPlayerAttack(action.PlayerAttack);
            int timesUsedBefore = _attackUsage.TryGetValue(action.PlayerAttack, out int count) ? count : 0;

            action.AttackName = profile.DisplayName;
            action.CanBeJumped = profile.CanBeJumped;
            action.WindupSeconds = profile.WindupSeconds;
            action.StrikeSeconds = profile.StrikeSeconds;
            action.Damage = CalculatePlayerDamage(action.PlayerAttack, profile.BaseDamage);
            action.EnemyWillJump = RollEnemyJump(profile, timesUsedBefore);

            EmitSignal(SignalName.MessageUpdated, $"Player attacks with {action.AttackName}!");
        }
        else
        {
            var profile = CombatDatabase.GetEnemyAttack(action.EnemyAttack);
            action.AttackName = string.IsNullOrEmpty(action.AttackName) ? profile.DisplayName : action.AttackName;
            action.CanBeJumped = profile.CanBeJumped;
            action.WindupSeconds = profile.WindupSeconds;
            action.StrikeSeconds = profile.StrikeSeconds;
            action.Damage = CalculateEnemyDamage(profile.BaseDamage);

            string jumpHint = action.CanBeJumped ? " Watch the timing bar to JUMP!" : " Brace yourself!";
            EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} prepares {action.AttackName}!{jumpHint}");
        }

        // Play the visual sequence (Jump logic happens inside here)
        bool attackDodged = false;
        if (_combatScene != null)
        {
            attackDodged = await _combatScene.PlayAttackSequenceAsync(action);
        }

        // Apply Damage / Dodge
        if (action.IsPlayerAction)
        {
            if (attackDodged) EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} jumped over your {action.AttackName}!");
            else
            {
                _currentEnemyHp = Mathf.Max(0, _currentEnemyHp - action.Damage);
                EmitSignal(SignalName.MessageUpdated, $"Hit! {action.AttackName} dealt {action.Damage} damage!");
            }
        }
        else
        {
            if (attackDodged) EmitSignal(SignalName.MessageUpdated, $"NICE JUMP! You dodged {_enemyDef.DisplayName}'s {action.AttackName}!");
            else
            {
                _currentPlayerHp = Mathf.Max(0, _currentPlayerHp - action.Damage);
                EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} hit you with {action.AttackName} for {action.Damage} damage!");
            }
        }

        EmitHealthUpdate();
    }

    // ─── MATH & UTILITIES ────────────────────────────────────────────────

    private bool RollEnemyJump(AttackProfile profile, int timesUsedBefore)
    {
        if (!profile.CanBeJumped) return false;
        float jumpChance = Mathf.Clamp(profile.BaseEnemyJumpChance + (timesUsedBefore * 0.15f), 0f, 0.80f);
        return GD.Randf() < jumpChance;
    }

    private int CalculatePlayerDamage(PlayerAttacksType type, int baseDmg)
    {
        if (!_attackUsage.ContainsKey(type)) _attackUsage[type] = 0;
        int timesUsed = _attackUsage[type];
        _attackUsage[type]++;

        float multiplier = Mathf.Max(0.2f, 1.0f - (timesUsed * 0.2f));
        int adjustedDmg = Mathf.RoundToInt(baseDmg * multiplier);
        int variance = Mathf.RoundToInt(adjustedDmg * 0.2f);
        return Mathf.Max(1, GD.RandRange(adjustedDmg - variance, adjustedDmg + variance));
    }

    private int CalculateEnemyDamage(int baseDmg)
    {
        int variance = Mathf.RoundToInt(baseDmg * 0.2f);
        return Mathf.Max(1, GD.RandRange(baseDmg - variance, baseDmg + variance));
    }

    private bool CheckCombatEnd()
    {
        if (_currentEnemyHp <= 0)
        {
            CurrentState = CombatState.Victory;
            EmitSignal(SignalName.StateChanged, (int)CurrentState);
            EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} was defeated!");
            return true;
        }
        if (_currentPlayerHp <= 0)
        {
            CurrentState = CombatState.Defeat;
            EmitSignal(SignalName.StateChanged, (int)CurrentState);
            EmitSignal(SignalName.MessageUpdated, "You were defeated...");
            return true;
        }
        return false;
    }

    private void EmitHealthUpdate()
    {
        EmitSignal(SignalName.HealthUpdated,
            _currentPlayerHp, _playerState.Definition.MaxHP,
            _playerState.CurrentMP, _playerState.Definition.MaxMP,
            _currentEnemyHp, _enemyDef.MaxHP);
    }

    public void CloseCombat()
    {
        _playerState.CurrentHP = _currentPlayerHp;
        bool playerWon = CurrentState == CombatState.Victory;
        CombatTransition.Instance.EndCombat(playerWon, _mapEnemy);
        QueueFree();
    }
}