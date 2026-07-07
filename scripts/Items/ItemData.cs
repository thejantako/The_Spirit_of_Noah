using Godot;

public enum CollectibleType
{
    GenericItem,
    Ammo,
    MeleeWeapon,
    RangedWeapon,
    Health
}

[GlobalClass]
public partial class ItemData : Resource
{
    [ExportCategory("Item")]
    [Export] public string ItemId { get; set; } = "item_id";
    [Export] public string DisplayName { get; set; } = "Item";
    [Export] public Texture2D Icon { get; set; }

    [ExportCategory("Collectible")]
    [Export] public CollectibleType Type { get; set; } = CollectibleType.GenericItem;
    [Export] public int Amount { get; set; } = 1;

    [ExportCategory("Weapons")]
    [Export] public MeleeWeaponData MeleeWeapon { get; set; }
    [Export] public RangedWeaponData RangedWeapon { get; set; }
}