using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CreatureInstance
{
    public const int MaxLevel = 3;
    public const int LevelTwoCopies = 3;
    public const int LevelThreeCopies = 6;
    [SerializeField] private string id;
    [SerializeField] private CreatureDataSO species;
    [SerializeField] private AttackDataSO[] equippedMoves = new AttackDataSO[2];
    [SerializeField] private CreatureAbilitySO ability;
    [SerializeField] private CreatureStats stats;
    [SerializeField] private CreatureItemSO heldItem;
    [SerializeField] private CreatureItemSO spray;
    [SerializeField] private int mergedCopies;
    [SerializeField] private bool consumed;

    public string Id => id;
    public CreatureDataSO Species => species;
    public IReadOnlyList<AttackDataSO> EquippedMoves => equippedMoves;
    public CreatureAbilitySO Ability => ability;
    public int CopyCount => 1 + mergedCopies;
    public int Level => CopyCount >= LevelThreeCopies ? 3 : CopyCount >= LevelTwoCopies ? 2 : 1;
    public bool IsConsumed => consumed;
    public string LevelSummary => ProgressSummary(CopyCount);
    public CreatureStats BaseStats => stats.AtLevel(Level);
    public CreatureStats Stats
    {
        get
        {
            return StatsAtCopies(CopyCount);
        }
    }
    public CreatureItemSO HeldItem => heldItem;
    public CreatureItemSO Spray => spray;

    private CreatureStats StatsAtCopies(int copies)
    {
        int level = copies >= LevelThreeCopies ? 3 : copies >= LevelTwoCopies ? 2 : 1;
        CreatureStats result = stats.AtLevel(level);
        if (heldItem != null) result = heldItem.ApplyStats(result);
        if (spray != null) result = spray.ApplyStats(result);
        return result;
    }

    private static string ProgressSummary(int copies)
    {
        if (copies >= LevelThreeCopies) return "Lv 3 · MAX";
        return copies >= LevelTwoCopies
            ? $"Lv 2 · {copies - LevelTwoCopies}/3 duplicates to Lv 3"
            : $"Lv 1 · {copies - 1}/2 duplicates to Lv 2";
    }

    public bool CanMerge(CreatureInstance donor) =>
        !consumed && donor != null && !donor.consumed && !ReferenceEquals(this, donor) &&
        species != null && species == donor.species && Level < MaxLevel;

    public string PreviewMergeProgress(CreatureInstance donor) =>
        CanMerge(donor) ? ProgressSummary(CopyCount + donor.CopyCount) : LevelSummary;

    public CreatureStats PreviewMergeStats(CreatureInstance donor) =>
        StatsAtCopies(CanMerge(donor) ? CopyCount + donor.CopyCount : CopyCount);

    public bool TryMerge(CreatureInstance donor)
    {
        if (!CanMerge(donor)) return false;
        mergedCopies = Mathf.Min(LevelThreeCopies, CopyCount + donor.CopyCount) - 1;
        donor.heldItem = null;
        donor.spray = null;
        donor.consumed = true;
        return true;
    }

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
