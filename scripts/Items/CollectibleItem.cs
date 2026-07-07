using Godot;

public partial class CollectibleItem : Area2D
{
    [ExportCategory("Data")]
    [Export] public ItemData ItemData { get; set; }

    [ExportCategory("Visual")]
    [Export] public Sprite2D Sprite { get; set; }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (Sprite == null)
            return;

        if (ItemData == null)
            return;

        if (ItemData.Icon != null)
        {
            Sprite.Texture = ItemData.Icon;
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        PlayerInventory inventory = body.GetNodeOrNull<PlayerInventory>("PlayerInventory");

        if (inventory == null)
            return;

        Collect(inventory, body);
    }

    private void Collect(PlayerInventory inventory, Node2D player)
    {
        if (ItemData == null)
        {
            GD.PushWarning("CollectibleItem hat keine ItemData.");
            return;
        }

        switch (ItemData.Type)
        {
            case CollectibleType.GenericItem:
                inventory.AddItem(ItemData.ItemId, ItemData.Amount);
                break;

            case CollectibleType.Ammo:
                inventory.AddAmmo(ItemData.ItemId, ItemData.Amount);
                break;

            case CollectibleType.MeleeWeapon:
                inventory.EquipMeleeWeapon(ItemData.MeleeWeapon);
                EquipWeaponOnPlayer(player, ItemData.MeleeWeapon);
                break;

            case CollectibleType.RangedWeapon:
                inventory.EquipRangedWeapon(ItemData.RangedWeapon);
                EquipWeaponOnPlayer(player, ItemData.RangedWeapon);
                break;

            case CollectibleType.Health:
                HealthComponent health = player.GetNodeOrNull<HealthComponent>("HealthComponent");

                if (health != null)
                {
                    health.Heal(ItemData.Amount);
                }

                break;
        }

        QueueFree();
    }

    private void EquipWeaponOnPlayer(Node2D player, MeleeWeaponData weapon)
    {
        WeaponHolder holder = player.GetNodeOrNull<WeaponHolder>("WeaponHolder");

        if (holder != null)
        {
            holder.EquipMeleeWeapon(weapon);
        }
    }

    private void EquipWeaponOnPlayer(Node2D player, RangedWeaponData weapon)
    {
        WeaponHolder holder = player.GetNodeOrNull<WeaponHolder>("WeaponHolder");

        if (holder != null)
        {
            holder.EquipRangedWeapon(weapon);
        }
    }
}