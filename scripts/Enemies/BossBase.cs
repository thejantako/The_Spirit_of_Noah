using Godot;

public enum BossState
{
    Moving,
    Attacking
}

public partial class BossBase : CharacterBody2D
{
    [ExportCategory("Stats")]
    [Export] public HealthComponent HealthComponent { get; set; }

    [ExportCategory("Movement")]
    [Export] public float MoveSpeedPhaseOne { get; set; } = 80f;
    [Export] public float MoveSpeedPhaseTwo { get; set; } = 120f;
    [Export] public float Gravity { get; set; } = 1000f;

    [ExportCategory("Attack")]
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public Node2D ProjectileSpawn { get; set; }
    [Export] public float ProjectileSpeed { get; set; } = 360f;
    [Export] public int ProjectileDamage { get; set; } = 1;
    [Export] public float AttackInterval { get; set; } = 1.2f;

    private BossState _state = BossState.Moving;
    private int _phase = 1;
    private int _direction = 1;

    private float _stateTimer = 2f;
    private float _attackTimer;

    public override void _Ready()
    {
        if (HealthComponent == null)
        {
            HealthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        }

        if (HealthComponent != null)
        {
            HealthComponent.HealthChanged += OnHealthChanged;
            HealthComponent.Died += OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        UpdatePhase();
        UpdateState(dt);

        MoveAndSlide();
    }

    private void UpdatePhase()
    {
        if (HealthComponent == null)
            return;

        if (_phase == 1 && HealthComponent.CurrentHealth <= HealthComponent.MaxHealth / 2)
        {
            _phase = 2;
            GD.Print("Boss wechselt in Phase 2.");
        }
    }

    private void UpdateState(float delta)
    {
        _stateTimer -= delta;

        switch (_state)
        {
            case BossState.Moving:
                HandleMovement(delta);

                if (_stateTimer <= 0f)
                {
                    ChangeState(BossState.Attacking);
                }

                break;

            case BossState.Attacking:
                HandleAttack(delta);

                if (_stateTimer <= 0f)
                {
                    ChangeState(BossState.Moving);
                }

                break;
        }
    }

    private void HandleMovement(float delta)
    {
        Vector2 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * delta;
        }

        float speed = _phase == 1 ? MoveSpeedPhaseOne : MoveSpeedPhaseTwo;

        velocity.X = _direction * speed;

        Velocity = velocity;
    }

    private void HandleAttack(float delta)
    {
        Vector2 velocity = Velocity;
        velocity.X = 0f;

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * delta;
        }

        Velocity = velocity;

        _attackTimer -= delta;

        if (_attackTimer <= 0f)
        {
            ShootProjectile();
            _attackTimer = AttackInterval;
        }
    }

    private void ShootProjectile()
    {
        if (ProjectileScene == null)
            return;

        if (ProjectileSpawn == null)
            return;

        Projectile projectile = ProjectileScene.Instantiate<Projectile>();

        GetTree().CurrentScene.AddChild(projectile);

        projectile.GlobalPosition = ProjectileSpawn.GlobalPosition;
        projectile.Setup(_direction, ProjectileSpeed, ProjectileDamage);
    }

    private void ChangeState(BossState newState)
    {
        _state = newState;

        if (_state == BossState.Moving)
        {
            _stateTimer = _phase == 1 ? 2.5f : 1.5f;
        }
        else
        {
            _stateTimer = _phase == 1 ? 1.5f : 2.5f;
            _attackTimer = 0f;
        }
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        GD.Print($"Boss HP: {currentHealth}/{maxHealth}");
    }

    private void OnDied()
    {
        GD.Print("Boss besiegt.");
        QueueFree();
    }

    public void FlipDirection()
    {
        _direction *= -1;
        Scale = new Vector2(_direction, 1f);
    }
}