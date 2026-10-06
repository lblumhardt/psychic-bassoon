using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class OpponentProgressionSmokeChecks
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [MenuItem("Tools/Mons/Check Opponent Progression")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run checks in edit mode.");
        var created = new List<UnityEngine.Object>();
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        try
        {
            OpponentRoster.Reset();
            var species = ScriptableObject.CreateInstance<CreatureDataSO>(); created.Add(species);
            var behavior = ScriptableObject.CreateInstance<StompBehaviorSO>(); created.Add(behavior);
            var move = ScriptableObject.CreateInstance<AttackDataSO>(); created.Add(move);
            move.behavior = behavior;
            species.movePool = new List<AttackDataSO> { move };
            var held = ScriptableObject.CreateInstance<CreatureItemSO>(); created.Add(held);
            held.slot = CreatureItemSlot.HeldItem; held.price = 2;
            var spray = ScriptableObject.CreateInstance<CreatureItemSO>(); created.Add(spray);
            spray.slot = CreatureItemSlot.Spray; spray.price = 2;
            var consumable = ScriptableObject.CreateInstance<CreatureItemSO>(); created.Add(consumable);
            consumable.slot = CreatureItemSlot.Consumable; consumable.price = 1;
            var items = new[] { held, spray, consumable };
            var pool = new[] { species };
            int previousCopies = 0;
            bool reachedLevelThree = false;
            for (int round = 1; round <= 20; round++)
            {
                OpponentRoster.PrepareForRound(pool, items);
                Require(OpponentRoster.LastSpent >= 0 && OpponentRoster.LastSpent <= 10, "CPU exceeded round budget.");
                Require(OpponentRoster.Members.Count == Mathf.Min(5, round + 2), "Unexpected roster growth.");
                int copies = 0, levelTwos = 0;
                foreach (CreatureInstance creature in OpponentRoster.Members)
                {
                    copies += creature.CopyCount;
                    Require(!creature.IsConsumed && creature.CopyCount <= CreatureInstance.LevelThreeCopies,
                        "Invalid merge receiver or excess copies.");
                    Require(creature.EquippedMoves[0] == move, "CPU lost its existing move.");
                    if (creature.Level >= 2)
                    {
                        levelTwos++;
                        Require(creature.IsMovePlus(0), "Leveling did not unlock the plus move.");
                    }
                    if (creature.Level == 3) reachedLevelThree = true;
                    Require(creature.HeldItem != consumable && creature.Spray != consumable,
                        "CPU equipped a consumable.");
                }
                Require(copies >= previousCopies, "CPU lost progression between rounds.");
                previousCopies = copies;
                if (round == 1) Require(copies == 4, "CPU should buy an upgrade in the first round.");
                if (round == 2) Require(levelTwos > 0, "CPU should have a level-two creature by round two.");
                if (round == 10) Require(reachedLevelThree, "CPU did not progress to level three.");
            }
            foreach (CreatureInstance creature in OpponentRoster.Members)
                Require(creature.Level == 3 && creature.HeldItem == held && creature.Spray == spray,
                    "CPU stopped investing before completing its team.");
            OpponentRoster.Reset();
            OpponentRoster.PrepareForRound(Array.Empty<CreatureDataSO>(), items);
            Require(OpponentRoster.Members.Count == 0 && OpponentRoster.LastSpent == 0, "Empty pools spent money.");
            Debug.Log("ALL OPPONENT PROGRESSION CHECKS PASSED");
        }
        finally
        {
            OpponentRoster.Reset();
            foreach (UnityEngine.Object obj in created) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Random.state = randomState;
        }
    }
}
