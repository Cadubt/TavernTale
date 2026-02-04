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

        // Adiciona componentes se não existirem
        if (playerMovement == null)
            playerMovement = gameObject.AddComponent<Player.Controllers.PlayerMovement>();
        
        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<Player.Controllers.PlayerInputHandler>();
        
        if (playerStats == null)
            playerStats = gameObject.AddComponent<Player.Data.PlayerStats>();
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
