using UnityEngine;
using System.Collections;

namespace Player.Abilities
{
    /// <summary>
    /// Habilidade de onda mágica (cubos de dano)
    /// </summary>
    public class MagicWaveAbility : PlayerAbility
    {
        [Header("Configurações da Habilidade")]
        [SerializeField] private GameObject magicCubePrefab;
        [SerializeField] private int cubeCount = 9;
        [SerializeField] private float cubeDuration = 1f;
        [SerializeField] private int manaCost = 20;
        [SerializeField] private int damage = 50;

        private Data.PlayerStats playerStats;
        private Controllers.PlayerMovement playerMovement;

        private void Awake()
        {
            playerStats = GetComponent<Data.PlayerStats>();
            playerMovement = GetComponent<Controllers.PlayerMovement>();
        }

        public override void Activate()
        {
            // Verifica se tem mana suficiente
            if (playerStats != null && !playerStats.UseMana(manaCost))
            {
                Debug.Log("Mana insuficiente!");
                return;
            }

            if (playerMovement == null) return;

            Vector3 direction = playerMovement.LastMoveDirection;
            Vector3 startPosition = transform.position + direction;

            // Cria os cubos de dano em sequência
            for (int i = 0; i < cubeCount; i++)
            {
                Vector3 cubePosition = startPosition + direction * i;
                GameObject cube = Instantiate(magicCubePrefab, cubePosition, Quaternion.identity);
                
                // Adiciona componente de dano se necessário
                var damageComponent = cube.AddComponent<MagicCubeDamage>();
                damageComponent.damage = damage;
                
                StartCoroutine(DestroyCubeAfterTime(cube, cubeDuration));
            }
        }

        private IEnumerator DestroyCubeAfterTime(GameObject cube, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (cube != null)
            {
                Destroy(cube);
            }
        }
    }

    /// <summary>
    /// Componente auxiliar para aplicar dano aos monstros
    /// </summary>
    public class MagicCubeDamage : MonoBehaviour
    {
        public int damage = 50;
        private bool hasDealtDamage = false;

        private void OnTriggerEnter(Collider other)
        {
            if (hasDealtDamage) return;

            if (other.CompareTag("monster"))
            {
                // Aplica dano ao monstro
                var monster = other.GetComponent<IDamageable>();
                if (monster != null)
                {
                    monster.TakeDamage(damage);
                    hasDealtDamage = true;
                }
            }
        }
    }
}
