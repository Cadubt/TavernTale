using System.Collections;
using UnityEngine;

namespace Player.Controllers
{
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Configurações de Movimento")]
        public float speed = 1.0f;
        public float originalSpeed = 1.0f;

        [Header("Status Internos")]
        private Vector3 targetPosition;
        private bool isMoving = false;
        private bool canMoveSideways = true;

        [Header("Altura / Plataformas")]
        public float PlayerlevelHight = 0f;
        public float LastestObjectYPosition = 0f;

        // Guardamos a última direção de movimento para flip de sprite
        private Vector3 lastMoveDirection = Vector3.forward;

        private void Start()
        {
            // Define a velocidade original
            originalSpeed = speed;

            // Define a posição alvo como a posição inicial do personagem
            targetPosition = transform.position;

            // Se tiver um BoxCollider, por exemplo
            BoxCollider collider = GetComponent<BoxCollider>();
            // collider.transform.rotation = Quaternion.identity; // se precisar
        }

        private void Update()
        {
            // Se já está se movendo (executando a corrotina), não processa novas entradas
            if (isMoving) return;

            // Checa se deve descer quando não houver chão
            CheckIfOnGroundAndDescend();

            // Se não pode se mover lateralmente no momento (p.ex. caindo), aborta
            if (!canMoveSideways) return;

            // Zera direção a cada frame
            Vector3 direction = Vector3.zero;

            // Movimentação com W, A, S, D + diagonais (Q, E, Z, C)
            if (Input.GetKey(KeyCode.W))
            {
                speed = originalSpeed;
                direction += IsCollided(direction + Vector3.forward) ? Vector3.forward : Vector3.zero;
                lastMoveDirection = Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                speed = originalSpeed;
                direction += IsCollided(direction + Vector3.back) ? Vector3.back : Vector3.zero;
                lastMoveDirection = Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                speed = originalSpeed;
                direction += IsCollided(direction + Vector3.left) ? Vector3.left : Vector3.zero;
                lastMoveDirection = Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                speed = originalSpeed;
                direction += IsCollided(direction + Vector3.right) ? Vector3.right : Vector3.zero;
                lastMoveDirection = Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += IsCollided(direction + (Vector3.forward + Vector3.left))
                    ? (Vector3.forward + Vector3.left) : Vector3.zero;
                lastMoveDirection = Vector3.forward + Vector3.left;
                speed = originalSpeed / 1.5f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction += IsCollided(direction + (Vector3.forward + Vector3.right))
                    ? (Vector3.forward + Vector3.right) : Vector3.zero;
                lastMoveDirection = Vector3.forward + Vector3.right;
                speed = originalSpeed / 1.5f;
            }
            if (Input.GetKey(KeyCode.Z))
            {
                direction += IsCollided(direction + (Vector3.back + Vector3.left))
                    ? (Vector3.back + Vector3.left) : Vector3.zero;
                lastMoveDirection = Vector3.back + Vector3.left;
                speed = originalSpeed / 1.5f;
            }
            if (Input.GetKey(KeyCode.C))
            {
                direction += IsCollided(direction + (Vector3.back + Vector3.right))
                    ? (Vector3.back + Vector3.right) : Vector3.zero;
                lastMoveDirection = Vector3.back + Vector3.right;
                speed = originalSpeed / 1.5f;
            }
        }

        /// <summary>
        /// Faz um raycast na direção informada para detectar objetos.
        /// Se for "elevator", faz subir; caso contrário, move normalmente.
        /// Retorna true se colidiu com algo (que não seja "enviroment").
        /// </summary>
        private bool IsCollided(Vector3 direction)
        {
            Ray ray = new Ray(transform.position, direction);
            bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1f);

            Debug.DrawLine(ray.origin, ray.origin + direction * 1f, isHit ? Color.red : Color.green);

            if (isHit && hit.collider.tag != "enviroment")
            {
                // Se for "elevator", subimos
                if (hit.collider.CompareTag("elevator"))
                {
                    LastestObjectYPosition = hit.collider.transform.position.y;
                    PlayerlevelHight = LastestObjectYPosition + 1f;

                    targetPosition = new Vector3(targetPosition.x, PlayerlevelHight, targetPosition.z) + direction;
                    StartCoroutine(Move(targetPosition));
                }
            }
            else
            {
                // Se não colidiu ou colidiu com "enviroment" (que pode bloquear se quiser)
                // Aqui assumimos que "enviroment" bloqueia o movimento
                targetPosition += direction;
                StartCoroutine(Move(targetPosition));
            }
            return isHit;
        }

        /// <summary>
        /// Corrotina para mover o jogador gradualmente até 'target'.
        /// </summary>
        private IEnumerator Move(Vector3 target)
        {
            isMoving = true;
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
            {
                // Flip da sprite dependendo do lado
                if (spriteRenderer != null)
                {
                    if (target.x > transform.position.x)
                        spriteRenderer.flipX = false;
                    else if (target.x < transform.position.x)
                        spriteRenderer.flipX = true;
                }

                // Move na direção, multiplicado por 'speed'
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    speed * Time.deltaTime
                );
                yield return null;
            }
            isMoving = false;
        }

        /// <summary>
        /// Verifica se não há nada embaixo. Se não houver, desce 1 unidade no Y.
        /// </summary>
        private void CheckIfOnGroundAndDescend()
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f);
            Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, isGroundHit ? Color.red : Color.green);

            // Se não bateu em nada, inicia corrotina para descer
            if (!isGroundHit || hit.collider == null)
            {
                StartCoroutine(WaitAndMoveDown());
            }
        }

        private IEnumerator WaitAndMoveDown()
        {
            canMoveSideways = false;

            // Pode ajustar esse tempo, se quiser ver a queda
            yield return new WaitForSeconds(0.0001f);

            // "Cai" 1 unidade
            targetPosition = new Vector3(
                targetPosition.x,
                PlayerlevelHight - 1f,
                targetPosition.z
            );

            StartCoroutine(Move(targetPosition));
            canMoveSideways = true;
        }

        // Se quiser expor a última direção para outro script, você pode criar uma property
        public Vector3 GetLastMoveDirection()
        {
            return lastMoveDirection;
        }
    }
}
