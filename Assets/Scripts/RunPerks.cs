using System.Collections.Generic;
using UnityEngine;

public static class RunPerks
{
    public const int DraftInterval = 3;
    private static readonly List<TeamPerkSO> selected = new();
    private static readonly List<TeamPerkSO> choices = new();
    private static int lastDraftRound;
    public static IReadOnlyList<TeamPerkSO> Selected => selected;
    public static IReadOnlyList<TeamPerkSO> Choices => choices;
    public static string Summary => selected.Count == 0 ? "Team perks: none · Choose a perk every 3 rounds" :
        "Team perks: " + string.Join(" · ", selected.ConvertAll(perk => perk.displayName));
    public static void Reset() { selected.Clear(); choices.Clear(); lastDraftRound = 0; }
    public static void PrepareDraft()
    {
        int rounds = RunProgress.Wins + RunProgress.Losses;
        if (RunProgress.IsOver || rounds == 0 || rounds % DraftInterval != 0 || rounds <= lastDraftRound || choices.Count > 0) return;
        List<TeamPerkSO> pool = new(Resources.LoadAll<TeamPerkSO>("Perks"));
        pool.RemoveAll(perk => perk == null || selected.Contains(perk));
        while (choices.Count < 3 && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            choices.Add(pool[index]);
            pool.RemoveAt(index);
        }
        if (choices.Count == 0) lastDraftRound = rounds;
    }
    public static bool Choose(TeamPerkSO perk)
    {
        if (RunProgress.IsOver || !choices.Contains(perk)) return false;
        selected.Add(perk);
        choices.Clear();
        lastDraftRound = RunProgress.Wins + RunProgress.Losses;
        return true;
    }
}
