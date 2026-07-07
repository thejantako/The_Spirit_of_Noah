using Godot;

public partial class EnemyBase : CharacterBody2D
{
    [ExportCategory("Movement")]
    [Export] public float MoveSpeed { get; set; } = 60f;
    [Export] public float Gravity { get; set; } = 1000f;

    [ExportCategory("Combat")]
    [Export] public int ContactDamage { get; set; } = 1;

    [ExportCategory("References")]
    [Export] public HealthComponent HealthComponent { get; set; }

    protected int Direction = 1;

    public override void _Ready()
    {
        if (HealthComponent == null)
        {
            HealthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        }

        if (HealthComponent != null)
        {
            HealthComponent.Died += OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        Vector2 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * dt;
        }

        velocity.X = Direction * MoveSpeed;

        Velocity = velocity;
        MoveAndSlide();
    }

    protected virtual void OnDied()
    {
        QueueFree();
    }

    public virtual void FlipDirection()
    {
        Direction *= -1;
        Scale = new Vector2(Direction, 1f);
    }

    public virtual void DealContactDamage(Node2D target)
    {
        HealthComponent targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(ContactDamage);
        }
    }
}