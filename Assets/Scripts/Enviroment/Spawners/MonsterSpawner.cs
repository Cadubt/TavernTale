using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject monsterPrefab;
    
    private void Start()
    {
        SpawnMonster();
    }
    
    private void SpawnMonster()
    {
        Instantiate(monsterPrefab, transform.position, transform.rotation);
    }
}