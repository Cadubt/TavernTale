using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicController : MonoBehaviour
{
    public GameObject cubePrefab; // Prefab do cubo
    public float cubeLifetime = 2f; // Tempo de vida dos cubos em segundos

    public void CreateMagicCubes(Vector3 startPosition, Vector3 direction, int numberOfCubes, float lifetime)
    {
        for (int i = 0; i < numberOfCubes; i++)
        {
            Vector3 cubePosition = startPosition + direction * i;
            GameObject cube = Instantiate(cubePrefab, cubePosition, Quaternion.identity);
            StartCoroutine(DestroyCubeAfterTime(cube, lifetime));
        }
    }

    private IEnumerator DestroyCubeAfterTime(GameObject cube, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(cube);
    }
}