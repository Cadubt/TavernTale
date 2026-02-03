using UnityEngine;
using System;

namespace Player.Data
{
    /// <summary>
    /// Gerencia os atributos e stats do jogador (HP, Mana, etc)
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Atributos Base")]
        [SerializeField] private int maxHealth = 645;
        [SerializeField] private int maxMana = 550;
        
        private int currentHealth;
        private int currentMana;

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
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
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
    }
}
