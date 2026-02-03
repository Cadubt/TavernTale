using UnityEngine;
using System.Collections;

namespace Player.Controllers
{
    /// <summary>
    /// Responsável exclusivamente pela movimentação tile-based do jogador
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Configurações de Movimento")]
        public float speed = 5.0f;
        public float diagonalSpeedMultiplier = 0.75f;
        
        [Header("Detecção de Terreno")]
        [SerializeField] private LayerMask groundLayer = -1; // -1 = Everything por padrão
        [SerializeField] private LayerMask elevatorLayer = -1;
        public bool checkFloorEnabled = true;
        
        // Estado do movimento
        private bool isMoving = false;
        private bool isFalling = false;
        private bool canMove = true;
        private Vector3 targetPosition;
        private float currentSpeed;
        private Vector3 lastMoveDirection = Vector3.forward;
        
        // Componentes
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        // Propriedades públicas
        public bool IsMoving => isMoving;
        public bool CanMove { get => canMove; set => canMove = value; }
        public Vector3 LastMoveDirection => lastMoveDirection;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            currentSpeed = speed;
            targetPosition = transform.position;
        }

        private void Update()
        {
            if (animator != null)
            {
                animator.SetBool("isWalking", isMoving);
            }
        }

        /// <summary>
        /// Tenta mover o jogador na direção especificada
        /// </summary>
        public void TryMove(Vector3 direction)
        {
            if (isMoving || !canMove) return;

            lastMoveDirection = direction;
            
            // Ajusta velocidade para diagonal
            bool isDiagonal = Mathf.Abs(direction.x) > 0.1f && Mathf.Abs(direction.z) > 0.1f;
            currentSpeed = isDiagonal ? speed * diagonalSpeedMultiplier : speed;

            // Verifica colisões
            Ray ray = new Ray(transform.position, direction);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 1f);
            
            Debug.DrawLine(ray.origin, ray.origin + direction * 1f, hasHit ? Color.red : Color.green, 0.5f);

            if (hasHit)
            {
                Debug.Log($"Colisão detectada: {hit.collider.name}, Tag: {hit.collider.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                
                // PRIMEIRO: Verifica bloqueios (paredes, monstros) - PRIORIDADE
                if (hit.collider.CompareTag("scenario") || hit.collider.CompareTag("monster"))
                {
                    Debug.Log($"Movimento BLOQUEADO por: {hit.collider.name} (Tag: {hit.collider.tag})");
                    return;
                }
                
                // SEGUNDO: Se não foi bloqueado, verifica se é elevador (por layer)
                int hitLayer = 1 << hit.collider.gameObject.layer;
                bool isElevator = (elevatorLayer.value & hitLayer) != 0;
                
                if (isElevator)
                {
                    Debug.Log($"Elevador detectado! Subindo de Y:{transform.position.y} para Y:{transform.position.y + 1f}");
                    float newY = transform.position.y + 1f;
                    targetPosition = new Vector3(transform.position.x, newY, transform.position.z) + direction;
                    StartCoroutine(Move(targetPosition));
                    return;
                }
            }

            targetPosition = transform.position + direction;
            StartCoroutine(Move(targetPosition));
        }

        private IEnumerator Move(Vector3 target)
        {
            isMoving = true;

            while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
            {
                // Flip do sprite baseado na direção
                if (spriteRenderer != null)
                {
                    if (target.x > transform.position.x)
                        spriteRenderer.flipX = true;
                    else if (target.x < transform.position.x)
                        spriteRenderer.flipX = false;
                }

                transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
            isMoving = false;
            
            // Verifica chão imediatamente após o movimento
            CheckFloor();
        }

        private void CheckFloor()
        {
            if (isMoving || isFalling || !checkFloorEnabled)
            {
                return;
            }

            Ray ray = new Ray(transform.position, Vector3.down);
            bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);

            Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, isGroundHit ? Color.green : Color.red, 0.5f);

            if (!isGroundHit)
            {
                Debug.Log($"Sem chão detectado em {transform.position} - Iniciando queda!");
                StartCoroutine(Fall());
            }
        }

        private IEnumerator Fall()
        {
            if (isFalling) yield break;

            isFalling = true;
            canMove = false;

            // Continua caindo até encontrar chão
            while (true)
            {
                Ray ray = new Ray(transform.position, Vector3.down);
                bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);
                
                Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, isGroundHit ? Color.green : Color.red, 0.5f);

                if (isGroundHit)
                {
                    Debug.Log($"Chão encontrado em {transform.position} - Parando queda");
                    break;
                }

                Debug.Log($"Caindo de Y:{transform.position.y} para Y:{transform.position.y - 1f}");
                float newY = transform.position.y - 1f;
                targetPosition = new Vector3(transform.position.x, newY, transform.position.z);
                
                yield return StartCoroutine(Move(targetPosition));
            }

            canMove = true;
            isFalling = false;
        }
    }
}
