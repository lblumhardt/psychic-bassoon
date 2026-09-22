using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Mons/Creature")]
public class CreatureDataSO : ScriptableObject
{
    public string creatureName;
    public Texture2D creatureTexture;
    public CreatureStats baseStats = new CreatureStats(10, 5, 5, 5, 5);

    [FormerlySerializedAs("attacks")]
    public List<AttackDataSO> movePool;
    public List<CreatureAbilitySO> potentialAbilities;

    public CreatureAbilitySO RollAbility()
    {
        if (potentialAbilities == null) return null;

        int count = 0;
        foreach (CreatureAbilitySO ability in potentialAbilities)
        {
            if (ability != null) count++;
        }
        if (count == 0) return null;

        int choice = Random.Range(0, count);
        foreach (CreatureAbilitySO ability in potentialAbilities)
        {
            if (ability == null) continue;
            if (choice-- == 0) return ability;
        }
        return null;
    }
}
