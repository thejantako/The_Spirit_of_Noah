using Godot;

public abstract partial class WeaponData : Resource
{
    [ExportCategory("Base Weapon Data")]
    [Export] public string WeaponId { get; set; } = "weapon_id";
    [Export] public string DisplayName { get; set; } = "Weapon";
    [Export] public Texture2D Icon { get; set; }

    [ExportCategory("Combat")]
    [Export] public int Damage { get; set; } = 1;
    [Export] public float Cooldown { get; set; } = 0.3f;
}