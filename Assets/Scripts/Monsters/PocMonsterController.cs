using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PocMonsterController : MonoBehaviour
{
    public float speed = 6.0f;
    private GameObject player;
    private bool isMoving = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");        
    }

    void Update()
    {

        if (!isMoving)
        {
            StartCoroutine(MoveMonster(player.transform.position));
        }
    }

    IEnumerator MoveMonster(Vector3 target)
    {
        isMoving = true;

        Vector3 directionX = Vector3.zero;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        Vector3 currentPos = transform.position;

        // Debug.Log("currentPos: " + currentPos);

        if (Mathf.Abs(currentPos.x - Mathf.Round(currentPos.x)) < 1f && Mathf.Abs(currentPos.z - Mathf.Round(currentPos.z)) < 1f)
        {
            // The monster is at the center of a square, so move to the next square
            Vector3 direction = (player.transform.position - transform.position).normalized;
            float max = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.z));
            direction = new Vector3(Mathf.Round(direction.x / max), 0, Mathf.Round(direction.z / max));
            Vector3 newTarget = transform.position + direction;

            // Create a Raycast in the direction of movement
            Ray ray = new Ray(transform.position, direction);
            RaycastHit hit;

            // If the Raycast hits an object with the tag "scenario", "monster", or "Player", try moving in a different direction
            if (Physics.Raycast(ray, out hit, direction.magnitude) && (hit.collider.tag == "scenario" || hit.collider.tag == "monster" || hit.collider.tag == "Player"))
            {
                // Try moving in a different direction
                direction = new Vector3(-direction.x, direction.y, -direction.z);
                newTarget = transform.position + direction;
            }

            while ((newTarget - transform.position).sqrMagnitude > Mathf.Epsilon)
            {
                // If the monster is touching the player, stop moving
                if (Physics.CheckSphere(transform.position, 0.5f, LayerMask.GetMask("Player") | LayerMask.GetMask("monster") | LayerMask.GetMask("scenario") ))
                {
                    yield return new WaitForSeconds(2);
                    yield break;
                }

                // Flip the sprite based on the direction of movement
                if (newTarget.x > transform.position.x)
                {
                    // Moving right
                    spriteRenderer.flipX = false;
                }
                else if (newTarget.x < transform.position.x)
                {
                    // Moving left
                    spriteRenderer.flipX = true;
                }

                float step = speed * Time.deltaTime; // calculate distance to move
                transform.position = Vector3.MoveTowards(transform.position, newTarget, step);
                yield return null;
            }
        }
        isMoving = false;
    }
}