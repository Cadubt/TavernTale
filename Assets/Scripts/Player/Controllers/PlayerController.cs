using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 4.0f;
    public float diagonalSpeedMultiplier = 0.75f;
    private float currentSpeed;
    
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isFalling = false;
    private bool canMove = true;
    private Vector3 lastMoveDirection = Vector3.forward;
    
    [Header("Altura")]
    public float PlayerlevelHight = 0.888881f;
    public float LastestObjectYPosition = 0.55f;
    public LayerMask groundLayer;
    public LayerMask elevatorLayer;
    
    [Header("Stats")]
    public int health = 645;
    public int mana = 550;
    
    [Header("Habilidades")]
    public GameObject cubePrefab;
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    private Vector3 pendingDirection = Vector3.zero;
    private bool hasBufferedInput = false;

    private void Start()
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

        if (isMoving)
        {
            ProcessInputBuffer();
            return;
        }

        CheckFloor();

        if (!canMove)
        {
            return;
        }

        Vector3 inputDirection = GetInputDirection();

        if (inputDirection != Vector3.zero)
        {
            TryMove(inputDirection);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            CreateMagicCubes();
        }
    }

    private Vector3 GetInputDirection()
    {
        Vector3 direction = Vector3.zero;
        bool isDiagonal = false;

        // Verifica teclas diagonais primeiro (prioridade)
        if (Input.GetKey(KeyCode.Q))
        {
            direction = Vector3.forward + Vector3.left;
            isDiagonal = true;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            direction = Vector3.forward + Vector3.right;
            isDiagonal = true;
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            direction = Vector3.back + Vector3.left;
            isDiagonal = true;
        }
        else if (Input.GetKey(KeyCode.C))
        {
            direction = Vector3.back + Vector3.right;
            isDiagonal = true;
        }
        else
        {
            // Movimentação cardinal - prioriza a última tecla válida
            bool up = Input.GetKey(KeyCode.W);
            bool down = Input.GetKey(KeyCode.S);
            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);

            // Se teclas opostas são pressionadas, cancela o movimento naquele eixo
            if (up && down) { up = false; down = false; }
            if (left && right) { left = false; right = false; }

            if (up) direction += Vector3.forward;
            else if (down) direction += Vector3.back;
            
            if (left) direction += Vector3.left;
            else if (right) direction += Vector3.right;

            isDiagonal = direction.sqrMagnitude > 1.1f;
        }

        if (direction != Vector3.zero)
        {
            // Arredonda primeiro para garantir valores inteiros
            direction.x = Mathf.Round(direction.x);
            direction.z = Mathf.Round(direction.z);
            
            // Limita para apenas 1 unidade por eixo
            direction.x = Mathf.Clamp(direction.x, -1f, 1f);
            direction.z = Mathf.Clamp(direction.z, -1f, 1f);
            
            currentSpeed = isDiagonal ? speed * diagonalSpeedMultiplier : speed;
        }

        return direction;
    }

    private void ProcessInputBuffer()
    {
        // Não processa buffer durante movimento para evitar double move
        // O buffer só será usado ao final da corrotina Move()
    }

    private void TryMove(Vector3 direction)
    {
        if (isMoving) return;

        lastMoveDirection = direction;
        
        Ray ray = new Ray(transform.position, direction);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 1f);
        
        Debug.DrawLine(ray.origin, ray.origin + direction * 1f, hasHit ? Color.red : Color.green, 0.5f);

        if (hasHit)
        {
            if (hit.collider.CompareTag("elevator"))
            {
                float newY = transform.position.y + 1f;
                targetPosition = new Vector3(transform.position.x, newY, transform.position.z) + direction;
                StartCoroutine(Move(targetPosition));
                return;
            }
            
            if (hit.collider.CompareTag("scenario") || hit.collider.CompareTag("monster"))
            {
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
            if (spriteRenderer != null)
            {
                if (target.x > transform.position.x)
                {
                    spriteRenderer.flipX = true;
                }
                else if (target.x < transform.position.x)
                {
                    spriteRenderer.flipX = false;
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        isMoving = false;

        if (hasBufferedInput)
        {
            hasBufferedInput = false;
            TryMove(pendingDirection);
            pendingDirection = Vector3.zero;
        }
    }

    private void CheckFloor()
    {
        if (isMoving || isFalling) return;

        Ray ray = new Ray(transform.position, Vector3.down);
        bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);

        if (!isGroundHit)
        {
            StartCoroutine(Fall());
        }
    }

    private IEnumerator Fall()
    {
        if (isFalling) yield break;

        isFalling = true;
        canMove = false;

        float newY = transform.position.y - 1f;
        targetPosition = new Vector3(transform.position.x, newY, transform.position.z);
        
        yield return StartCoroutine(Move(targetPosition));

        canMove = true;
        isFalling = false;
    }

    private void CreateMagicCubes()
    {
        Vector3 startPosition = transform.position + lastMoveDirection;
        for (int i = 0; i < 9; i++)
        {
            Vector3 cubePosition = startPosition + lastMoveDirection * i;
            GameObject cube = Instantiate(cubePrefab, cubePosition, Quaternion.identity);
            StartCoroutine(DestroyCubeAfterTime(cube, 1));
        }
    }

    private IEnumerator DestroyCubeAfterTime(GameObject cube, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(cube);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health < 0) health = 0;
    }

    public Vector3 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}
