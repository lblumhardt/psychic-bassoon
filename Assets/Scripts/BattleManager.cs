using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private CreatureRegistry _creatureRegistry;

    private void Awake()
    {
        GameObject registryObject = new GameObject("CreatureRegistry");
        registryObject.transform.SetParent(transform);
        _creatureRegistry = registryObject.AddComponent<CreatureRegistry>();
    }

    void Start()
    {
        CreatureController[] creatures = FindObjectsByType<CreatureController>(FindObjectsSortMode.None);
        foreach (CreatureController creature in creatures)
        {
            creature.SetRegistry(_creatureRegistry);
        }
    }
}
