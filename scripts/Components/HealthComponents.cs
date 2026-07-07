using Godot;

public partial class HealthComponent : Node
{
    [Signal] public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void DiedEventHandler();

    [ExportCategory("Health")]
    [Export] public int MaxHealth { get; set; } = 5;
    [Export] public bool DestroyOwnerOnDeath { get; set; } = true;

    public int CurrentHealth { get; private set; }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);

        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    private void Die()
    {
        EmitSignal(SignalName.Died);

        if (DestroyOwnerOnDeath && Owner != null)
        {
            Owner.QueueFree();
        }
    }
}