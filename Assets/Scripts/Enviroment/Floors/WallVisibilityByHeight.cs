using UnityEngine;

public class WallVisibilityByHeight : MonoBehaviour
{
    [Tooltip("Referência ao Transform do jogador (deixe vazio para buscar automaticamente)")]
    public Transform player;

    [Tooltip("Altura em Y a partir da qual esta parede será visível")]
    private float minYToShow = 7.9f;

    private bool isVisible = true;
    private Renderer[] renderers;

    void Start()
    {
        // Busca automaticamente o player se não foi atribuído
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"WallVisibilityByHeight: Player encontrado automaticamente em '{playerObj.name}'");
            }
            else
            {
                Debug.LogWarning("WallVisibilityByHeight: Player não encontrado. Certifique-se que o GameObject do player tem a tag 'Player'.");
                return;
            }
        }

        // Pega todos os renderers deste objeto e filhos
        renderers = GetComponentsInChildren<Renderer>();

        // Inicializa visibilidade
        UpdateWallVisibility();
    }

    void Update()
    {
        if (player == null) return;

        UpdateWallVisibility();
    }

    private void UpdateWallVisibility()
    {
        float playerY = player.position.y;

        if (playerY >= minYToShow && !isVisible)
        {
            SetWallVisible(true);
        }
        else if (playerY < minYToShow && isVisible)
        {
            SetWallVisible(false);
        }
    }

    private void SetWallVisible(bool visible)
    {
        if (renderers == null) return;

        foreach (var renderer in renderers)
        {
            renderer.enabled = visible;
        }

        isVisible = visible;
    }
}
