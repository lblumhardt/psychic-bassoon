using UnityEngine;

public class TeamManager : MonoBehaviour
{
    public TeamSO playerTeam;
    public TeamSO enemyTeam;
    public Transform playerTeamSpawnPoint;
    public Transform enemyTeamSpawnPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3 spawnPoint = playerTeamSpawnPoint.position;
        Debug.Log("Spawning player team");
        foreach (CreatureDataSO creature in playerTeam.creatures) {
            GameObject creatureObject = Instantiate(creature.creaturePrefab, spawnPoint, Quaternion.identity);
            creatureObject.GetComponent<CreatureController>().Initialize(creature);
            spawnPoint += new Vector3(0, 0, 3);
        }
        spawnPoint = enemyTeamSpawnPoint.position;
        Debug.Log("Spawning enemy team");
        foreach (CreatureDataSO creature in enemyTeam.creatures) {
            GameObject creatureObject = Instantiate(creature.creaturePrefab, spawnPoint, Quaternion.identity);
            creatureObject.GetComponent<CreatureController>().Initialize(creature);
            spawnPoint += new Vector3(0, 0, 3);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
