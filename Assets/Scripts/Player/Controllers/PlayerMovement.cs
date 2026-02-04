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
        
        // Sistema de clique e pathfinding
        [Header("Movimentação por Clique")]
        [SerializeField] private bool enableClickMovement = true;
        private bool isFollowingPath = false;
        private System.Collections.Generic.Queue<Vector3> pathQueue = new System.Collections.Generic.Queue<Vector3>();
        private Camera mainCamera;
        
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
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (animator != null)
            {
                animator.SetBool("isWalking", isMoving);
            }
            
            // Processa movimentação por clique
            if (enableClickMovement)
            {
                ProcessClickMovement();
            }
            
            // Processa fila de caminho
            if (isFollowingPath && !isMoving && canMove && pathQueue.Count > 0)
            {
                Vector3 nextPosition = pathQueue.Dequeue();
                
                // Calcula direção tile-based (sempre 1 unidade nos eixos)
                Vector3 currentTile = new Vector3(
                    Mathf.Round(transform.position.x),
                    transform.position.y,
                    Mathf.Round(transform.position.z)
                );
                
                Vector3 direction = nextPosition - currentTile;
                
                // Garante que a direção seja exatamente 1 unidade nos eixos principais
                direction.x = Mathf.Clamp(Mathf.Round(direction.x), -1, 1);
                direction.y = 0;
                direction.z = Mathf.Clamp(Mathf.Round(direction.z), -1, 1);
                
                Debug.Log($"Movendo de {currentTile} para {nextPosition} - Direção: {direction}");
                
                if (direction != Vector3.zero)
                {
                    TryMove(direction);
                }
            }
            else if (pathQueue.Count == 0)
            {
                isFollowingPath = false;
            }
        }

        /// <summary>
        /// Cancela o caminho atual (útil quando player usa teclado durante movimento por clique)
        /// </summary>
        public void CancelPath()
        {
            pathQueue.Clear();
            isFollowingPath = false;
        }
        
        /// <summary>
        /// Tenta mover o jogador usando input do teclado (cancela pathfinding)
        /// </summary>
        public void TryMoveFromKeyboard(Vector3 direction)
        {
            if (isFollowingPath)
            {
                CancelPath();
            }
            TryMove(direction);
        }
        
        /// <summary>
        /// Apenas aponta o personagem para a direção sem se mover (Ctrl + direção)
        /// </summary>
        public void PointToDirection(Vector3 direction)
        {
            if (isMoving) return;
            
            // Cancela pathfinding se estiver ativo
            if (isFollowingPath)
            {
                CancelPath();
            }
            
            lastMoveDirection = direction;
            
            // Atualiza o flip do sprite baseado na direção
            if (spriteRenderer != null)
            {
                if (direction.x > 0)
                    spriteRenderer.flipX = true;
                else if (direction.x < 0)
                    spriteRenderer.flipX = false;
            }
            
            Debug.Log($"Apontando para direção: {direction}");
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
        
        /// <summary>
        /// Processa clique do mouse para movimentação
        /// </summary>
        private void ProcessClickMovement()
        {
            if (!Input.GetMouseButtonDown(0) || mainCamera == null)
                return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Faz raycast ignorando scenarios (atravessa paredes para detectar chão)
            RaycastHit[] hits = Physics.RaycastAll(ray);
            
            // Procura por chão ou elevador nos hits, ignorando scenarios
            foreach (RaycastHit hit in hits)
            {
                // Verifica se clicou em chão ou elevador
                int hitLayer = 1 << hit.collider.gameObject.layer;
                bool isGround = (groundLayer.value & hitLayer) != 0;
                bool isElevator = (elevatorLayer.value & hitLayer) != 0;
                
                if (isGround || isElevator)
                {
                    Vector3 clickPosition = hit.point;
                    
                    // Arredonda para o tile mais próximo, mantém o Y do hit
                    Vector3 targetTile = new Vector3(
                        Mathf.Round(clickPosition.x),
                        transform.position.y, // Usa o Y atual do player
                        Mathf.Round(clickPosition.z)
                    );
                    
                    Debug.Log($"Clique detectado em: {targetTile} - Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    
                    // Calcula caminho até o tile clicado
                    CalculatePath(targetTile);
                    break; // Usa o primeiro chão encontrado
                }
            }
        }
        
        /// <summary>
        /// Calcula caminho tile-based até o destino
        /// </summary>
        private void CalculatePath(Vector3 destination)
        {
            pathQueue.Clear();
            
            Vector3 currentPos = new Vector3(
                Mathf.Round(transform.position.x),
                transform.position.y,
                Mathf.Round(transform.position.z)
            );
            
            // Garante que destino também esteja arredondado
            destination = new Vector3(
                Mathf.Round(destination.x),
                destination.y,
                Mathf.Round(destination.z)
            );
            
            // Se já está no destino, não faz nada
            if (Vector3.Distance(new Vector3(currentPos.x, 0, currentPos.z), 
                                 new Vector3(destination.x, 0, destination.z)) < 0.1f)
            {
                Debug.Log("Já está no tile de destino");
                return;
            }
            
            // Simples pathfinding tile-based (Manhattan distance)
            System.Collections.Generic.List<Vector3> path = new System.Collections.Generic.List<Vector3>();
            Vector3 current = currentPos;
            
            int maxSteps = 100; // Limite de segurança
            int steps = 0;
            
            while (steps < maxSteps)
            {
                steps++;
                
                // Calcula diferença em tiles inteiros
                int dx = Mathf.RoundToInt(destination.x - current.x);
                int dz = Mathf.RoundToInt(destination.z - current.z);
                
                // Se chegou ao destino, para
                if (dx == 0 && dz == 0)
                {
                    break;
                }
                
                Vector3 nextStep = current;
                Vector3 moveDirection = Vector3.zero;
                
                // Decide a próxima direção (prioriza maior distância)
                if (Mathf.Abs(dx) > Mathf.Abs(dz))
                {
                    // Move horizontalmente
                    if (dx > 0)
                    {
                        moveDirection = Vector3.right;
                        nextStep = current + Vector3.right;
                    }
                    else
                    {
                        moveDirection = Vector3.left;
                        nextStep = current + Vector3.left;
                    }
                }
                else if (dz != 0)
                {
                    // Move verticalmente (no eixo Z)
                    if (dz > 0)
                    {
                        moveDirection = Vector3.forward;
                        nextStep = current + Vector3.forward;
                    }
                    else
                    {
                        moveDirection = Vector3.back;
                        nextStep = current + Vector3.back;
                    }
                }
                
                // Verifica se o próximo tile é válido (não tem obstáculo)
                Ray ray = new Ray(current + Vector3.up * 0.5f, moveDirection);
                bool hasObstacle = Physics.Raycast(ray, out RaycastHit hit, 1f);
                
                if (hasObstacle && (hit.collider.CompareTag("scenario") || hit.collider.CompareTag("monster")))
                {
                    Debug.Log($"Caminho bloqueado por: {hit.collider.name}");
                    // Tenta contornar o obstáculo
                    if (Mathf.Abs(dx) > 0 && Mathf.Abs(dz) > 0)
                    {
                        // Tenta mover na outra direção
                        if (Mathf.Abs(dx) > Mathf.Abs(dz))
                        {
                            moveDirection = dz > 0 ? Vector3.forward : Vector3.back;
                            nextStep = current + moveDirection;
                        }
                        else
                        {
                            moveDirection = dx > 0 ? Vector3.right : Vector3.left;
                            nextStep = current + moveDirection;
                        }
                        
                        // Verifica novamente
                        ray = new Ray(current + Vector3.up * 0.5f, moveDirection);
                        hasObstacle = Physics.Raycast(ray, out hit, 1f) && 
                                     (hit.collider.CompareTag("scenario") || hit.collider.CompareTag("monster"));
                        
                        if (hasObstacle)
                        {
                            Debug.Log("Não foi possível encontrar caminho");
                            break; // Não conseguiu contornar
                        }
                    }
                    else
                    {
                        break; // Caminho bloqueado
                    }
                }
                
                path.Add(nextStep);
                current = nextStep;
            }
            
            if (path.Count > 0)
            {
                Debug.Log($"Caminho calculado com {path.Count} tiles");
                
                // Adiciona tiles à fila
                foreach (Vector3 tile in path)
                {
                    pathQueue.Enqueue(tile);
                }
                
                isFollowingPath = true;
            }
            else
            {
                Debug.Log("Nenhum caminho válido encontrado");
            }
        }
    }
}
