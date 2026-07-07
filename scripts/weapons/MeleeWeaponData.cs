using Godot;

[GlobalClass]
public partial class MeleeWeaponData : WeaponData
{
    [ExportCategory("Melee")]
    [Export] public float Range { get; set; } = 48f;
    [Export] public Vector2 HitboxSize { get; set; } = new Vector2(48f, 32f);
}