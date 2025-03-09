using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region  ## Variáveis ##

    public float speed = 4.0f;
    public float originalSpeed = 4.0f;
    private Vector3 targetPosition;
    private bool isMoving = false;
    public int health = 645;
    public int mana = 550;
    public HUDController hudController;
    private float elevatedYPosition;
    private RaycastHit hit;
    Vector3 direction = Vector3.zero;
    public float PlayerlevelHight = 0.888881f;
    public float LastestObjectYPosition = 0.55f;
    private bool canMoveSideways = true;
    public GameObject cubePrefab; // Prefab do cubos
    private Vector3 lastMoveDirection = Vector3.forward; // Última direção de movimento
    private Animator animator;
    private bool isFalling = false;
    public LayerMask groundLayer;
    public LayerMask elevatorLayer;
    [SerializeField] private LayerMask supportLayerMask; // ground + elevator

    #endregion


    /// <summary>
    /// Método chamado automaticamente pelo Unity ao iniciar a cena ou ativar o GameObject.
    /// Inicializa componentes essenciais como o Animator, configura a posição inicial do alvo,
    /// armazena a velocidade original do personagem e atualiza a HUD com a saúde atual.
    /// </summary>
    private void Start()
    {
        animator = GetComponent<Animator>(); // Obtém o componente Animator do GameObject
        originalSpeed = speed; // Armazena o valor original da velocidade
        BoxCollider collider = GetComponent<BoxCollider>(); // Obtém o componente BoxCollider (linha comentada abaixo não altera rotação)
        targetPosition = transform.position; // Define a posição alvo inicial como a posição atual
        hudController = FindObjectOfType<HUDController>(); // Busca o objeto do tipo HUDController na cena
        hudController.UpdateHealth(health); // Atualiza a HUD com a saúde atual do personagem
    }

    private void Update()
    {

        if (isMoving)
        {
            animator.SetBool("isWalking", true);
            return;
        }

        if (!isMoving)
        {
            animator.SetBool("isWalking", false);
            CheckFloor();
        }

        Vector3 playerPosition = transform.position;
        Vector3 lookDirection = transform.forward;
        // Criar um raio da câmera até a posição do mouse
        Ray ray = new Ray(playerPosition, lookDirection);
        direction = Vector3.zero;

        #region Movimentação do jogador com as teclas W, A, S, D, Q, E, Z e C

        if (!canMoveSideways)
        {
            return;
        }

        if (Input.GetKey(KeyCode.W))
        {
            speed = originalSpeed;
            direction += this.IsCollided(direction + Vector3.forward) ? Vector3.forward : Vector3.zero;
            lastMoveDirection = Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            speed = originalSpeed;
            direction += this.IsCollided(direction + Vector3.back) ? Vector3.back : Vector3.zero;
            lastMoveDirection = Vector3.back;
        }
        if (Input.GetKey(KeyCode.A))
        {
            speed = originalSpeed;
            direction += this.IsCollided(direction + Vector3.left) ? Vector3.left : Vector3.zero;
            lastMoveDirection = Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            speed = originalSpeed;
            direction += this.IsCollided(direction + Vector3.right) ? Vector3.right : Vector3.zero;
            lastMoveDirection = Vector3.right;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            direction += this.IsCollided(direction + Vector3.forward + Vector3.left) ? Vector3.forward + Vector3.left : Vector3.zero;
            lastMoveDirection = Vector3.forward + Vector3.left;
            speed = originalSpeed / 1.5f; // Reduz a velocidade pela metade
        }
        if (Input.GetKey(KeyCode.E))
        {
            direction += this.IsCollided(direction + Vector3.forward + Vector3.right) ? Vector3.forward + Vector3.right : Vector3.zero;
            lastMoveDirection = Vector3.forward + Vector3.right;
            speed = originalSpeed / 1.5f; // Reduz a velocidade pela metade
        }
        if (Input.GetKey(KeyCode.Z))
        {
            direction += this.IsCollided(direction + Vector3.back + Vector3.left) ? Vector3.back + Vector3.left : Vector3.zero;
            lastMoveDirection = Vector3.back + Vector3.left;
            speed = originalSpeed / 1.5f; // Reduz a velocidade pela metade
        }
        if (Input.GetKey(KeyCode.C))
        {
            direction += this.IsCollided(direction + Vector3.back + Vector3.right) ? Vector3.back + Vector3.right : Vector3.zero;
            lastMoveDirection = Vector3.back + Vector3.right;
            speed = originalSpeed / 1.5f; // Reduz a velocidade pela metade
        }

        #endregion

        // Detecta a tecla F pressionada para criar cubos
        if (Input.GetKeyDown(KeyCode.F))
        {
            CreateMagicCubes();
        }
    }

    private bool IsCollided(Vector3 direction)
    {
        Ray ray = new Ray(transform.position, direction);
        bool isHit = Physics.Raycast(ray, out RaycastHit hit, 1f);
        Debug.DrawLine(ray.origin, ray.origin + direction * 1f, isHit ? Color.red : Color.green);

        if (isHit && hit.collider.tag != "enviroment")
        {
            Debug.Log("COLIDI COM O OBJETO: " + hit.collider.tag);
            if (hit.collider.CompareTag("elevator"))
            {

                float currentY = transform.position.y + 1f;
                // Debug.Log("Y =" + currentY);


                // Define a nova posição-alvo mantendo o movimento horizontal e alterando só o Y
                targetPosition = new Vector3(targetPosition.x, currentY, targetPosition.z) + direction;

                StartCoroutine(Move(targetPosition));
            }
        }
        else
        {
            targetPosition += direction;
            StartCoroutine(Move(targetPosition));
        }
        return isHit;
    }

    IEnumerator Move(Vector3 target, System.Action onComplete = null)
    {
        Debug.Log(transform.position.y);
        isMoving = true;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            if (target.x > transform.position.x)
            {
                spriteRenderer.flipX = true;
            }
            else if (target.x < transform.position.x)
            {
                spriteRenderer.flipX = false;
            }

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;
        isMoving = false;


        // onComplete?.Invoke(); // Chama o callback se existir
    }


    /// <summary>
    /// Verifica se o personagem esta em cima de um objeto se sim avisa, se não cai
    /// </summary>
    private void CheckFloor()
    {
        if (isMoving || isFalling) return;


        Ray ray = new Ray(transform.position, Vector3.down);
        bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);

        Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, Color.red);

        if (!isGroundHit)
        {
            Debug.Log("SEM CHAAAAAAAAo");
            StartCoroutine(MoveDown());
        }
    }

    private IEnumerator MoveDown()
    {
        if (isFalling) yield break;

        isFalling = true;
        canMoveSideways = false;

        float currentY = transform.position.y;
        Ray ray = new Ray(transform.position + Vector3.up * 1f, Vector3.down);
        bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);

        Debug.DrawRay(ray.origin, ray.direction * 1f, isGroundHit ? Color.green : Color.red);

        if (isGroundHit)
        {
            Debug.Log("TOQUEI NO CHÃO: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
        }
        currentY -= 1f;


        targetPosition = new Vector3(targetPosition.x, currentY, targetPosition.z);
        yield return StartCoroutine(Move(targetPosition));

        canMoveSideways = true;
        isFalling = false;
    }



    private void CreateMagicCubes()
    {
        Vector3 startPosition = transform.position + lastMoveDirection;
        for (int i = 0; i < 9; i++)
        {
            Debug.Log("CRIANDO CUBO");
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
        // Garante que a vida não fique negativa
        health = Mathf.Max(health, 0);
        // Atualiza a UI com a nova vida
        hudController.UpdateHealth(health);
    }
}