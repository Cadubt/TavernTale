using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Player.Abilities
{
    /// <summary>
    /// Habilidade Great Fireball (exevo gran flam) - Cria uma explosão de fogo 3x3
    /// na direção que o player está olhando
    /// </summary>
    public class GreatFireballAbility : PlayerAbility
    {
        [Header("Configurações da Habilidade")]
        [SerializeField] public GameObject explosionPrefab;
        [SerializeField] private int manaCost = 530;
        [SerializeField] private int damage = 120;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector3 tileOffset = Vector3.zero;
        [SerializeField] private float explosionDuration = 0.8f;
        
        [Header("Padrão Great Fireball")]
        [SerializeField] private int targetDistance = 5; // Distância do alvo
        
        private Data.PlayerStats playerStats;
        private Controllers.PlayerMovement playerMovement;
        private List<GameObject> activeExplosions = new List<GameObject>();

        private void Awake()
        {
            Debug.Log(">>> GreatFireballAbility Awake chamado! <<<");
            playerStats = GetComponent<Data.PlayerStats>();
            playerMovement = GetComponent<Controllers.PlayerMovement>();
        }
        
        private void Start()
        {
            Debug.Log($"=== GreatFireballAbility Start ===");
            
            // Sempre usa WaveEnergy
            explosionPrefab = Resources.Load<GameObject>("WaveEnergy");
            
            if (explosionPrefab != null)
            {
                Debug.Log($"✓ WaveEnergy carregado para Great Fireball");
            }
            else
            {
                Debug.LogError("❌ FALHA ao carregar WaveEnergy!");
            }
        }

        public override void Activate()
        {
            if (explosionPrefab == null)
            {
                Debug.LogError("❌ Great Fireball Prefab não atribuído!");
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
                Debug.Log("⚠ Mana insuficiente para Great Fireball!");
                return;
            }

            ClearActiveObjects();
            Vector3 direction = playerMovement.LastMoveDirection;
            direction = GetCardinalDirection(direction);
            
            CreateExplosion(direction);
        }

        private void CreateExplosion(Vector3 direction)
        {
            Vector3 playerTilePos = GetTilePosition(transform.position);
            Vector3 targetTile = playerTilePos + direction * tileSize * targetDistance;
            
            // Cria explosão 3x3 centrada no alvo
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 explosionPos = targetTile + new Vector3(x * tileSize, 0, z * tileSize);
                    CreateExplosionObject(explosionPos);
                }
            }
            
            StartCoroutine(DestroyAfterTime(explosionDuration));
        }

        private void CreateExplosionObject(Vector3 position)
        {
            if (explosionPrefab == null) return;

            Vector3 spawnPosition = new Vector3(position.x, position.y + 0.1f, position.z);
            GameObject obj = Instantiate(explosionPrefab, spawnPosition, Quaternion.Euler(45f, 0f, 0f));
            obj.transform.parent = null;
            obj.transform.localScale = Vector3.one * 1f;
            
            activeExplosions.Add(obj);

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

            var damageComponent = obj.GetComponent<GreatFireballDamage>();
            if (damageComponent == null)
            {
                damageComponent = obj.AddComponent<GreatFireballDamage>();
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
            foreach (GameObject obj in activeExplosions)
            {
                if (obj != null) Destroy(obj);
            }
            activeExplosions.Clear();
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

    public class GreatFireballDamage : MonoBehaviour
    {
        public int damage = 120;
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
                    Debug.Log($"✓ Great Fireball causou {damage} de dano em {other.gameObject.name}");
                }
            }
        }
    }
}
