using Godot;
using System.Threading.Tasks;
using Attacks;

public partial class CombatManager : Node
{
    public static CombatManager Instance { get; private set; }

    [Signal] public delegate void StateChangedEventHandler(int newState);
    // Added MP to the signal
    [Signal] public delegate void HealthUpdatedEventHandler(int p, int pMax, int pMp, int pMaxMp, int e, int eMax);
    [Signal] public delegate void MessageUpdatedEventHandler(string message);

    public CombatState CurrentState { get; private set; } = CombatState.Busy;

    private PlayerState _playerState;
    private EnemyDefinitionData _enemyDef;
    private Enemy1 _mapEnemy;
    private int _currentPlayerHp;
    private int _currentEnemyHp;

    public override void _EnterTree() => Instance = this;

    //Para leer los ataques del jugador
    public PlayerState GetPlayerState() => _playerState;

    public void Setup(PlayerState playerState, Enemy1 enemy)
    {
        _playerState = playerState;
        _enemyDef = enemy.enemyData;
        _mapEnemy = enemy;

        _currentPlayerHp = _playerState.CurrentHP;
        _currentEnemyHp = _enemyDef.MaxHP;

        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        CurrentState = CombatState.PlayerTurn;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);
        EmitSignal(SignalName.MessageUpdated, "What will you do?");
        EmitHealthUpdate();
    }

    public async void ExecuteAction(CombatAction action)
    {
        if (CurrentState != CombatState.PlayerTurn && action.IsPlayerAction) return;
        
        CurrentState = CombatState.Busy;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);

        if (action.IsPlayerAction)
        {
            _currentEnemyHp = Mathf.Max(0, _currentEnemyHp - action.Damage);
            EmitSignal(SignalName.MessageUpdated, $"Player used {action.AttackName} for {action.Damage} damage!");
        }
        else
        {
            _currentPlayerHp = Mathf.Max(0, _currentPlayerHp - action.Damage);
            EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} used {action.AttackName} for {action.Damage} damage!");
        }

        EmitHealthUpdate();
        await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);

        if (CheckCombatEnd()) return;

        if (action.IsPlayerAction)
            await ExecuteEnemyTurn();
        else
            StartPlayerTurn();
    }

    private async Task ExecuteEnemyTurn()
    {
        CurrentState = CombatState.EnemyTurn;
        EmitSignal(SignalName.StateChanged, (int)CurrentState);
        
        EmitSignal(SignalName.MessageUpdated, $"{_enemyDef.DisplayName} is thinking...");
        await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout); 

        // ENEMY AI: Pick a random attack from the enemy's resource
        var enemyAttacks = _enemyDef.Attacks?.attacks;
        
        if (enemyAttacks == null || enemyAttacks.Count == 0)
        {
            // Fallback if you forgot to assign attacks to the enemy
            ExecuteAction(new CombatAction { IsPlayerAction = false, AttackName = "Struggle", Damage = 2 });
            return;
        }

        var chosenAttack = enemyAttacks[GD.RandRange(0, enemyAttacks.Count - 1)];
        
        var action = new CombatAction
        {
            IsPlayerAction = false,
            AttackName = chosenAttack.attackType.ToString(),
            Damage = chosenAttack.Damage
        };
        
        ExecuteAction(action);
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
        _playerState.CurrentHP = _currentPlayerHp; // Save HP
        bool playerWon = CurrentState == CombatState.Victory;
        
        CombatTransition.Instance.EndCombat(playerWon, _mapEnemy);
        QueueFree();
    }
}