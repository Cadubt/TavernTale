using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Player.Abilities
{
    /// <summary>
    /// Habilidade Energy Wave (exevo vis hur) - Cria uma onda de energia
    /// na direção que o player está olhando, respeitando o sistema de SQMs
    /// </summary>
    public class EnergyWaveAbility : PlayerAbility
    {
        [Header("Configurações da Habilidade")]
        [SerializeField] public GameObject cubePrefab; // Público temporariamente para debug
        [SerializeField] private int manaCost = 20; // Custo de mana do Tibia
        [SerializeField] private int damage = 150; // Dano base
        [SerializeField] private float tileSize = 1f; // Tamanho do tile (igual ao do monstro)
        [SerializeField] private Vector3 tileOffset = Vector3.zero; // Offset do grid
        [SerializeField] private float cubeDuration = 1f; // Tempo que o cubo fica visível
        [SerializeField] private float spawnDelay = 0.05f; // Delay entre cada cubo aparecer
        
        [Header("Padrão Energy Wave")]
        [SerializeField] private int waveDistance = 4; // Distância da onda (em tiles)
        
        private Data.PlayerStats playerStats;
        private Controllers.PlayerMovement playerMovement;
        private List<GameObject> activeWaveCubes = new List<GameObject>();

        private void Awake()
        {
            playerStats = GetComponent<Data.PlayerStats>();
            playerMovement = GetComponent<Controllers.PlayerMovement>();
        }
        
        private void Start()
        {
            Debug.Log($"=== EnergyWaveAbility Start (Debug Completo) ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"cubePrefab é null? {cubePrefab == null}");
            
            // Se cubePrefab não está atribuído, tenta carregar via Resources
            if (cubePrefab == null)
            {
                Debug.LogWarning("⚠ Tentando carregar WaveEnergy via Resources...");
                cubePrefab = Resources.Load<GameObject>("WaveEnergy");
                
                if (cubePrefab != null)
                {
                    Debug.Log("✓ WaveEnergy carregado com sucesso via Resources!");
                }
                else
                {
                    Debug.LogError("❌ FALHA ao carregar WaveEnergy via Resources!");
                    Debug.LogError("SOLUÇÃO: Copie o arquivo 'WaveEnergy.prefab' para a pasta 'Assets/Resources/'");
                    return;
                }
            }
            
            if (cubePrefab != null)
            {
                Debug.Log($"✓ cubePrefab.name: {cubePrefab.name}");
                Debug.Log($"✓ cubePrefab ativo? {cubePrefab.activeSelf}");
                Debug.Log($"✓ cubePrefab tem Transform? {cubePrefab.transform != null}");
                Debug.Log($"✓ cubePrefab tem parent? {cubePrefab.transform.parent != null}");
                
                if (cubePrefab.transform.parent != null)
                {
                    Debug.LogWarning($"⚠ ATENÇÃO: Cube Prefab '{cubePrefab.name}' tem parent '{cubePrefab.transform.parent.name}'!");
                    Debug.LogWarning("Isso indica que está sendo usado um objeto da hierarquia ao invés de um prefab!");
                }
                
                // Verifica componentes
                var sr = cubePrefab.GetComponent<SpriteRenderer>();
                var col = cubePrefab.GetComponent<Collider>();
                Debug.Log($"✓ Tem SpriteRenderer? {sr != null}" + (sr != null ? $" (Sprite: {sr.sprite?.name})" : ""));
                Debug.Log($"✓ Tem Collider? {col != null}" + (col != null ? $" (Tipo: {col.GetType().Name})" : ""));
            }
        }

        public override void Activate()
        {
            Debug.Log("=== Energy Wave Ativada! ===");
            
            if (cubePrefab == null)
            {
                Debug.LogError("❌ Cube Prefab não está atribuído! Arraste o prefab 'WaveEnergy' da pasta 'Prefabs/Player/Speels/' no campo 'Cube Prefab' do componente EnergyWaveAbility no Inspector.");
                return;
            }
            
            // Verifica se não é um objeto de cena (filho do player)
            if (cubePrefab.transform.parent != null)
            {
                Debug.LogError("❌ ERRO: O Cube Prefab está apontando para um objeto FILHO do Player, não para um prefab independente!");
                Debug.LogError("SOLUÇÃO: No Inspector do PlayerPrefab, no componente EnergyWaveAbility:");
                Debug.LogError("1. Clique no círculo ao lado de 'Cube Prefab'");
                Debug.LogError("2. Na janela que abrir, procure por 'WaveEnergy' na aba 'Assets' (não 'Scene')");
                Debug.LogError("3. Selecione o prefab da pasta 'Prefabs/Player/Speels/WaveEnergy'");
                return;
            }
            
            Debug.Log($"✓ Cube Prefab encontrado: {cubePrefab.name}");
            
            // Verifica se tem mana suficiente
            if (playerStats != null && !playerStats.UseMana(manaCost))
            {
                Debug.Log("⚠ Mana insuficiente para Energy Wave!");
                return;
            }
            
            Debug.Log($"✓ Mana consumida: {manaCost}");

            if (playerMovement == null)
            {
                Debug.LogWarning("PlayerMovement não encontrado!");
                return;
            }

            // Limpa cubos anteriores se existirem
            ClearActiveCubes();

            // Pega a direção que o player está olhando
            Vector3 direction = playerMovement.LastMoveDirection;
            
            // Normaliza para direção cardinal (N, S, E, W)
            direction = GetCardinalDirection(direction);
            
            Debug.Log($"✓ Direção: {direction}");

            // Inicia a corrotina para criar a onda
            StartCoroutine(CreateEnergyWave(direction));
        }

        /// <summary>
        /// Cria a onda de energia com padrão de propagação
        /// </summary>
        private IEnumerator CreateEnergyWave(Vector3 direction)
        {
            Vector3 playerTilePos = GetTilePosition(transform.position);
            
            Debug.Log($"Player position: {transform.position}, Tile position: {playerTilePos}, Direction: {direction}");

            // Energy Wave cria um padrão de 3 tiles de largura que se propaga na direção
            // Padrão do Tibia:
            //   X X X
            //   X P X  (P = Player)
            //   X X X
            // Propaga-se na direção olhada

            for (int distance = 1; distance <= waveDistance; distance++)
            {
                // Tile central na direção
                Vector3 centerTile = playerTilePos + direction * tileSize * distance;
                
                Debug.Log($"Distance {distance}: Creating center cube at {centerTile}");
                
                // Cria cubo central
                CreateWaveCube(centerTile);

                // Cria cubos laterais (perpendiculares à direção)
                Vector3 perpendicular = GetPerpendicularDirection(direction);
                Vector3 leftTile = centerTile + perpendicular * tileSize;
                Vector3 rightTile = centerTile - perpendicular * tileSize;
                
                Debug.Log($"Distance {distance}: Creating left cube at {leftTile}");
                Debug.Log($"Distance {distance}: Creating right cube at {rightTile}");
                
                CreateWaveCube(leftTile);
                CreateWaveCube(rightTile);

                // Aguarda antes de criar a próxima linha
                yield return new WaitForSeconds(spawnDelay);
            }

            // Inicia timer para destruir todos os cubos
            StartCoroutine(DestroyWaveAfterTime(cubeDuration));
        }

        /// <summary>
        /// Cria um cubo de energia em uma posição específica
        /// </summary>
        private void CreateWaveCube(Vector3 position)
        {
            if (cubePrefab == null)
            {
                Debug.LogError("cubePrefab não está atribuído! Configure o prefab no Inspector do PlayerPrefab.");
                return;
            }

            // Mantém a posição X e Z do tile calculado, usa Y do chão + pequeno offset
            Vector3 spawnPosition = new Vector3(position.x, position.y + 0.1f, position.z);
            
            // Rotação para ficar de frente para a câmera (billboard effect)
            // 45 graus no eixo X para a visão isométrica do Tibia
            GameObject cube = Instantiate(cubePrefab, spawnPosition, Quaternion.Euler(45f, 0f, 0f));
            
            // Remove da hierarquia do player (se estiver anexado)
            cube.transform.parent = null;
            
            // Escala maior para garantir visibilidade
            cube.transform.localScale = Vector3.one * 1f;
            
            activeWaveCubes.Add(cube);
            
            Debug.Log($"✓ Energy Wave CRIADO em {spawnPosition}");

            // Garante que tem um Collider configurado como Trigger
            Collider collider = cube.GetComponent<Collider>();
            if (collider == null)
            {
                // Adiciona BoxCollider se não tiver nenhum collider
                BoxCollider boxCollider = cube.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1.5f, 1.5f, 1.5f); // Collider maior para garantir detecção
                boxCollider.isTrigger = true;
                Debug.Log("✓ BoxCollider adicionado");
            }
            else
            {
                collider.isTrigger = true;
                Debug.Log($"✓ Collider existente configurado como Trigger: {collider.GetType().Name}");
            }
            
            // Adiciona Rigidbody para garantir detecção de colisões
            Rigidbody rb = cube.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = cube.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true; // Não é afetado por física
            rb.useGravity = false; // Não cai
            Debug.Log("✓ Rigidbody configurado");
            
            // Configura SpriteRenderer para garantir visibilidade
            SpriteRenderer sr = cube.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 100; // Bem acima de outros elementos
                
                // Garante que a cor está visível (alpha = 1)
                Color color = sr.color;
                color.a = 1f;
                sr.color = color;
                
                Debug.Log($"✓ Sprite: {sr.sprite?.name}, Rotation: {cube.transform.eulerAngles}");
            }
            else
            {
                Debug.LogError("❌ SpriteRenderer NÃO encontrado! Verifique o prefab WaveEnergy.");
            }

            // Adiciona componente de dano
            var damageComponent = cube.GetComponent<EnergyWaveDamage>();
            if (damageComponent == null)
            {
                damageComponent = cube.AddComponent<EnergyWaveDamage>();
            }
            damageComponent.damage = damage;
        }

        /// <summary>
        /// Destroi todos os cubos da onda após um tempo
        /// </summary>
        private IEnumerator DestroyWaveAfterTime(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearActiveCubes();
        }

        /// <summary>
        /// Remove todos os cubos ativos
        /// </summary>
        private void ClearActiveCubes()
        {
            foreach (GameObject cube in activeWaveCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
            activeWaveCubes.Clear();
        }

        /// <summary>
        /// Converte posição do mundo para posição de tile
        /// </summary>
        private Vector3 GetTilePosition(Vector3 position)
        {
            Vector3 adjusted = (position - tileOffset) / tileSize;
            
            Vector3 tilePos = new Vector3(
                Mathf.Round(adjusted.x) * tileSize + tileOffset.x,
                position.y,
                Mathf.Round(adjusted.z) * tileSize + tileOffset.z
            );
            
            return tilePos;
        }

        /// <summary>
        /// Normaliza direção para uma das 4 direções cardinais
        /// </summary>
        private Vector3 GetCardinalDirection(Vector3 direction)
        {
            // Normaliza a direção
            direction.y = 0;
            direction.Normalize();

            // Determina a direção cardinal mais próxima
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                // Direção predominante é X (Leste ou Oeste)
                return direction.x > 0 ? Vector3.right : Vector3.left;
            }
            else
            {
                // Direção predominante é Z (Norte ou Sul)
                return direction.z > 0 ? Vector3.forward : Vector3.back;
            }
        }

        /// <summary>
        /// Retorna a direção perpendicular (90 graus)
        /// </summary>
        private Vector3 GetPerpendicularDirection(Vector3 direction)
        {
            // Rotaciona 90 graus no plano XZ
            return new Vector3(-direction.z, 0, direction.x);
        }

        private void OnDestroy()
        {
            // Limpa cubos ao destruir o componente
            ClearActiveCubes();
        }
    }

    /// <summary>
    /// Componente que aplica dano aos monstros
    /// </summary>
    public class EnergyWaveDamage : MonoBehaviour
    {
        public int damage = 150;
        private HashSet<GameObject> damagedMonsters = new HashSet<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"EnergyWave colidiu com: {other.gameObject.name}, Tag: {other.tag}");
            
            // Verifica se é um monstro e se ainda não recebeu dano
            if (other.CompareTag("monster") && !damagedMonsters.Contains(other.gameObject))
            {
                var monster = other.GetComponent<PocMonsterController>();
                if (monster != null)
                {
                    monster.TakeDamage(damage);
                    damagedMonsters.Add(other.gameObject);
                    Debug.Log($"✓ Energy Wave causou {damage} de dano em {other.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠ Monstro {other.gameObject.name} não tem PocMonsterController!");
                }
            }
        }
    }
}
