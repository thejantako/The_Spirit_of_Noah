using Godot;
using Godot.Collections;

public partial class WeaponHolder : Node2D
{
    [ExportCategory("Weapons")]
    [Export] public MeleeWeaponData EquippedMeleeWeapon { get; set; }
    [Export] public RangedWeaponData EquippedRangedWeapon { get; set; }

    [ExportCategory("References")]
    [Export] public PlayerInventory Inventory { get; set; }

    private float _meleeCooldownTimer;
    private float _rangedCooldownTimer;

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_meleeCooldownTimer > 0f)
        {
            _meleeCooldownTimer -= dt;
        }

        if (_rangedCooldownTimer > 0f)
        {
            _rangedCooldownTimer -= dt;
        }
    }

    public void EquipMeleeWeapon(MeleeWeaponData weapon)
    {
        EquippedMeleeWeapon = weapon;

        if (Inventory != null)
        {
            Inventory.EquipMeleeWeapon(weapon);
        }
    }

    public void EquipRangedWeapon(RangedWeaponData weapon)
    {
        EquippedRangedWeapon = weapon;

        if (Inventory != null)
        {
            Inventory.EquipRangedWeapon(weapon);
        }
    }

    public void UseMeleeWeapon(int direction)
    {
        if (EquippedMeleeWeapon == null)
            return;

        if (_meleeCooldownTimer > 0f)
            return;

        _meleeCooldownTimer = EquippedMeleeWeapon.Cooldown;

        Vector2 hitCenter = GlobalPosition + new Vector2(direction * EquippedMeleeWeapon.Range, 0f);

        DealAreaDamage(
            hitCenter,
            EquippedMeleeWeapon.HitboxSize,
            EquippedMeleeWeapon.Damage
        );
    }

    public void UseRangedWeapon(int direction)
    {
        if (EquippedRangedWeapon == null)
            return;

        if (_rangedCooldownTimer > 0f)
            return;

        if (Inventory == null)
            return;

        if (!Inventory.HasAmmo(EquippedRangedWeapon.AmmoId, EquippedRangedWeapon.AmmoCost))
            return;

        if (EquippedRangedWeapon.ProjectileScene == null)
            return;

        bool ammoRemoved = Inventory.RemoveAmmo(
            EquippedRangedWeapon.AmmoId,
            EquippedRangedWeapon.AmmoCost
        );

        if (!ammoRemoved)
            return;

        _rangedCooldownTimer = EquippedRangedWeapon.Cooldown;

        Projectile projectile = EquippedRangedWeapon.ProjectileScene.Instantiate<Projectile>();
        GetTree().CurrentScene.AddChild(projectile);

        projectile.GlobalPosition = GlobalPosition;
        projectile.Setup(
            direction,
            EquippedRangedWeapon.ProjectileSpeed,
            EquippedRangedWeapon.Damage
        );
    }

    private void DealAreaDamage(Vector2 center, Vector2 size, int damage)
    {
        RectangleShape2D shape = new RectangleShape2D
        {
            Size = size
        };

        PhysicsShapeQueryParameters2D query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0f, center),
            CollideWithAreas = true,
            CollideWithBodies = true
        };

        Array<Dictionary> results = GetWorld2D().DirectSpaceState.IntersectShape(query);

        foreach (Dictionary result in results)
        {
            if (!result.ContainsKey("collider"))
                continue;

            Node collider = result["collider"].As<Node>();
            HealthComponent health = FindHealthComponent(collider);

            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }
    }

    private HealthComponent FindHealthComponent(Node node)
    {
        if (node == null)
            return null;

        HealthComponent ownHealth = node.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (ownHealth != null)
            return ownHealth;

        Node parent = node.GetParent();

        if (parent == null)
            return null;

        return parent.GetNodeOrNull<HealthComponent>("HealthComponent");
    }
}