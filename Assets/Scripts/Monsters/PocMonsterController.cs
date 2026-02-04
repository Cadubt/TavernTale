using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de monstro com comportamento tile-based
/// Segue o player, causa dano ao encostar e se reposiciona nos 8 SQMs adjacentes
/// </summary>
public class PocMonsterController : MonoBehaviour
{
    // Estrutura para representar um tile 2D (apenas X e Z)
    private struct Tile2D
    {
        public float x;
        public float z;
        
        public Tile2D(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
        
        public Tile2D(Vector3 position)
        {
            this.x = Mathf.Round(position.x * 100f) / 100f; // Arredonda para evitar erros de ponto flutuante
            this.z = Mathf.Round(position.z * 100f) / 100f;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Tile2D other)
            {
                return Mathf.Abs(x - other.x) < 0.01f && Mathf.Abs(z - other.z) < 0.01f;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return (x, z).GetHashCode();
        }
        
        public override string ToString()
        {
            return $"({x}, {z})";
        }
    }
    
    // Sistema estático de reserva de tiles para prevenir race conditions
    private static HashSet<Tile2D> reservedTiles = new HashSet<Tile2D>();
    private Tile2D currentReservedTile; // Tile que este monstro reservou
    private bool hasTileReserved = false;
    
    // Sistema de seleção e highlight
    private static PocMonsterController selectedMonster = null;
    private bool isSelected = false;
    private GameObject selectionQuad; // Quad de seleção no chão
    private GameObject outlineSprite; // Sprite duplicado para o outline
    
    // Sistema de exibição de dano
    private GameObject damageTextObject;
    private TextMesh damageText;
    private Coroutine hideDamageCoroutine;
    
    [Header("Sistema de Seleção")]
    [SerializeField] private Color selectionQuadColor = new Color(1f, 1f, 0f, 0.5f); // Amarelo semi-transparente
    [SerializeField] private Color outlineColor = new Color(1f, 0f, 0f, 1f); // Vermelho para o contorno
    [SerializeField] private float outlineWidth = 1f; // Largura do contorno em pixels
    [SerializeField] private Texture2D cursorTexture; // Textura customizada do cursor (opcional)
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero; // Ponto quente do cursor
    [SerializeField] private int attackDamageOnSelect = 50; // Dano ao selecionar monstro adjacente
    [SerializeField] private float autoAttackInterval = 3.0f; // Intervalo entre ataques automáticos em segundos
    private Coroutine autoAttackCoroutine; // Referência para a corrotina de ataque automático
    
    [Header("Sistema de Vida")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    
    [Header("Sistema de Dano Visual")]
    [SerializeField] private Color damageTextColor = Color.red;
    [SerializeField] private float damageTextSize = 0.2f;
    [SerializeField] private float damageTextYOffset = 6f; // Altura acima do monstro (em unidades do mundo)
    [SerializeField] private float damageTextDuration = 1f; // Tempo antes de desaparecer
    
    [Header("Configurações de Movimento")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float chaseRange = 10f; // Distância para começar a perseguir
    
    [Header("Configurações de Combate")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackInterval = 2.0f; // Tempo entre ataques
    [SerializeField] private float repositionInterval = 4.0f; // Tempo para se reposicionar
    
    [Header("Detecção")]
    [SerializeField] private LayerMask obstacleLayer = -1;
    
    [Header("Sistema de Tiles")]
    [SerializeField] private float tileSize = 1f; // Tamanho do tile (1 = padrão)
    [SerializeField] private Vector3 tileOffset = Vector3.zero; // Offset do grid de tiles
    [SerializeField] private bool showTileGizmos = true; // Mostra gizmos de debug
    
    [Header("Sistema de Fall e Elevator")]
    [SerializeField] private bool enableFallSystem = true; // Habilita queda quando não há chão
    [SerializeField] private bool enableElevatorSystem = true; // Habilita subida em elevadores
    [SerializeField] private LayerMask groundLayer = -1; // Layer do chão
    [SerializeField] private LayerMask elevatorLayer = -1; // Layer dos elevadores
    
    // Estado
    private GameObject player;
    private PlayerController playerController;
    private bool isMoving = false;
    private bool isAttacking = false;
    private bool isFalling = false;
    private Vector3 targetPosition;
    private Vector3 previousPosition; // Posição anterior para desfazer movimento
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    
    // Timers
    private float attackTimer = 0f;
    private float repositionTimer = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        targetPosition = transform.position;
        
        // Inicializa a vida
        currentHealth = maxHealth;
        
        // Cria o sprite de outline
        CreateOutlineSprite();
        
        // Cria o texto de dano
        CreateDamageText();
        
        // Cria o quad de seleção no chão
        CreateSelectionQuad();
        
        // Reserva o tile inicial
        Vector3 initialTile = GetTilePosition(transform.position);
        ReserveTile(initialTile);
        
        // Remove OptimizableObject se existir (monstros não devem ser culled)
        var optimizableType = System.Type.GetType("Core.OptimizableObject");
        if (optimizableType != null)
        {
            var optimizable = GetComponent(optimizableType);
            if (optimizable != null)
            {
                Debug.LogWarning($"Removendo OptimizableObject de {gameObject.name} - Monstros não devem ser otimizados!");
                Destroy(optimizable);
            }
        }
        
        // Garante que o renderer está ativo
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            
            // Garante configurações corretas do renderer
            spriteRenderer.sortingLayerName = "Default"; // Ajuste conforme sua configuração
            // spriteRenderer.sortingOrder = 10; // Valor alto para aparecer na frente
            
            Debug.Log($"{gameObject.name} - Sprite: {spriteRenderer.sprite?.name ?? "NULL"}, Enabled: {spriteRenderer.enabled}");
        }
        
        // Garante que o GameObject está ativo
        gameObject.SetActive(true);
        
        // Debug para verificar configuração
        if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name} não tem SpriteRenderer!");
        }
        else if (spriteRenderer.sprite == null)
        {
            Debug.LogError($"{gameObject.name} SpriteRenderer não tem sprite atribuído!");
        }
        else
        {
            Debug.Log($"{gameObject.name} configurado corretamente! Sprite: {spriteRenderer.sprite.name}");
        }
    }

