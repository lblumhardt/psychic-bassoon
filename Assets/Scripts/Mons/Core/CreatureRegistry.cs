using System.Collections.Generic;
using UnityEngine;

public class CreatureRegistry : MonoBehaviour
{
    public BattlePerks Perks { get; set; }
    public List<CreatureController> playerCreatures = new();
    public List<CreatureController> enemyCreatures = new();

    public void Register(CreatureController creature)
    {
        if (creature == null)
        {
            return;
        }

        List<CreatureController> targetList = creature.Team == Team.Player ? playerCreatures : enemyCreatures;
        if (!targetList.Contains(creature))
        {
            targetList.Add(creature);
        }
    }

    public void Unregister(CreatureController creature)
    {
        if (creature == null)
        {
            return;
        }

        playerCreatures.Remove(creature);
        enemyCreatures.Remove(creature);
    }

    public IReadOnlyList<CreatureController> GetOpponents(Team team)
    {
        return team == Team.Player ? enemyCreatures : playerCreatures;
    }

    public IReadOnlyList<CreatureController> GetAllies(Team team) =>
        team == Team.Player ? playerCreatures : enemyCreatures;

    public void NotifyKnockout(CreatureController victim, CreatureController source)
    {
        var participants = new List<CreatureController>(playerCreatures);
        participants.AddRange(enemyCreatures);
        foreach (var creature in participants)
            if (creature != null) creature.AbilityRuntime?.OnKnockout(victim, source);
    }

}
