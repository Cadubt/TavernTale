using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Abilities
{
    /// <summary>
    /// Fire Wave - exevo flam hur
    /// Elder Druid attack spell - creates a 3-tile wide wave of fire
    /// Mana Cost: 170 | Damage: 130
    /// </summary>
    public class FireWaveAbility : PlayerAbility
    {
        [Header("🔥 Fire Wave Settings")]
        [SerializeField] private GameObject wavePrefab;
        [SerializeField] private int manaCost = 170;
        [SerializeField] private float waveSpeed = 5f;
        [SerializeField] private float waveDistance = 4f;
        [SerializeField] private float waveDuration = 2f;
        [SerializeField] private int damage = 130;

        private Data.PlayerStats playerStats;
        private Controllers.PlayerMovement playerMovement;
        private List<GameObject> activeWaves = new List<GameObject>();

        private void Awake()
        {
            playerStats = GetComponent<Data.PlayerStats>();
            playerMovement = GetComponent<Controllers.PlayerMovement>();

            if (playerStats == null)
                Debug.LogError("❌ PlayerStats não encontrado no FireWaveAbility!");
            
            if (playerMovement == null)
                Debug.LogError("❌ PlayerMovement não encontrado no FireWaveAbility!");
        }

        private void Start()
        {
            // Tenta carregar o prefab automaticamente se não estiver atribuído
            if (wavePrefab == null)
            {
                wavePrefab = Resources.Load<GameObject>("WaveEnergy");
                if (wavePrefab != null)
                {
                    Debug.Log("✅ Fire Wave Prefab carregado automaticamente de Resources/WaveEnergy");
                }
            }
        }

        public override void Activate()
        {
            if (wavePrefab == null)
            {
                Debug.LogError("❌ Fire Wave Prefab não atribuído!");
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
                Debug.Log("⚠ Mana insuficiente para Fire Wave!");
                return;
            }

            ClearActiveObjects();
            Vector3 direction = playerMovement.LastMoveDirection;
            direction = GetCardinalDirection(direction);
            
            StartCoroutine(CreateWave(direction));
        }

        private IEnumerator CreateWave(Vector3 direction)
        {
            Vector3 playerPos = transform.position;
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x).normalized;

            for (int distance = 1; distance <= waveDistance; distance++)
            {
                Vector3 centerPos = playerPos + direction * distance;

                // Cria 3 cubos em linha (centro, esquerda, direita)
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 wavePos = centerPos + perpendicular * i;
                    CreateWaveObject(wavePos, direction);
                }

                yield return new WaitForSeconds(1f / waveSpeed);
            }
        }

        private void CreateWaveObject(Vector3 position, Vector3 direction)
        {
            Vector3 tilePosition = GetTilePosition(position);
            GameObject waveObj = Instantiate(wavePrefab, new Vector3(tilePosition.x, tilePosition.y + 0.1f, tilePosition.z), Quaternion.Euler(45f, 0f, 0f));
            
            // Configurar física
            Rigidbody rb = waveObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = waveObj.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Configurar colisor
            BoxCollider collider = waveObj.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = waveObj.AddComponent<BoxCollider>();
            }
            collider.isTrigger = true;
            collider.size = new Vector3(1.5f, 1.5f, 1.5f);

            // Adicionar componente de dano
            FireWaveDamage damageComponent = waveObj.AddComponent<FireWaveDamage>();
            damageComponent.damage = damage;

            // Configurar sprite (se houver SpriteRenderer)
            SpriteRenderer spriteRenderer = waveObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.4f, 0.1f, 0.8f); // Cor laranja-fogo
            }

            activeWaves.Add(waveObj);
            Destroy(waveObj, waveDuration);
        }

        private Vector3 GetTilePosition(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x),
                position.y,
                Mathf.Round(position.z)
            );
        }

        private Vector3 GetCardinalDirection(Vector3 direction)
        {
            float absX = Mathf.Abs(direction.x);
            float absZ = Mathf.Abs(direction.z);

            if (absX > absZ)
            {
                return new Vector3(Mathf.Sign(direction.x), 0, 0);
            }
            else
            {
                return new Vector3(0, 0, Mathf.Sign(direction.z));
            }
        }

        private void ClearActiveObjects()
        {
            foreach (GameObject obj in activeWaves)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            activeWaves.Clear();
        }

        private void OnDestroy()
        {
            ClearActiveObjects();
        }
    }

    /// <summary>
    /// Componente de dano para Fire Wave
    /// </summary>
    public class FireWaveDamage : MonoBehaviour
    {
        public int damage = 130;
        private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Monster") && !hitTargets.Contains(other.gameObject))
            {
                hitTargets.Add(other.gameObject);
                
                // TODO: Aplicar dano ao monstro
                Debug.Log($"🔥 Fire Wave causou {damage} de dano em {other.gameObject.name}");
                
                // Quando o sistema de vida dos monstros estiver pronto:
                // MonsterHealth health = other.GetComponent<MonsterHealth>();
                // if (health != null) health.TakeDamage(damage);
            }
        }
    }
}
