using System;

public static class RunShop
{
    public static readonly CreatureInstance[] Creatures = new CreatureInstance[3];
    public static readonly CreatureItemSO[] Items = new CreatureItemSO[2];
    public static readonly bool[] CreatureLocked = new bool[3];
    public static readonly bool[] ItemLocked = new bool[2];

    public static void Reset()
    {
        Array.Clear(Creatures, 0, Creatures.Length);
        Array.Clear(Items, 0, Items.Length);
        Array.Clear(CreatureLocked, 0, CreatureLocked.Length);
        Array.Clear(ItemLocked, 0, ItemLocked.Length);
    }
}
