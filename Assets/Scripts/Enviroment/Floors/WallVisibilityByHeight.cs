using UnityEngine;

public class WallVisibilityByHeight : MonoBehaviour
{
    [Tooltip("Referência ao Transform do jogador")]
    public Transform player;

    [Tooltip("Altura em Y a partir da qual esta parede será visível")]
    public float minYToShow = 2.88f;

    private bool isVisible = true;
    private Renderer[] renderers;

    void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("Player não atribuído ao WallVisibilityByHeight.");
            return;
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
