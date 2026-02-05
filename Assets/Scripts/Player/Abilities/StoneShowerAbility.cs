using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Player.Abilities
{
    /// <summary>
    /// Habilidade Stone Shower (exevo mas tera) - Cria uma chuva de pedras 3x3
    /// na direção que o player está olhando
    /// </summary>
    public class StoneShowerAbility : PlayerAbility
    {
        [Header("Configurações da Habilidade")]
        [SerializeField] public GameObject stonePrefab;
        [SerializeField] private int manaCost = 530;
        [SerializeField] private int damage = 130;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector3 tileOffset = Vector3.zero;
        [SerializeField] private float stoneDuration = 0.8f;
        
        [Header("Padrão Stone Shower")]
        [SerializeField] private int targetDistance = 5;
        
        private Data.PlayerStats playerStats;
        private Controllers.PlayerMovement playerMovement;
        private List<GameObject> activeStones = new List<GameObject>();

        private void Awake()
        {
            Debug.Log(">>> StoneShowerAbility Awake chamado! <<<");
            playerStats = GetComponent<Data.PlayerStats>();
            playerMovement = GetComponent<Controllers.PlayerMovement>();
        }
        
        private void Start()
        {
            Debug.Log($"=== StoneShowerAbility Start ===");
            
            // Sempre usa WaveEnergy
            stonePrefab = Resources.Load<GameObject>("WaveEnergy");
            
            if (stonePrefab != null)
            {
                Debug.Log($"✓ WaveEnergy carregado para Stone Shower");
            }
            else
            {
                Debug.LogError("❌ FALHA ao carregar WaveEnergy!");
            }
        }

        public override void Activate()
        {
            if (stonePrefab == null)
            {
                Debug.LogError("❌ Stone Shower Prefab não atribuído!");
                return;
            }
            
            // Verifica e recarrega componentes se necessário
            if (playerStats == null)
            {
                playerStats = GetComponent<Data.PlayerStats>();
                Debug.LogWarning("⚠ PlayerStats recarregado no Activate");
            }
            
            if (playerMovement == null)
            {
                playerMovement = GetComponent<Controllers.PlayerMovement>();
                Debug.LogWarning("⚠ PlayerMovement recarregado no Activate");
            }
            
            if (playerStats == null || playerMovement == null)
            {
                Debug.LogError("❌ Componentes necessários não encontrados!");
                return;
            }
            
            if (!playerStats.UseMana(manaCost))
            {
                Debug.Log("⚠ Mana insuficiente para Stone Shower!");
                return;
            }

            ClearActiveObjects();
            Vector3 direction = playerMovement.LastMoveDirection;
            direction = GetCardinalDirection(direction);
            
            CreateStoneShower(direction);
        }

        private void CreateStoneShower(Vector3 direction)
        {
            Vector3 playerTilePos = GetTilePosition(transform.position);
            Vector3 targetTile = playerTilePos + direction * tileSize * targetDistance;
            
            // Cria chuva de pedras 3x3 centrada no alvo
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 stonePos = targetTile + new Vector3(x * tileSize, 0, z * tileSize);
                    CreateStoneObject(stonePos);
                }
            }
            
            StartCoroutine(DestroyAfterTime(stoneDuration));
        }

        private void CreateStoneObject(Vector3 position)
        {
            if (stonePrefab == null) return;

            Vector3 spawnPosition = new Vector3(position.x, position.y + 0.1f, position.z);
            GameObject obj = Instantiate(stonePrefab, spawnPosition, Quaternion.Euler(45f, 0f, 0f));
            obj.transform.parent = null;
            obj.transform.localScale = Vector3.one * 1f;
            
            activeStones.Add(obj);

            Collider collider = obj.GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1.5f, 1.5f, 1.5f);
                boxCollider.isTrigger = true;
            }
            else
            {
                collider.isTrigger = true;
            }
            
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 100;
                Color color = sr.color;
                color.a = 1f;
                sr.color = color;
            }

            var damageComponent = obj.GetComponent<StoneShowerDamage>();
            if (damageComponent == null)
            {
                damageComponent = obj.AddComponent<StoneShowerDamage>();
            }
            damageComponent.damage = damage;
        }

        private IEnumerator DestroyAfterTime(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearActiveObjects();
        }

        private void ClearActiveObjects()
        {
            foreach (GameObject obj in activeStones)
            {
                if (obj != null) Destroy(obj);
            }
            activeStones.Clear();
        }

        private Vector3 GetTilePosition(Vector3 position)
        {
            Vector3 adjusted = (position - tileOffset) / tileSize;
            return new Vector3(
                Mathf.Round(adjusted.x) * tileSize + tileOffset.x,
                position.y,
                Mathf.Round(adjusted.z) * tileSize + tileOffset.z
            );
        }

        private Vector3 GetCardinalDirection(Vector3 direction)
        {
            direction.y = 0;
            direction.Normalize();
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                return direction.x > 0 ? Vector3.right : Vector3.left;
            else
                return direction.z > 0 ? Vector3.forward : Vector3.back;
        }

        private void OnDestroy()
        {
            ClearActiveObjects();
        }
    }

    public class StoneShowerDamage : MonoBehaviour
    {
        public int damage = 130;
        private HashSet<GameObject> damagedMonsters = new HashSet<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("monster") && !damagedMonsters.Contains(other.gameObject))
            {
                var monster = other.GetComponent<PocMonsterController>();
                if (monster != null)
                {
                    monster.TakeDamage(damage);
                    damagedMonsters.Add(other.gameObject);
                    Debug.Log($"✓ Stone Shower causou {damage} de dano em {other.gameObject.name}");
                }
            }
        }
    }
}
