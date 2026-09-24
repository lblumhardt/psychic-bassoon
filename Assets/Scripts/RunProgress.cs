using UnityEngine;

public static class RunProgress
{
    public const int WinsRequired = 10;
    public const int LossLimit = 3;

    public static int Wins { get; private set; }
    public static int Losses { get; private set; }
    public static bool HasWon => Wins >= WinsRequired;
    public static bool IsOver => HasWon || Losses >= LossLimit;
    public static string Summary => $"Wins {Wins}/{WinsRequired} · Losses {Losses}/{LossLimit}";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void StartNewRun()
    {
        Wins = 0;
        Losses = 0;
        RunRoster.Reset();
        OpponentRoster.Reset();
        BattleManager.ResetResult();
    }

    public static void RecordResult(RoundResult result)
    {
        if (IsOver) return;
        if (result == RoundResult.Win) Wins++;
        else if (result == RoundResult.Loss) Losses++;
    }
}
