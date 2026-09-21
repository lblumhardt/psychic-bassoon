using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreatureInstance
{
    [SerializeField] private string id;
    [SerializeField] private CreatureDataSO species;
    [SerializeField] private AttackDataSO[] equippedMoves = new AttackDataSO[2];
    [SerializeField] private CreatureAbilitySO ability;
    [SerializeField] private CreatureStats stats;
    [SerializeField] private CreatureItemSO heldItem;
    [SerializeField] private CreatureItemSO spray;

    public string Id => id;
    public CreatureDataSO Species => species;
    public IReadOnlyList<AttackDataSO> EquippedMoves => equippedMoves;
    public CreatureAbilitySO Ability => ability;
    public CreatureStats BaseStats => stats;
    public CreatureStats Stats
    {
        get
        {
            CreatureStats result = stats;
            if (heldItem != null) result = heldItem.ApplyStats(result);
            if (spray != null) result = spray.ApplyStats(result);
            return result;
        }
    }
    public CreatureItemSO HeldItem => heldItem;
    public CreatureItemSO Spray => spray;

    public void Equip(CreatureItemSO item)
    {
        if (item == null) return;
        if (item.slot == CreatureItemSlot.HeldItem) heldItem = item;
        else spray = item;
    }

    public static CreatureInstance Generate(CreatureDataSO species)
    {
        if (species == null) return null;

        CreatureInstance instance = new CreatureInstance
        {
            id = Guid.NewGuid().ToString("N"),
            species = species,
            ability = species.RollAbility(),
            stats = species.baseStats.RollVariation()
        };

        List<AttackDataSO> choices = new();
        if (species.movePool != null)
        {
            foreach (AttackDataSO move in species.movePool)
            {
                if (move != null && move.behavior != null && !choices.Contains(move)) choices.Add(move);
            }
        }

        for (int slot = 0; slot < instance.equippedMoves.Length && choices.Count > 0; slot++)
        {
            int choice = UnityEngine.Random.Range(0, choices.Count);
            instance.equippedMoves[slot] = choices[choice];
            choices.RemoveAt(choice);
        }

        return instance;
    }
}
