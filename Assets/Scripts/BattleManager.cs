using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private CreatureRegistry _creatureRegistry;
    private bool _isRoundOver = false;

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

    // TODO should be delegate/event
    void Update() {
        if (_isRoundOver) {
            return;
        }

        if (_creatureRegistry.playerCreatures.Count == 0 ) {
            Debug.Log("You Lose");
            _isRoundOver = true;
        } else if (_creatureRegistry.enemyCreatures.Count == 0 ) {
            Debug.Log("You Win!");
            _isRoundOver = true;
        }
    }
}
