using System.Collections.Generic;
using UnityEngine;

public class CreatureRegistry : MonoBehaviour
{
    public List<CreatureController> playerCreatures = new();
    public List<CreatureController> enemyCreatures = new();
}
