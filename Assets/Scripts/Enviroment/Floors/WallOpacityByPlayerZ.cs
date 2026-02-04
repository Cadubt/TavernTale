using UnityEngine;

public class WallOpacityByPlayerZ : MonoBehaviour
{
    [Tooltip("Referência ao Transform do jogador (deixe vazio para buscar automaticamente)")]
    public Transform player;

    [Tooltip("Distância extra no eixo Z antes de aplicar transparência")]
    public float zOffset = 1f;

    [Tooltip("Opacidade quando o player estiver atrás do muro")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0.1f;

    [Tooltip("Tempo (segundos) para a transição de opacidade")]
    public float fadeDuration = 0.15f;

    private Renderer[] renderers;
    private Material[] cachedMaterials;   // evita instanciar materiais toda frame
    private float originalAlpha = 1f;

    private float currentOpacity; // estado atual
    private float targetOpacity;  // alvo a ser perseguido

    void Start()
    {
        // Busca automaticamente o player se não foi atribuído
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"WallOpacityByPlayerZ: Player encontrado automaticamente em '{playerObj.name}'");
            }
            else
            {
                Debug.LogWarning("WallOpacityByPlayerZ: Player não encontrado. Certifique-se que o GameObject do player tem a tag 'Player'.");
                enabled = false;
                return;
            }
        }

        // Pega todos os renderers deste muro e filhos
        renderers = GetComponentsInChildren<Renderer>();

        // Cacheia materiais (gera instâncias uma única vez)
        // e prepara o shader para transparência.
        // Também captura alpha original do primeiro material com _Color.
        var mats = new System.Collections.Generic.List<Material>();
        foreach (var r in renderers)
        {
            var rms = r.materials; // cria instâncias por renderer (uma vez só)
            foreach (var m in rms)
            {
                PrepareMaterialForTransparency(m);
                mats.Add(m);
            }
        }
        cachedMaterials = mats.ToArray();

        if (cachedMaterials.Length > 0 && cachedMaterials[0].HasProperty("_Color"))
            originalAlpha = cachedMaterials[0].color.a;

        // Inicializa estados
        currentOpacity = originalAlpha;
        targetOpacity = originalAlpha;
        ApplyOpacityImmediate(currentOpacity);
    }

    void Update()
    {
        if (player == null) return;

        // Sua condição: player dentro de um "corredor" em X e janela de Z
        bool shouldBeTransparent =
            (player.position.z < transform.position.z + zOffset + 1) &&
            (player.position.z > transform.position.z - 0f) &&
            (player.position.x < transform.position.x + 3f) &&
            (player.position.x > transform.position.x - 2f);

        targetOpacity = shouldBeTransparent ? transparentAlpha : originalAlpha;

        // Faz o fade suave em direção ao alvo
        if (!Mathf.Approximately(currentOpacity, targetOpacity))
        {
            float step = (fadeDuration <= 0f) ? 1f : (Time.deltaTime / fadeDuration);
            currentOpacity = Mathf.MoveTowards(currentOpacity, targetOpacity, step);
            ApplyOpacityImmediate(currentOpacity);
        }
    }

    private void ApplyOpacityImmediate(float opacity)
    {
        if (cachedMaterials == null) return;

        opacity = Mathf.Clamp01(opacity);

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            var mat = cachedMaterials[i];
            if (!mat || !mat.HasProperty("_Color")) continue;

            Color c = mat.color;
            c.a = opacity;
            mat.color = c;
        }
    }

/// <summary>
/// Configura o Standard Shader para modo Transparent (ou mantém se já estiver)
/// </summary>
/// <param name="mat">material a ser</param>
    private void PrepareMaterialForTransparency(Material mat)
    {
        if (!mat) return;

        // Configura o Standard Shader para modo Transparent (ou mantém se já estiver)
        // Esses parâmetros são padrão para transparência com alpha blending.
        mat.SetFloat("_Mode", 3); // 3 = Transparent no Standard Shader
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 25000;
    }
}
