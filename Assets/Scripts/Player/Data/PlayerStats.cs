using UnityEngine;
using System;
using System.Collections;

namespace Player.Data
{
    /// <summary>
    /// Gerencia os atributos e stats do jogador (HP, Mana, etc)
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Atributos Base")]
        [SerializeField] private int maxHealth = 645;
        [SerializeField] private int maxMana = 2550;
        
        [Header("Regeneração")]
        [SerializeField] private int healthRegenAmount = 10;
        [SerializeField] private float healthRegenInterval = 5f;
        [SerializeField] private int manaRegenAmount = 50;
        [SerializeField] private float manaRegenInterval = 3f;
        
        [Header("Damage Display")]
        [SerializeField] private float damageTextYOffset = 0.8f;
        [SerializeField] private float damageTextSize = 0.025f;
        [SerializeField] private float damageTextDuration = 0.5f;
        
        private int currentHealth;
        private int currentMana;
        
        // Damage text components
        private GameObject damageTextObject;
        private TextMesh damageTextMesh;
        private Coroutine hideTextCoroutine;
        private SpriteRenderer spriteRenderer;

        // Events para notificar mudanças
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action<int, int> OnManaChanged;   // current, max
        public event Action OnPlayerDeath;

        // Propriedades públicas (read-only)
        public int CurrentHealth => currentHealth;
        public int CurrentMana => currentMana;
        public int MaxHealth => maxHealth;
        public int MaxMana => maxMana;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentMana = maxMana;
            
            // Get or find SpriteRenderer
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            
            // Cria o texto de dano
            CreateDamageText();
        }

        private void Start()
        {
            // Inicia as corrotinas de regeneração
            StartCoroutine(HealthRegeneration());
            StartCoroutine(ManaRegeneration());
        }

        private IEnumerator HealthRegeneration()
        {
            while (true)
            {
                yield return new WaitForSeconds(healthRegenInterval);
                
                if (currentHealth < maxHealth)
                {
                    Heal(healthRegenAmount);
                }
            }
        }

        private IEnumerator ManaRegeneration()
        {
            while (true)
            {
                yield return new WaitForSeconds(manaRegenInterval);
                
                if (currentMana < maxMana)
                {
                    RestoreMana(manaRegenAmount);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            // Show damage text
            ShowDamage(damage);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                OnPlayerDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(maxHealth, currentHealth);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public bool UseMana(int amount)
        {
            if (currentMana < amount) return false;

            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }

        public void RestoreMana(int amount)
        {
            if (amount <= 0) return;

            currentMana += amount;
            currentMana = Mathf.Min(maxMana, currentMana);
            
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
        
        private void ShowDamage(int damage)
        {
            if (damageTextMesh == null || damageTextObject == null) return;
            
            // Para a corrotina anterior se existir
            if (hideTextCoroutine != null)
            {
                StopCoroutine(hideTextCoroutine);
            }
            
            // Atualiza o texto com o novo dano
            damageTextMesh.text = damage.ToString();
            damageTextObject.SetActive(true);
            
            // Inicia nova corrotina para esconder após o delay
            hideTextCoroutine = StartCoroutine(HideDamageTextAfterDelay());
        }
        
        private void CreateDamageText()
        {
            // Cria GameObject para o texto
            damageTextObject = new GameObject("DamageText");
            
            // Se encontrou SpriteRenderer, usa o transform dele como parent
            // Caso contrário, usa o transform principal
            Transform parentTransform = (spriteRenderer != null) ? spriteRenderer.transform : transform;
            damageTextObject.transform.SetParent(parentTransform);
            damageTextObject.transform.localPosition = new Vector3(0, damageTextYOffset, 0);
            damageTextObject.transform.localRotation = Quaternion.identity; // Mesma rotação do sprite
            
            // Aplica escala para compensar a distorção da perspectiva
            // Estica verticalmente e comprime horizontalmente
            damageTextObject.transform.localScale = new Vector3(1f, 1.2f, 1f); // 2x mais alto
            
            // Adiciona TextMesh
            damageTextMesh = damageTextObject.AddComponent<TextMesh>();
            damageTextMesh.text = "";
            damageTextMesh.fontSize = 50;
            damageTextMesh.characterSize = damageTextSize;
            damageTextMesh.anchor = TextAnchor.MiddleCenter;
            damageTextMesh.alignment = TextAlignment.Center;
            damageTextMesh.color = Color.white; // White color for player
            
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
        
        private IEnumerator HideDamageTextAfterDelay()
        {
            yield return new WaitForSeconds(damageTextDuration);
            if (damageTextObject != null)
            {
                damageTextObject.SetActive(false);
            }
        }
        
        private void Update()
        {
            // Make damage text face camera
            if (damageTextObject != null && damageTextObject.activeSelf && Camera.main != null)
            {
                damageTextObject.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
