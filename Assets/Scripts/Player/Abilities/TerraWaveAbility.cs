using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Player.Abilities
{
    /// <summary>
    /// Habilidade Terra Wave (exevo tera hur) - Cria uma onda de terra
    /// na direção que o player está olhando
    /// </summary>
    public class TerraWaveAbility : PlayerAbility
    {
        [Header("Configurações da Habilidade")]
        [SerializeField] public GameObject wavePrefab;
        [SerializeField] private int manaCost = 170;
        [SerializeField] private int damage = 160;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector3 tileOffset = Vector3.zero;
        [SerializeField] private float waveDuration = 1f;
        [SerializeField] private float spawnDelay = 0.05f;
        
        [Header("Padrão Terra Wave")]
        [SerializeField] private int waveDistance = 4;
        
        private Data.PlayerStats playerStats;
        private Controllers.PlayerMovement playerMovement;
        private List<GameObject> activeWaveObjects = new List<GameObject>();

        private void Awake()
        {
            Debug.Log(">>> TerraWaveAbility Awake chamado! <<<");
            playerStats = GetComponent<Data.PlayerStats>();
            playerMovement = GetComponent<Controllers.PlayerMovement>();
        }
        
        private void Start()
        {
            Debug.Log($"=== TerraWaveAbility Start ===");
            
            // Sempre usa WaveEnergy
            wavePrefab = Resources.Load<GameObject>("WaveEnergy");
            
            if (wavePrefab != null)
            {
                Debug.Log($"✓ WaveEnergy carregado para Terra Wave");
            }
            else
            {
                Debug.LogError("❌ FALHA ao carregar WaveEnergy!");
            }
        }

        public override void Activate()
        {
            if (wavePrefab == null)
            {
                Debug.LogError("❌ Terra Wave Prefab não atribuído!");
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
                Debug.Log("⚠ Mana insuficiente para Terra Wave!");
                return;
            }

            ClearActiveObjects();
            Vector3 direction = playerMovement.LastMoveDirection;
            direction = GetCardinalDirection(direction);
            
            StartCoroutine(CreateWave(direction));
        }

        private IEnumerator CreateWave(Vector3 direction)
        {
            Vector3 playerTilePos = GetTilePosition(transform.position);

            for (int distance = 1; distance <= waveDistance; distance++)
            {
                Vector3 centerTile = playerTilePos + direction * tileSize * distance;
                CreateWaveObject(centerTile);

                Vector3 perpendicular = GetPerpendicularDirection(direction);
                CreateWaveObject(centerTile + perpendicular * tileSize);
                CreateWaveObject(centerTile - perpendicular * tileSize);

                yield return new WaitForSeconds(spawnDelay);
            }

            StartCoroutine(DestroyAfterTime(waveDuration));
        }

        private void CreateWaveObject(Vector3 position)
        {
            if (wavePrefab == null) return;

            Vector3 spawnPosition = new Vector3(position.x, position.y + 0.1f, position.z);
            GameObject obj = Instantiate(wavePrefab, spawnPosition, Quaternion.Euler(45f, 0f, 0f));
            obj.transform.parent = null;
            obj.transform.localScale = Vector3.one * 1f;
            
            activeWaveObjects.Add(obj);

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

            var damageComponent = obj.GetComponent<TerraWaveDamage>();
            if (damageComponent == null)
            {
                damageComponent = obj.AddComponent<TerraWaveDamage>();
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
            foreach (GameObject obj in activeWaveObjects)
            {
                if (obj != null) Destroy(obj);
            }
            activeWaveObjects.Clear();
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

        private Vector3 GetPerpendicularDirection(Vector3 direction)
        {
            return new Vector3(-direction.z, 0, direction.x);
        }

        private void OnDestroy()
        {
            ClearActiveObjects();
        }
    }

    public class TerraWaveDamage : MonoBehaviour
    {
        public int damage = 160;
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
                    Debug.Log($"✓ Terra Wave causou {damage} de dano em {other.gameObject.name}");
                }
            }
        }
    }
}