    private void Update()
    {
        if (player == null) return;
        
        // VERIFICAÇÃO CRÍTICA: Se detectar colisão e não estiver se movendo, empurra para tile livre
        if (!isMoving && CheckMonsterCollision())
        {
            Debug.LogError($"{gameObject.name} detectou colisão em Update! Tentando se separar...");
            StartCoroutine(SeparateFromOtherMonster());
            return;
        }
        
        // PROTEÇÃO: Garante que o sprite está sempre ativo
        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            spriteRenderer.enabled = true;
            Debug.LogWarning($"{gameObject.name} sprite foi reativado!");
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // Verifica se está no alcance de perseguição
        if (distanceToPlayer > chaseRange)
        {
            isAttacking = false;
            return;
        }

        // Verifica se está adjacente ao player (nos 8 SQMs ao redor)
        bool isAdjacentToPlayer = IsAdjacentToPlayer();

        if (isAdjacentToPlayer)
        {
            // Está próximo do player - modo ataque
            HandleAttackMode();
        }
        else if (!isMoving)
        {
            // Não está próximo - persegue o player
            StartCoroutine(MoveTowardsPlayer());
        }

        // Atualiza animação
        if (animator != null)
        {
            animator.SetBool("isWalking", isMoving);
        }
        
        // Sincroniza o flip do sprite de outline com o sprite principal
        SyncOutlineFlip();
    }
    
    /// <summary>
    /// Sincroniza o flip do sprite de outline com o sprite principal
    /// </summary>
    private void SyncOutlineFlip()
    {
        if (outlineSprite != null && spriteRenderer != null)
        {
            SpriteRenderer outlineRenderer = outlineSprite.GetComponent<SpriteRenderer>();
            if (outlineRenderer != null)
            {
                outlineRenderer.flipX = spriteRenderer.flipX;
                outlineRenderer.flipY = spriteRenderer.flipY;
            }
        }
    }
    
    /// <summary>
    /// Cria o texto de dano acima do monstro
    /// </summary>
    private void CreateDamageText()
    {
        // Cria GameObject para o texto
        damageTextObject = new GameObject("DamageText");
        damageTextObject.transform.SetParent(transform);
        damageTextObject.transform.localPosition = new Vector3(0, damageTextYOffset, 0);
        damageTextObject.transform.localRotation = Quaternion.identity; // Mesma rotação do sprite
        
        // Aplica escala para compensar a distorção da perspectiva
        // Estica verticalmente e comprime horizontalmente
        damageTextObject.transform.localScale = new Vector3(1f, 1.2f, 1f); // 2x mais alto
        
        // Adiciona TextMesh
        damageText = damageTextObject.AddComponent<TextMesh>();
        damageText.text = "";
        damageText.fontSize = 50;
        damageText.characterSize = damageTextSize;
        damageText.anchor = TextAnchor.MiddleCenter;
        damageText.alignment = TextAlignment.Center;
        damageText.color = damageTextColor;
        
        // Adiciona MeshRenderer para aparecer corretamente
        MeshRenderer renderer = damageTextObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 100; // Renderiza na frente de tudo
        }
        
