using System.Collections.Generic;
using UnityEngine;

public class CreatureRegistry : MonoBehaviour
{
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

}
