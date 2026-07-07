using Godot;
using System.Collections.Generic;

public partial class PlayerInventory : Node
{
    private readonly Dictionary<string, int> _items = new();
    private readonly Dictionary<string, int> _ammo = new();

    public MeleeWeaponData EquippedMeleeWeapon { get; private set; }
    public RangedWeaponData EquippedRangedWeapon { get; private set; }

    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        if (!_items.ContainsKey(itemId))
        {
            _items[itemId] = 0;
        }

        _items[itemId] += amount;

        GD.Print($"Item erhalten: {itemId} x{amount}");
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return _items.ContainsKey(itemId) && _items[itemId] >= amount;
    }

    public int GetItemAmount(string itemId)
    {
        if (!_items.ContainsKey(itemId))
            return 0;

        return _items[itemId];
    }

    public void AddAmmo(string ammoId, int amount)
    {
        if (string.IsNullOrWhiteSpace(ammoId))
            return;

        if (!_ammo.ContainsKey(ammoId))
        {
            _ammo[ammoId] = 0;
        }

        _ammo[ammoId] += amount;

        GD.Print($"Munition erhalten: {ammoId} x{amount}");
    }

    public bool HasAmmo(string ammoId, int amount)
    {
        return _ammo.ContainsKey(ammoId) && _ammo[ammoId] >= amount;
    }

    public bool RemoveAmmo(string ammoId, int amount)
    {
        if (!HasAmmo(ammoId, amount))
            return false;

        _ammo[ammoId] -= amount;
        return true;
    }

    public int GetAmmoAmount(string ammoId)
    {
        if (!_ammo.ContainsKey(ammoId))
            return 0;

        return _ammo[ammoId];
    }

    public void EquipMeleeWeapon(MeleeWeaponData weapon)
    {
        if (weapon == null)
            return;

        EquippedMeleeWeapon = weapon;
        GD.Print($"Nahkampfwaffe ausgerüstet: {weapon.DisplayName}");
    }

    public void EquipRangedWeapon(RangedWeaponData weapon)
    {
        if (weapon == null)
            return;

        EquippedRangedWeapon = weapon;
        GD.Print($"Fernkampfwaffe ausgerüstet: {weapon.DisplayName}");
    }
}