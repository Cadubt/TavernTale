using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 1.0f;
    private Vector3 targetPosition;
    private bool isMoving;

    private void Start()
    {
        targetPosition = transform.position;
        isMoving = false;
    }

    private void Update()
    {
        Vector3 direction = Vector3.zero;
        if (isMoving)
            return;

        // Detectar um clique do mouse
        if (Input.GetMouseButtonDown(0))
        {
            // Criar um raio da câmera até a posição do mouse
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Se o raio atingir o chão, definir a direção para o ponto onde o mouse clicou
            if (Physics.Raycast(ray, out hit) && hit.collider.tag == "Ground")
            {
                Vector3 targetPosition = hit.point;
                targetPosition.x = Mathf.RoundToInt(targetPosition.x);
                // targetPosition.y = Mathf.RoundToInt(targetPosition.y);
                targetPosition.z = Mathf.RoundToInt(targetPosition.z);

                direction = targetPosition - transform.position;
            }
        }


        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            // Use o material aqui
        }
        else
        {
            Debug.LogError("No MeshRenderer attached to this game object.");
        }


        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector3.right;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            direction += Vector3.forward + Vector3.left;
        }
        if (Input.GetKey(KeyCode.E))
        {
            direction += Vector3.forward + Vector3.right;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            direction += Vector3.back + Vector3.left;
        }
        if (Input.GetKey(KeyCode.C))
        {
            direction += Vector3.back + Vector3.right;
        }
        if (direction != Vector3.zero)
        {
            // Lançar um raio na direção do movimento do jogador
            Ray ray = new Ray(transform.position, direction);
            RaycastHit hit;

            // Imprimir uma mensagem no console quando o raio é desenhado
            Debug.Log("Drawing ray from " + ray.origin + " in direction " + ray.direction);

            // Desenhar o raio para fins de depuração
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * direction.magnitude, Color.red, 2f);

            // Se o raio atingir algo com a tag "scenario" dentro da distância do passo do jogador, não mover o jogador
            if (Physics.Raycast(ray, out hit, direction.magnitude) && hit.collider.tag == "scenario")
            {
                float objectHeight = hit.collider.bounds.size.y;

                // Calcular a posição Y do topo do objeto
                float topYPosition = hit.collider.bounds.center.y + objectHeight / 2;

                // Imprimir a altura do objeto e a posição Y do topo do objeto
                Debug.Log("Object height: " + objectHeight);

                return;
            }

            targetPosition += direction;
            StartCoroutine(MovePlayer(targetPosition));
        }


    }

    IEnumerator MovePlayer(Vector3 target)
    {
        isMoving = true;
        while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        isMoving = false;
    }
}