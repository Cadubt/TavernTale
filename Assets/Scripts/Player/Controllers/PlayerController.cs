using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region  ## Variáveis ##

    public float speed = 1.0f;
    public float originalSpeed = 1.0f;
    private Vector3 targetPosition;
    private bool isMoving = false;
    public int health = 645;
    public int mana = 550;
    public HUDController hudController;
    private float elevatedYPosition;
    private RaycastHit hit;
    Vector3 direction = Vector3.zero;
    public float PlayerlevelHight = 0f;
    public float LastestObjectYPosition = 0f;
    private bool canMoveSideways = true;
    public GameObject cubePrefab; // Prefab do cubos
    private Vector3 lastMoveDirection = Vector3.forward; // Última direção de movimento

    #endregion

    private void Start()
    {
        originalSpeed = speed;
        BoxCollider collider = GetComponent<BoxCollider>();
        // collider.transform.rotation = Quaternion.identity;
        targetPosition = transform.position;
        hudController = FindObjectOfType<HUDController>();
        // Atualize a UI inicialmente com a saúde atual
        hudController.UpdateHealth(health);
    }

    private void Update()
    {       

        if (isMoving)
            return;

        Vector3 playerPosition = transform.position;
        Vector3 lookDirection = transform.forward;
        // Criar um raio da câmera até a posição do mouse
        Ray ray = new Ray(playerPosition, lookDirection);
        direction = Vector3.zero;

        CheckIfOnGroundAndDescend();

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
            if (hit.collider.tag == "elevator")
            {
                LastestObjectYPosition = hit.collider.transform.position.y;
                PlayerlevelHight = LastestObjectYPosition + 1f;
                targetPosition = new Vector3(targetPosition.x, PlayerlevelHight, targetPosition.z) + direction;
                Debug.Log(PlayerlevelHight);
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

    IEnumerator Move(Vector3 target)
    {
        isMoving = true;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        while ((target - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            // Flip the sprite based on the direction of movement
            if (target.x > transform.position.x)
            {
                // Moving right
                spriteRenderer.flipX = false;
            }
            else if (target.x < transform.position.x)
            {
                // Moving left
                spriteRenderer.flipX = true;
            }
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        isMoving = false;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        // Garante que a vida não fique negativa
        health = Mathf.Max(health, 0);
        // Atualiza a UI com a nova vida
        hudController.UpdateHealth(health);
    }

    /**
     * Verifica se o personagem esta em cima de um objeto e desce 0.5f no eixo Y
     */
    private void CheckIfOnGroundAndDescend()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f);
        Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, isGroundHit ? Color.red : Color.green);
        // Debug.Log("IS GROUND HIT: " + hit.collider.tag);

        if (hit.collider == null)
        {
            Debug.Log("NÃO ESTOU EM CIMA DE NADA");
            StartCoroutine(WaitAndMoveDown());
        }
        // else
        // {
        //     Debug.Log("ESTOU EM CIMA DO : " + hit.collider.tag);
        // }
    }

    private IEnumerator WaitAndMoveDown()
    {
        // Desativa a movimentação lateral
        canMoveSideways = false;

        // Aguarda 1 segundo
        yield return new WaitForSeconds(0.0001f);

        // Move o jogador para baixo
        targetPosition = new Vector3(targetPosition.x, PlayerlevelHight - 1f, targetPosition.z);
        StartCoroutine(Move(targetPosition));

        // Reativa a movimentação lateral
        canMoveSideways = true;
    }

    private void CreateMagicCubes()
    {
        Vector3 startPosition = transform.position + lastMoveDirection;
        for (int i = 0; i < 5; i++)
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
}