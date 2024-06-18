using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform playerTransform;
    private Vector3 offset;
    private RaycastHit hit;

    private void Start()
    {
        // Calculate the initial offset.
        offset = transform.position - playerTransform.position;
    }

    private void LateUpdate()
    {
        // Update the camera's position.
        transform.position = playerTransform.position + offset;

        // Cast a ray from the camera to the player.
        if (Physics.Raycast(transform.position, (playerTransform.position - transform.position).normalized, out hit))
        {
            // If the ray hits an object, change the transparency of its material.
            if (hit.collider.gameObject != playerTransform.gameObject)
            {
                Color color = hit.collider.gameObject.GetComponent<MeshRenderer>().material.color;
                color.a = 0.4f; // 60% transparency
                hit.collider.gameObject.GetComponent<MeshRenderer>().material.color = color;
            }
        }
    }
}