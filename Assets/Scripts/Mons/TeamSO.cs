using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Mons/Team")]
public class TeamSO : ScriptableObject
{
    public Team team;
    public List<CreatureDataSO> creatures;
}
