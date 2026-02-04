using UnityEngine;

public class TorchController : MonoBehaviour
{
    public Light torchLight; // Assign this in the inspector
    public Transform playerTransform; // Assign this in the inspector
    void Start()
    {
        BoxCollider collider = GetComponent<BoxCollider>();

        if (collider != null)
        {
            // Ajusta o tamanho do colisor
            collider.size = new Vector3(0.55f, 1.1f, 0.55f);

            // Rotaciona o colisor
            collider.transform.rotation = Quaternion.identity;
            // Ajusta a posição do colisor
            collider.center = new Vector3(collider.center.x, collider.center.y, 0.35f);
        }
        else
        {
            Debug.LogWarning("BoxCollider component is missing");
        }
    }
    private void OnMouseDown()
    {
        // Check the distance between the player and the torch
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        // If the player is more than 1 unit away, do nothing
        if (distance > 1.5f)
        {
            return;
        }

        // Toggle light on click
        torchLight.enabled = !torchLight.enabled;
    }

    void Update()
    {
        // Check if the mouse is over the object and the right mouse button is clicked
        if (Input.GetMouseButtonDown(1) && IsMouseOver())
        {
            // Check the distance between the player and the torch
            float distance = Vector3.Distance(playerTransform.position, transform.position);

            // If the player is more than 1 unit away, do nothing
            if (distance > 1.5f)
            {
                return;
            }

            // Toggle light on click
            torchLight.enabled = !torchLight.enabled;
        }
    }

    private bool IsMouseOver()
    {
        // Cast a ray from the mouse position towards the object
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // If the ray hits this object, return true
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == transform)
            {
                return true;
            }
        }

        // Otherwise, return false
        return false;
    }
}