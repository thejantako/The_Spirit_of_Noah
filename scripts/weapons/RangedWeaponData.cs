using Godot;

[GlobalClass]
public partial class RangedWeaponData : WeaponData
{
    [ExportCategory("Ranged")]
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public Texture2D ShootTexture { get; set; }

    [ExportCategory("Ammo")]
    [Export] public string AmmoId { get; set; } = "basic_ammo";
    [Export] public int AmmoCost { get; set; } = 1;

    [ExportCategory("Projectile")]
    [Export] public float ProjectileSpeed { get; set; } = 520f;
}