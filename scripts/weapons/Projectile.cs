using Godot;

public partial class Projectile : Area2D
{
    [ExportCategory("Projectile")]
    [Export] public float LifeTime { get; set; } = 2f;
    [Export] public bool DestroyOnHit { get; set; } = true;

    private int _direction = 1;
    private float _speed = 500f;
    private int _damage = 1;
    private float _lifeTimer;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;

        _lifeTimer = LifeTime;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        GlobalPosition += new Vector2(_direction * _speed * dt, 0f);

        _lifeTimer -= dt;

        if (_lifeTimer <= 0f)
        {
            QueueFree();
        }
    }

    public void Setup(int direction, float speed, int damage)
    {
        _direction = direction >= 0 ? 1 : -1;
        _speed = speed;
        _damage = damage;

        Scale = new Vector2(_direction, 1f);
    }

    private void OnBodyEntered(Node2D body)
    {
        TryDamage(body);
    }

    private void OnAreaEntered(Area2D area)
    {
        TryDamage(area);
    }

    private void TryDamage(Node node)
    {
        HealthComponent health = FindHealthComponent(node);

        if (health != null)
        {
            health.TakeDamage(_damage);

            if (DestroyOnHit)
            {
                QueueFree();
            }
        }
    }

    private HealthComponent FindHealthComponent(Node node)
    {
        if (node == null)
            return null;

        HealthComponent health = node.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (health != null)
            return health;

        Node parent = node.GetParent();

        if (parent == null)
            return null;

        return parent.GetNodeOrNull<HealthComponent>("HealthComponent");
    }
}