        // Começa invisível
        damageTextObject.SetActive(false);
    }
    
    /// <summary>
    /// Detecta clique do botão direito do mouse no monstro
    /// </summary>
    private void OnMouseOver()
    {
        // Verifica se foi clique com botão direito (1 = botão direito)
        if (Input.GetMouseButtonDown(1))
        {
            SelectMonster();
        }
    }
    
    /// <summary>
    /// Muda cursor para pointer quando mouse passa por cima
    /// </summary>
    private void OnMouseEnter()
    {
        if (cursorTexture != null)
        {
            // Usa textura customizada se fornecida
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            // Cria uma textura simples de pointer programaticamente
            CreateAndSetPointerCursor();
        }
        Debug.Log("Mouse entrou no monstro - cursor alterado");
    }
    
    /// <summary>
    /// Cria um cursor de pointer simples programaticamente
    /// </summary>
    private void CreateAndSetPointerCursor()
    {
        // Cria uma textura 32x32 para simular um pointer
        Texture2D pointerTexture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        // Preenche com transparente
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        
        // Desenha uma seta simples (pointer)
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 12; x++)
            {
                if (x <= y / 2 && x < 10)
                {
                    colors[y * 32 + x] = Color.white;
                    if (x > 0 && x < y / 2 - 1)
                    {
                        colors[y * 32 + x] = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }
        }
        
        pointerTexture.SetPixels(colors);
        pointerTexture.Apply();
        
        Cursor.SetCursor(pointerTexture, Vector2.zero, CursorMode.Auto);
    }
    
    /// <summary>
    /// Restaura cursor padrão quando mouse sai de cima
    /// </summary>
    private void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Debug.Log("Mouse saiu do monstro - cursor restaurado");
    }
    
    /// <summary>
    /// Cria um sprite duplicado atrás para mostrar o outline
    /// </summary>
    private void CreateOutlineSprite()
    {
        if (spriteRenderer == null) return;
        
        // Cria um GameObject filho para o outline
        outlineSprite = new GameObject("Outline");
        outlineSprite.transform.SetParent(transform);
        outlineSprite.transform.localPosition = Vector3.zero;
        outlineSprite.transform.localRotation = Quaternion.identity;
        outlineSprite.transform.localScale = Vector3.one;
        
        // Adiciona SpriteRenderer e copia configurações
        SpriteRenderer outlineRenderer = outlineSprite.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = spriteRenderer.sprite;
        outlineRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        outlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1; // Renderiza atrás
        
        // Cria material com shader de outline
        Shader outlineShader = Shader.Find("Sprites/Outline");
        if (outlineShader != null)
        {
            Material outlineMat = new Material(outlineShader);
            outlineMat.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            outlineMat.SetColor("_Color", Color.white);
            outlineMat.SetColor("_OutlineColor", outlineColor);
            outlineMat.SetFloat("_OutlineWidth", outlineWidth / 100f);
            outlineRenderer.material = outlineMat;
        }
        
        // Começa desativado
        outlineSprite.SetActive(false);
    }
    
    /// <summary>
    /// Cria o quad de seleção no chão
    /// </summary>
    private void CreateSelectionQuad()
    {
        // Cria um GameObject para o quad
        selectionQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        selectionQuad.name = "SelectionQuad";
        selectionQuad.transform.SetParent(transform);
        
        // Posiciona no chão (ligeiramente acima para evitar z-fighting)
        selectionQuad.transform.localPosition = new Vector3(0, 0.01f, 0);
        selectionQuad.transform.localRotation = Quaternion.Euler(90, 0, 0); // Rotaciona para ficar horizontal
        selectionQuad.transform.localScale = new Vector3(1f, 1f, 1f); // Tamanho do tile
        
        // Configura o material
        Renderer quadRenderer = selectionQuad.GetComponent<Renderer>();
        if (quadRenderer != null)
        {
            // Cria um material simples com a cor de seleção
            Material selectionMaterial = new Material(Shader.Find("Sprites/Default"));
            selectionMaterial.color = selectionQuadColor;
            quadRenderer.material = selectionMaterial;
            
            // Configura para renderizar acima do chão
            quadRenderer.sortingLayerName = "Default";
            quadRenderer.sortingOrder = 1;
        }
        
        // Remove o collider do quad para não interferir nos clicks
        Collider quadCollider = selectionQuad.GetComponent<Collider>();
        if (quadCollider != null)
        {
            Destroy(quadCollider);
        }
        
        // Começa desativado
        selectionQuad.SetActive(false);
    }
    
    /// <summary>
    /// Seleciona este monstro e aplica highlight
    /// </summary>
    private void SelectMonster()
    {
        // Desseleciona o monstro anterior se houver
        if (selectedMonster != null && selectedMonster != this)
        {
            selectedMonster.Deselect();
        }
        
        // Seleciona este monstro
        selectedMonster = this;
        isSelected = true;
        
        // Ativa o sprite de outline (não altera o sprite original!)
        if (outlineSprite != null)
        {
            outlineSprite.SetActive(true);
            Debug.Log($"Outline ativado para {gameObject.name}");
        }
        
        // Não mostra mais o quad de seleção no chão (removido)
        // if (selectionQuad != null)
        // {
        //     selectionQuad.SetActive(true);
        // }
        
        Debug.Log($"{gameObject.name} foi selecionado! HP: {currentHealth}/{maxHealth}");
        
        // Inicia ataque automático a cada 3 segundos
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
        }
        autoAttackCoroutine = StartCoroutine(AutoAttackRoutine());
    }
    
    /// <summary>
    /// Corrotina que ataca automaticamente a cada intervalo definido
    /// </summary>
    private IEnumerator AutoAttackRoutine()
    {
        while (isSelected)
        {
            // Verifica se está adjacente ao player e ataca
            if (player != null && IsAdjacentToPlayer())
            {
                TakeDamage(attackDamageOnSelect);
                Debug.Log($"Ataque automático! {gameObject.name} recebeu {attackDamageOnSelect} de dano! HP: {currentHealth}/{maxHealth}");
            }
            
            // Aguarda o intervalo antes do próximo ataque
            yield return new WaitForSeconds(autoAttackInterval);
        }
    }
    
    /// <summary>
    /// Desseleciona este monstro e remove highlight
    /// </summary>
    private void Deselect()
    {
        isSelected = false;
        
        // Para o ataque automático
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
        
        // Desativa o sprite de outline (sprite original não foi alterado!)
        if (outlineSprite != null)
        {
            outlineSprite.SetActive(false);
        }
        
        // Não precisa mais esconder o quad (removido)
        // if (selectionQuad != null)
        // {
        //     selectionQuad.SetActive(false);
        // }
        
        Debug.Log($"{gameObject.name} foi desselecionado!");
    }
    
    /// <summary>
    /// Recebe dano e verifica se morreu
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} recebeu {damageAmount} de dano! HP restante: {currentHealth}/{maxHealth}");
        
        // Mostra o dano visualmente
        ShowDamage(damageAmount);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Mostra o dano recebido acima do monstro
    /// </summary>
    private void ShowDamage(int damage)
    {
        if (damageText == null || damageTextObject == null) return;
        
        // Para a corrotina anterior se existir
        if (hideDamageCoroutine != null)
        {
            StopCoroutine(hideDamageCoroutine);
        }
        
        // Atualiza o texto com o novo dano
        damageText.text = $"-{damage}";
        damageTextObject.SetActive(true);
        
        // Inicia nova corrotina para esconder após 2 segundos
        hideDamageCoroutine = StartCoroutine(HideDamageAfterDelay());
    }
    
    /// <summary>
    /// Esconde o texto de dano após um delay
    /// </summary>
    private IEnumerator HideDamageAfterDelay()
    {
        yield return new WaitForSeconds(damageTextDuration);
        
        if (damageTextObject != null)
        {
            damageTextObject.SetActive(false);
        }
        
        hideDamageCoroutine = null;
    }
    
    /// <summary>
    /// Morre e desaparece
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} morreu!");
        
        // Para o ataque automático se estava ativo
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
            autoAttackCoroutine = null;
        }
        
        // Se estava selecionado, limpa a referência estática
        if (selectedMonster == this)
        {
            selectedMonster = null;
        }
        
        // Libera o tile reservado
        ReleaseTile();
        
        // Destroi o GameObject
        Destroy(gameObject);
    }
    
    private void LateUpdate()
    {
        // PROTEÇÃO FINAL: Força o sprite a estar sempre ativo
        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            spriteRenderer.enabled = true;
        }
        
        // Garante que o GameObject está ativo
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
    
    private void OnDestroy()
    {
        // Libera o tile reservado quando o monstro é destruído
        ReleaseTile();
    }
    
    /// <summary>
    /// Reserva um tile para este monstro
    /// </summary>
    private bool ReserveTile(Vector3 tile)
    {
        Tile2D tile2D = new Tile2D(tile);
        
        // Se já tem um tile reservado, libera primeiro
        if (hasTileReserved)
        {
            ReleaseTile();
        }
        
        // Tenta reservar o novo tile
        if (!reservedTiles.Contains(tile2D))
        {
            reservedTiles.Add(tile2D);
            currentReservedTile = tile2D;
            hasTileReserved = true;
            Debug.Log($"{gameObject.name} reservou tile {tile2D}");
            return true;
        }
        
        Debug.Log($"{gameObject.name} não conseguiu reservar tile {tile2D} - já está reservado");
        return false;
    }
    
    /// <summary>
    /// Libera o tile reservado por este monstro
    /// </summary>
    private void ReleaseTile()
    {
        if (hasTileReserved)
        {
            reservedTiles.Remove(currentReservedTile);
            Debug.Log($"{gameObject.name} liberou tile {currentReservedTile}");
            hasTileReserved = false;
        }
    }
    
    /// <summary>
    /// Verifica se um tile está reservado por outro monstro
    /// </summary>
    private bool IsTileReserved(Vector3 tile)
    {
        Tile2D tile2D = new Tile2D(tile);
        return reservedTiles.Contains(tile2D);
    }

    private bool IsAdjacentToPlayer()
    {
        Vector3 monsterTile = GetTilePosition(transform.position);
        Vector3 playerTile = GetTilePosition(player.transform.position);
        
        float dx = Mathf.Abs(monsterTile.x - playerTile.x);
        float dz = Mathf.Abs(monsterTile.z - playerTile.z);
        
        // Verifica se está nos 8 tiles adjacentes ou no mesmo tile
        return dx <= 1 && dz <= 1;
    }

    private void HandleAttackMode()
    {
        isAttacking = true;
        attackTimer += Time.deltaTime;
        repositionTimer += Time.deltaTime;
        
        // Faz o sprite olhar para o player quando está atacando
        if (spriteRenderer != null && player != null)
        {
            if (player.transform.position.x > transform.position.x)
                spriteRenderer.flipX = false;
            else if (player.transform.position.x < transform.position.x)
                spriteRenderer.flipX = true;
        }

        // Aplica dano periodicamente
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            AttackPlayer();
        }

        // Reposiciona-se periodicamente para um dos 8 SQMs adjacentes
        if (repositionTimer >= repositionInterval && !isMoving)
        {
            repositionTimer = 0f;
            
            // VERIFICAÇÃO PREVENTIVA: Antes de tentar reposicionar, verifica se já está em colisão
            if (CheckMonsterCollision())
            {
                Debug.LogError($"{gameObject.name} já está em colisão! Não tentará reposicionar.");
                return;
            }
            
            StartCoroutine(RepositionAroundPlayer());
        }
    }

    private void AttackPlayer()
    {
        if (playerController != null)
        {
            playerController.TakeDamage(damage);
            Debug.Log($"{gameObject.name} causou {damage} de dano ao player!");
        }
    }

    private IEnumerator MoveTowardsPlayer()
    {
        // VERIFICAÇÃO PREVENTIVA: Não inicia movimento se já está em colisão
        if (CheckMonsterCollision())
        {
            Debug.LogError($"{gameObject.name} já está em colisão antes de mover! Cancelando movimento.");
            yield break;
        }
        
        isMoving = true;

        Vector3 currentTile = GetTilePosition(transform.position);
        Vector3 playerTile = GetTilePosition(player.transform.position);

        // Calcula direção para o player
        Vector3 direction = CalculateDirection(currentTile, playerTile);

        // Verifica se há monstro bloqueando o caminho
        if (HasMonsterInDirection(currentTile, direction))
        {
            Debug.Log($"{gameObject.name} detectou monstro no caminho direto - procurando rota alternativa");
            
            // Tenta encontrar um tile adjacente ao player disponível
            Vector3 targetAdjacentTile = FindBestAdjacentTileToPlayer(currentTile);
            
            if (targetAdjacentTile != Vector3.zero)
            {
                // Tenta mover em direção ao tile adjacente encontrado
                direction = CalculateDirection(currentTile, targetAdjacentTile);
                
                // Verifica novamente se há monstro nesta nova direção
                if (HasMonsterInDirection(currentTile, direction))
                {
                    // Tenta todas as direções adjacentes disponíveis
                    direction = FindAlternativeDirectionAvoidingMonsters(currentTile, targetAdjacentTile);
                }
            }
            else
            {
                // Tenta direções alternativas evitando monstros
                direction = FindAlternativeDirectionAvoidingMonsters(currentTile, playerTile);
            }
            
            if (direction == Vector3.zero)
            {
                Debug.Log($"{gameObject.name} não encontrou caminho livre - ficando parado");
                isMoving = false;
                yield break;
            }
        }
        // Verifica se pode mover nessa direção (obstáculos, paredes, etc)
        else if (!CanMoveToDirection(currentTile, direction))
        {
            Debug.Log($"{gameObject.name} caminho bloqueado por obstáculo - procurando alternativa");
            direction = FindAlternativeDirection(currentTile, playerTile);
            
            if (direction == Vector3.zero)
            {
                isMoving = false;
                yield break;
            }
        }

        Vector3 newTarget = currentTile + direction;
        
        // VERIFICAÇÃO 1: Verifica fisicamente se há monstro no tile de destino
        if (IsTileOccupied(newTarget))
        {
            Debug.LogWarning($"{gameObject.name} VERIFICAÇÃO FÍSICA: tile {newTarget} está ocupado!");
            isMoving = false;
            yield break;
        }
        
        // VERIFICAÇÃO 2: Tenta reservar o tile de destino ANTES de mover
        if (!ReserveTile(newTarget))
        {
            Debug.Log($"{gameObject.name} VERIFICAÇÃO RESERVA: não conseguiu reservar tile {newTarget}");
            isMoving = false;
            yield break;
        }
        
        // VERIFICAÇÃO 3: Verifica novamente após reservar (double-check)
        if (IsTileOccupied(newTarget))
        {
            Debug.LogWarning($"{gameObject.name} VERIFICAÇÃO DUPLA: tile {newTarget} foi ocupado após reserva!");
            ReleaseTile(); // Libera a reserva
            isMoving = false;
            yield break;
        }
        
        // Move para o novo tile
        yield return StartCoroutine(MoveToTile(newTarget));

        isMoving = false;
    }
    
    /// <summary>
    /// Separa este monstro de outro quando detecta colisão
    /// </summary>
    private IEnumerator SeparateFromOtherMonster()
    {
        if (isMoving) yield break;
        
        isMoving = true;
        Vector3 currentTile = GetTilePosition(transform.position);
        
        Debug.LogWarning($"{gameObject.name} tentando se separar de outro monstro...");
        
        // Tenta encontrar um tile livre adjacente
        Vector3[] directions = new Vector3[]
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            Vector3.right + Vector3.forward, Vector3.right + Vector3.back,
            Vector3.left + Vector3.forward, Vector3.left + Vector3.back
        };
        
        foreach (Vector3 dir in directions)
        {
            Vector3 normalized = new Vector3(Mathf.Sign(dir.x), 0, Mathf.Sign(dir.z));
            Vector3 targetTile = currentTile + normalized;
            
            if (!IsTileOccupied(targetTile) && !IsTileReserved(targetTile) && ReserveTile(targetTile))
            {
                Debug.Log($"{gameObject.name} encontrou tile livre para separação: ({targetTile.x}, {targetTile.z})");
                yield return StartCoroutine(MoveToTile(targetTile));
                isMoving = false;
                yield break;
            }
        }
        
        Debug.LogError($"{gameObject.name} não encontrou tile livre para separação! Forçando deslocamento...");
        // Se não encontrou tile livre, força um deslocamento pequeno
        transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
        
        isMoving = false;
    }

    private IEnumerator RepositionAroundPlayer()
    {
        // VERIFICAÇÃO PREVENTIVA: Não reposiciona se já está em colisão
        if (CheckMonsterCollision())
        {
            Debug.LogError($"{gameObject.name} já está em colisão! Não irá reposicionar.");
            yield break;
        }
        
        isMoving = true;

        Vector3 playerTile = GetTilePosition(player.transform.position);
        Vector3 currentTile = GetTilePosition(transform.position);
        
        // Lista dos 8 tiles adjacentes ao player
        Vector3[] adjacentTiles = new Vector3[]
        {
            playerTile + Vector3.right,
            playerTile + Vector3.left,
            playerTile + Vector3.forward,
            playerTile + Vector3.back,
            playerTile + Vector3.right + Vector3.forward,
            playerTile + Vector3.right + Vector3.back,
            playerTile + Vector3.left + Vector3.forward,
            playerTile + Vector3.left + Vector3.back
        };

        // Embaralha e tenta encontrar um tile válido
        List<Vector3> shuffledTiles = new List<Vector3>(adjacentTiles);
        ShuffleList(shuffledTiles);

        Vector3 targetTile = Vector3.zero;
        bool foundValidTile = false;

        foreach (Vector3 tile in shuffledTiles)
        {
            // Ignora o tile atual
            if (IsSameTile(tile, currentTile)) continue;
            
            // VERIFICAÇÃO TRIPLA: tile ocupado, reservado e verificação física
            if (IsTileOccupied(tile))
            {
                Debug.Log($"{gameObject.name} REPOSIÇÃO: tile {tile} está ocupado fisicamente");
                continue;
            }
            
            if (IsTileReserved(tile))
            {
                Debug.Log($"{gameObject.name} REPOSIÇÃO: tile {tile} está reservado");
                continue;
            }
            
            // Tenta reservar IMEDIATAMENTE
            if (!ReserveTile(tile))
            {
                Debug.Log($"{gameObject.name} REPOSIÇÃO: falhou ao reservar tile {tile}");
                continue;
            }
            
            // Verifica novamente após reservar
            if (IsTileOccupied(tile))
            {
                Debug.LogWarning($"{gameObject.name} REPOSIÇÃO: tile {tile} foi ocupado após reserva!");
                ReleaseTile();
                continue;
            }
            
            targetTile = tile;
            foundValidTile = true;
            Debug.Log($"{gameObject.name} REPOSIÇÃO: encontrou e reservou tile válido {tile}");
            break;
        }

        if (foundValidTile)
        {
            // Move DIRETAMENTE para o targetTile (não usa direção intermediária)
            yield return StartCoroutine(MoveToTile(targetTile));
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} REPOSIÇÃO: não encontrou tile válido para reposicionar");
        }

        isMoving = false;
    }

    private IEnumerator MoveToTile(Vector3 targetTile)
    {
        Vector3 startPosition = transform.position;
        
        // Salva a posição anterior antes de mover
        previousPosition = startPosition;
        
        Vector3 direction = (targetTile - GetTilePosition(startPosition)).normalized;
        Vector3 endPosition = new Vector3(targetTile.x, transform.position.y, targetTile.z);
        
        // Verifica se há elevador na direção do movimento
        if (enableElevatorSystem && elevatorLayer != -1)
        {
            Ray ray = new Ray(startPosition + Vector3.up * 0.5f, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, 1f))
            {
                int hitLayer = 1 << hit.collider.gameObject.layer;
                bool isElevator = (elevatorLayer.value & hitLayer) != 0;
                
                if (isElevator)
                {
                    Debug.Log($"{gameObject.name} subindo elevador de Y:{startPosition.y} para Y:{startPosition.y + 1f}");
                    float newY = startPosition.y + 1f;
                    endPosition = new Vector3(targetTile.x, newY, targetTile.z);
                }
            }
        }

        // PROTEÇÃO: Garante sprite ativo durante movimento
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            
            // Atualiza flip do sprite
            if (endPosition.x > startPosition.x)
                spriteRenderer.flipX = true;
            else if (endPosition.x < startPosition.x)
                spriteRenderer.flipX = false;
        }

        float elapsed = 0f;
        float duration = 0.5f / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            
            // Verifica colisão durante o movimento
            if (CheckMonsterCollision())
            {
                Debug.LogError($"{gameObject.name} COLISÃO DURANTE MOVIMENTO! Revertendo imediatamente!");
                transform.position = previousPosition;
                yield break;
            }
            
            yield return null;
        }

        transform.position = endPosition;
        
        // Verifica se há colisão com outro monstro após movimento (verificação final)
        if (CheckMonsterCollision())
        {
            Debug.LogError($"{gameObject.name} COLISÃO APÓS MOVIMENTO! Desfazendo movimento!");
            transform.position = previousPosition;
            // Força atualização da reserva para a posição correta
            ReserveTile(GetTilePosition(previousPosition));
        }
        
        // Verifica chão após movimento
        if (enableFallSystem)
        {
            CheckFloor();
        }
    }

    private Vector3 CalculateDirection(Vector3 from, Vector3 to)
    {
        float dx = to.x - from.x;
        float dz = to.z - from.z;

        // Prioriza a maior distância
        if (Mathf.Abs(dx) > Mathf.Abs(dz))
        {
            return new Vector3(Mathf.Sign(dx), 0, 0);
        }
        else if (Mathf.Abs(dz) > 0.1f)
        {
            return new Vector3(0, 0, Mathf.Sign(dz));
        }

        return Vector3.zero;
    }

    private Vector3 FindAlternativeDirection(Vector3 from, Vector3 to)
    {
        // Tenta direções alternativas quando o caminho direto está bloqueado
        Vector3[] directions = new Vector3[]
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            Vector3.right + Vector3.forward, Vector3.right + Vector3.back,
            Vector3.left + Vector3.forward, Vector3.left + Vector3.back
        };

        // Ordena direções por proximidade ao alvo
        System.Array.Sort(directions, (a, b) =>
        {
            float distA = Vector3.Distance(from + a, to);
            float distB = Vector3.Distance(from + b, to);
            return distA.CompareTo(distB);
        });

        foreach (Vector3 dir in directions)
        {
            Vector3 normalized = new Vector3(Mathf.Sign(dir.x), 0, Mathf.Sign(dir.z));
            if (CanMoveToDirection(from, normalized))
            {
                return normalized;
            }
        }

        return Vector3.zero;
    }
    
    /// <summary>
    /// Encontra o melhor tile adjacente ao player que esteja livre
    /// </summary>
    private Vector3 FindBestAdjacentTileToPlayer(Vector3 currentPosition)
    {
        if (player == null) return Vector3.zero;
        
        Vector3 playerTile = GetTilePosition(player.transform.position);
        
        // Lista dos 8 tiles adjacentes ao player
        Vector3[] adjacentTiles = new Vector3[]
        {
            playerTile + Vector3.right,
            playerTile + Vector3.left,
            playerTile + Vector3.forward,
            playerTile + Vector3.back,
            playerTile + Vector3.right + Vector3.forward,
            playerTile + Vector3.right + Vector3.back,
            playerTile + Vector3.left + Vector3.forward,
            playerTile + Vector3.left + Vector3.back
        };
        
        // Ordena por proximidade à posição atual do monstro
        System.Array.Sort(adjacentTiles, (a, b) =>
        {
            float distA = Vector3.Distance(currentPosition, a);
            float distB = Vector3.Distance(currentPosition, b);
            return distA.CompareTo(distB);
        });
        
        // Retorna o primeiro tile livre e não reservado
        foreach (Vector3 tile in adjacentTiles)
        {
            if (!IsTileOccupied(tile) && !IsTileReserved(tile))
            {
                Debug.Log($"{gameObject.name} encontrou tile adjacente livre em {tile}");
                return tile;
            }
        }
        
        Debug.Log($"{gameObject.name} não encontrou tiles adjacentes livres ao redor do player");
        return Vector3.zero;
    }
    
    /// <summary>
    /// Verifica se há um monstro na direção especificada usando Raycast
    /// </summary>
    private bool HasMonsterInDirection(Vector3 from, Vector3 direction)
    {
        if (direction == Vector3.zero) return false;
        
        Ray ray = new Ray(from + Vector3.up * 0.5f, direction);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1f);
        
        foreach (RaycastHit hit in hits)
        {
            // Ignora ele mesmo
            if (hit.collider.gameObject == gameObject) continue;
            
            if (hit.collider.CompareTag("monster"))
            {
                Debug.Log($"{gameObject.name} detectou monstro {hit.collider.gameObject.name} no caminho!");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Encontra direção alternativa evitando monstros
    /// </summary>
    private Vector3 FindAlternativeDirectionAvoidingMonsters(Vector3 from, Vector3 to)
    {
        Vector3[] directions = new Vector3[]
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            Vector3.right + Vector3.forward, Vector3.right + Vector3.back,
            Vector3.left + Vector3.forward, Vector3.left + Vector3.back
        };

        // Ordena direções por proximidade ao alvo
        System.Array.Sort(directions, (a, b) =>
        {
            float distA = Vector3.Distance(from + a, to);
            float distB = Vector3.Distance(from + b, to);
            return distA.CompareTo(distB);
        });

        foreach (Vector3 dir in directions)
        {
            Vector3 normalized = new Vector3(Mathf.Sign(dir.x), 0, Mathf.Sign(dir.z));
            
            // Verifica se não há monstro nesta direção
            if (!HasMonsterInDirection(from, normalized) && CanMoveToDirection(from, normalized))
            {
                Debug.Log($"{gameObject.name} encontrou caminho alternativo: {normalized}");
                return normalized;
            }
        }

        Debug.Log($"{gameObject.name} não encontrou caminho alternativo sem monstros");
        return Vector3.zero;
    }

    private bool CanMoveToDirection(Vector3 from, Vector3 direction)
    {
        if (direction == Vector3.zero) return false;

        Ray ray = new Ray(from + Vector3.up * 0.5f, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1f))
        {
            // Ignora ele mesmo
            if (hit.collider.gameObject == gameObject)
                return true;
            
            // Verifica se é elevador
            if (enableElevatorSystem && elevatorLayer != -1)
            {
                int hitLayer = 1 << hit.collider.gameObject.layer;
                bool isElevator = (elevatorLayer.value & hitLayer) != 0;
                
                if (isElevator)
                {
                    // Pode subir no elevador
                    return true;
                }
            }
            
            // Bloqueia se encontrar cenário ou o player (monstros são verificados separadamente)
            if (hit.collider.CompareTag("scenario") || hit.collider.CompareTag("Player"))
            {
                return false;
            }
        }
        
        // Verifica adicional se há player no tile de destino
        Vector3 targetTile = from + direction;
        if (player != null && IsTileOccupiedByPlayer(targetTile))
        {
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Verifica se há outro monstro na mesma posição
    /// </summary>
    private bool CheckMonsterCollision()
    {
        Vector3 currentTile = GetTilePosition(transform.position);
        
        // Usa um raio maior para garantir detecção
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.6f);
        
        foreach (Collider col in colliders)
        {
            // Ignora ele mesmo
            if (col.gameObject == gameObject) continue;
            
            if (col.CompareTag("monster"))
            {
                // Verifica se realmente estão no mesmo tile (ignorando Y)
                Vector3 otherTile = GetTilePosition(col.transform.position);
                if (IsSameTile(currentTile, otherTile))
                {
                    Debug.LogError($"COLISÃO! {gameObject.name} está no mesmo tile que {col.gameObject.name}! Tile atual: ({currentTile.x}, {currentTile.z}) vs Outro: ({otherTile.x}, {otherTile.z})");
                    return true;
                }
            }
        }
        
        return false;
    }

    private bool IsTileOccupied(Vector3 tile)
    {
        // Usa um BoxCast mais preciso para verificar ocupação no plano XZ
        Vector3 boxCenter = new Vector3(tile.x, tile.y + 0.5f, tile.z);
        Vector3 boxSize = new Vector3(0.9f, 1f, 0.9f); // Caixa maior para melhor detecção
        
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize / 2f);
        
        foreach (Collider col in colliders)
        {
            // Ignora ele mesmo
            if (col.gameObject == gameObject) continue;
            
            // Verifica cenário, outros monstros e o player
            if (col.CompareTag("scenario") || col.CompareTag("monster") || col.CompareTag("Player"))
            {
                // Para monstros e player, verifica se realmente estão no mesmo tile XZ
                if (col.CompareTag("monster") || col.CompareTag("Player"))
                {
                    Vector3 otherTile = GetTilePosition(col.transform.position);
                    if (IsSameTile(tile, otherTile))
                    {
                        Debug.Log($"{gameObject.name} BLOQUEIO: {col.gameObject.name} está no tile ({tile.x}, {tile.z})");
                        return true;
                    }
                }
                else
                {
                    // Para cenário, apenas verifica se há colisão
                    return true;
                }
            }
        }

        return false;
    }
    
    /// <summary>
    /// Verifica especificamente se o player está no tile
    /// </summary>
    private bool IsTileOccupiedByPlayer(Vector3 tile)
    {
        if (player == null) return false;
        
        Vector3 playerTile = GetTilePosition(player.transform.position);
        float distance = Vector3.Distance(new Vector3(tile.x, 0, tile.z), new Vector3(playerTile.x, 0, playerTile.z));
        
        return distance < 0.5f; // Considera ocupado se player está a menos de 0.5 unidades
    }

    private Vector3 GetTilePosition(Vector3 position)
    {
        // Ajusta pela escala e offset do sistema de tiles
        Vector3 adjusted = (position - tileOffset) / tileSize;
        
        Vector3 tilePos = new Vector3(
            Mathf.Round(adjusted.x) * tileSize + tileOffset.x,
            position.y,
            Mathf.Round(adjusted.z) * tileSize + tileOffset.z
        );
        
        return tilePos;
    }
    
    /// <summary>
    /// Compara dois tiles ignorando a diferença de Y
    /// </summary>
    private bool IsSameTile(Vector3 tile1, Vector3 tile2)
    {
        float distX = Mathf.Abs(tile1.x - tile2.x);
        float distZ = Mathf.Abs(tile1.z - tile2.z);
        
        return distX < 0.1f && distZ < 0.1f;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    /// <summary>
    /// Verifica se há chão abaixo do monstro
    /// </summary>
    private void CheckFloor()
    {
        if (!enableFallSystem || isMoving || isFalling)
        {
            return;
        }

        Ray ray = new Ray(transform.position, Vector3.down);
        bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);

        Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, isGroundHit ? Color.green : Color.red, 0.5f);

        if (!isGroundHit)
        {
            Debug.Log($"{gameObject.name} sem chão detectado - Iniciando queda!");
            StartCoroutine(Fall());
        }
    }
    
    /// <summary>
    /// Coroutine que gerencia a queda do monstro
    /// </summary>
    private IEnumerator Fall()
    {
        if (isFalling) yield break;

        isFalling = true;
        bool originalCanMove = isMoving;
        isMoving = true; // Bloqueia outros movimentos durante queda

        // Continua caindo até encontrar chão
        while (true)
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            bool isGroundHit = Physics.Raycast(ray, out RaycastHit hit, 1f, groundLayer | elevatorLayer);
            
            Debug.DrawLine(ray.origin, ray.origin + Vector3.down * 1f, isGroundHit ? Color.green : Color.red, 0.5f);

            if (isGroundHit)
            {
                Debug.Log($"{gameObject.name} chão encontrado - Parando queda");
                break;
            }

            Debug.Log($"{gameObject.name} caindo de Y:{transform.position.y} para Y:{transform.position.y - 1f}");
            float newY = transform.position.y - 1f;
            Vector3 targetPos = new Vector3(transform.position.x, newY, transform.position.z);
            
            yield return StartCoroutine(MoveToTile(new Vector3(
                Mathf.Round(targetPos.x),
                targetPos.y,
                Mathf.Round(targetPos.z)
            )));
        }

        isMoving = originalCanMove;
        isFalling = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showTileGizmos) return;

        // Desenha alcance de perseguição
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Desenha tile atual
        Gizmos.color = Color.red;
        Vector3 tile = GetTilePosition(transform.position);
        Gizmos.DrawWireCube(tile, Vector3.one * tileSize * 0.9f);
        
        // Desenha os 8 tiles adjacentes se tiver player
        if (player != null)
        {
            Vector3 playerTile = GetTilePosition(player.transform.position);
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Verde transparente
            
            // Desenha tile do player
            Gizmos.DrawWireCube(playerTile, Vector3.one * tileSize);
            
            // Desenha tiles adjacentes
            Gizmos.color = new Color(0, 0, 1, 0.2f); // Azul transparente
            Vector3[] adjacentOffsets = new Vector3[]
            {
                Vector3.right * tileSize,
                Vector3.left * tileSize,
                Vector3.forward * tileSize,
                Vector3.back * tileSize,
                (Vector3.right + Vector3.forward) * tileSize,
                (Vector3.right + Vector3.back) * tileSize,
                (Vector3.left + Vector3.forward) * tileSize,
                (Vector3.left + Vector3.back) * tileSize
            };
            
            foreach (Vector3 offset in adjacentOffsets)
            {
                Gizmos.DrawWireCube(playerTile + offset, Vector3.one * tileSize * 0.8f);
            }
        }
    }
}