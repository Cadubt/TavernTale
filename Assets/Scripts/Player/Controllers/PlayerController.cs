using UnityEngine;

/// <summary>
/// Controlador principal do jogador - orquestra todos os componentes
/// Segue o padrão Component para melhor separação de responsabilidades
/// </summary>
public class PlayerController : MonoBehaviour
{
    // Componentes do jogador
    private Player.Controllers.PlayerMovement playerMovement;
    private Player.Controllers.PlayerInputHandler inputHandler;
    private Player.Data.PlayerStats playerStats;
    private Player.Abilities.MagicWaveAbility magicWaveAbility;
    
    // Magias de Ataque (Waves) - Elder Druid e Master Sorcerer
    private Player.Abilities.EnergyWaveAbility energyWaveAbility; // Tecla 1 - exevo vis hur
    private Player.Abilities.IceWaveAbility iceWaveAbility;       // Tecla 2 - exevo frigo hur
    private Player.Abilities.TerraWaveAbility terraWaveAbility;   // Tecla 3 - exevo tera hur
    private Player.Abilities.FireWaveAbility fireWaveAbility;     // Tecla 7 - exevo flam hur
    
    // Magias de Área - Master Sorcerer
    private Player.Abilities.GreatFireballAbility greatFireballAbility; // Tecla 4 - exevo gran flam
    private Player.Abilities.AvalancheAbility avalancheAbility;         // Tecla 5 - exevo mas frigo
    private Player.Abilities.StoneShowerAbility stoneShowerAbility;     // Tecla 6 - exevo mas tera

    // Propriedades públicas para compatibilidade com código legado
    public int health => playerStats != null ? playerStats.CurrentHealth : 0;
    public int mana => playerStats != null ? playerStats.CurrentMana : 0;

    private void Awake()
    {
        // Inicializa ou obtém componentes
        playerMovement = GetComponent<Player.Controllers.PlayerMovement>();
        inputHandler = GetComponent<Player.Controllers.PlayerInputHandler>();
        playerStats = GetComponent<Player.Data.PlayerStats>();
        magicWaveAbility = GetComponent<Player.Abilities.MagicWaveAbility>();
        
        // Carrega as magias de ataque
        energyWaveAbility = GetComponent<Player.Abilities.EnergyWaveAbility>();
        iceWaveAbility = GetComponent<Player.Abilities.IceWaveAbility>();
        terraWaveAbility = GetComponent<Player.Abilities.TerraWaveAbility>();
        fireWaveAbility = GetComponent<Player.Abilities.FireWaveAbility>();
        greatFireballAbility = GetComponent<Player.Abilities.GreatFireballAbility>();
        avalancheAbility = GetComponent<Player.Abilities.AvalancheAbility>();
        stoneShowerAbility = GetComponent<Player.Abilities.StoneShowerAbility>();

        // Adiciona componentes se não existirem
        if (playerMovement == null)
            playerMovement = gameObject.AddComponent<Player.Controllers.PlayerMovement>();
        
        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<Player.Controllers.PlayerInputHandler>();
        
        if (playerStats == null)
            playerStats = gameObject.AddComponent<Player.Data.PlayerStats>();
        
        if (energyWaveAbility == null)
        {
            energyWaveAbility = gameObject.AddComponent<Player.Abilities.EnergyWaveAbility>();
            Debug.Log("EnergyWaveAbility adicionado automaticamente ao Player");
        }
    }

    private void Update()
    {
        // Processa input de movimento
        if (playerMovement != null && !playerMovement.IsMoving)
        {
            Vector3 movementInput = inputHandler.GetMovementInput();
            if (movementInput != Vector3.zero)
            {
                // Verifica se Ctrl está pressionado (Left ou Right Ctrl)
                bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                
                if (isCtrlPressed)
                {
                    // Apenas aponta para a direção sem se mover
                    playerMovement.PointToDirection(movementInput);
                }
                else
                {
                    // Usa método específico do teclado que cancela pathfinding
                    playerMovement.TryMoveFromKeyboard(movementInput);
                }
            }
        }

        // Processa input de habilidades
        if (inputHandler.GetAbilityInput(KeyCode.F) && magicWaveAbility != null)
        {
            magicWaveAbility.Activate();
        }
        
        // === MAGIAS DE ATAQUE (WAVES) ===
        
        // Tecla 1 - Energy Wave (exevo vis hur)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha1) && energyWaveAbility != null)
        {
            energyWaveAbility.Activate();
        }
        
        // Tecla 2 - Ice Wave (exevo frigo hur)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha2) && iceWaveAbility != null)
        {
            iceWaveAbility.Activate();
        }
        
        // Tecla 3 - Terra Wave (exevo tera hur)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha3) && terraWaveAbility != null)
        {
            terraWaveAbility.Activate();
        }
        
        // === MAGIAS DE ÁREA ===
        
        // Tecla 4 - Great Fireball (exevo gran flam)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha4) && greatFireballAbility != null)
        {
            greatFireballAbility.Activate();
        }
        
        // Tecla 5 - Avalanche (exevo mas frigo)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha5) && avalancheAbility != null)
        {
            avalancheAbility.Activate();
        }
        
        // Tecla 6 - Stone Shower (exevo mas tera)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha6) && stoneShowerAbility != null)
        {
            stoneShowerAbility.Activate();
        }
        
        // Tecla 7 - Fire Wave (exevo flam hur)
        if (inputHandler.GetAbilityInput(KeyCode.Alpha7) && fireWaveAbility != null)
        {
            fireWaveAbility.Activate();
        }
    }

    // Métodos públicos para compatibilidade e integração
    public void TakeDamage(int damage)
    {
        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
        }
    }

    public Vector3 GetLastMoveDirection()
    {
        return playerMovement != null ? playerMovement.LastMoveDirection : Vector3.forward;
    }

    public bool IsMoving()
    {
        return playerMovement != null && playerMovement.IsMoving;
    }
}
