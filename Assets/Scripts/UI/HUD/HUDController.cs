using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    public UIDocument hudDocument;
    public PlayerController playerController;
    public Label healthLabel;
    public Label manaLabel;
    
    // FPS Counter
    private Label telemetryTitleLabel;
    private Label fpsLabel;
    private Label msLabel;
    private Label memoryLabel;
    private bool showFPS = false;
    private float deltaTime = 0.0f;
    
    // FPS Graph
    private VisualElement fpsGraphContainer;
    private List<float> fpsHistory = new List<float>();
    private const int FPS_HISTORY_SIZE = 120; // 120 amostras para ~30 segundos (4 amostras/segundo)
    private float graphUpdateTimer = 0f;
    private const float GRAPH_UPDATE_INTERVAL = 0.25f; // Atualiza 4x por segundo
    
    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        healthLabel = hudDocument.rootVisualElement.Q<Label>("life");
        manaLabel = hudDocument.rootVisualElement.Q<Label>("mana");
        
        // Cria título de telemetria
        telemetryTitleLabel = hudDocument.rootVisualElement.Q<Label>("telemetryTitle");
        if (telemetryTitleLabel == null)
        {
            telemetryTitleLabel = new Label("Tela de Telemetria");
            telemetryTitleLabel.name = "telemetryTitle";
            telemetryTitleLabel.style.position = Position.Absolute;
            telemetryTitleLabel.style.top = 10;
            telemetryTitleLabel.style.right = 10;
            telemetryTitleLabel.style.color = Color.cyan;
            telemetryTitleLabel.style.fontSize = 8;
            telemetryTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hudDocument.rootVisualElement.Add(telemetryTitleLabel);
        }
        telemetryTitleLabel.style.display = DisplayStyle.None;
        
        // Cria label para FPS se não existir
        fpsLabel = hudDocument.rootVisualElement.Q<Label>("fps");
        if (fpsLabel == null)
        {
            fpsLabel = new Label();
            fpsLabel.name = "fps";
            fpsLabel.style.position = Position.Absolute;
            fpsLabel.style.top = 20;
            fpsLabel.style.right = 10;
            fpsLabel.style.color = Color.yellow;
            fpsLabel.style.fontSize = 8;
            fpsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hudDocument.rootVisualElement.Add(fpsLabel);
        }
        fpsLabel.style.display = DisplayStyle.None;
        // Cria container do gráfico
        CreateFPSGraph();
        
        // Cria label para MS (milliseconds)
        msLabel = hudDocument.rootVisualElement.Q<Label>("ms");
        if (msLabel == null)
        {
            msLabel = new Label();
            msLabel.name = "ms";
            msLabel.style.position = Position.Absolute;
            msLabel.style.top = 118;
            msLabel.style.right = 10;
            msLabel.style.color = Color.white;
            msLabel.style.fontSize = 8;
            msLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hudDocument.rootVisualElement.Add(msLabel);
        }
        msLabel.style.display = DisplayStyle.None;
        
        // Cria label para Memory Usage
        memoryLabel = hudDocument.rootVisualElement.Q<Label>("memory");
        if (memoryLabel == null)
        {
            memoryLabel = new Label();
            memoryLabel.name = "memory";
            memoryLabel.style.position = Position.Absolute;
            memoryLabel.style.top = 130;
            memoryLabel.style.right = 10;
            memoryLabel.style.color = Color.white;
            memoryLabel.style.fontSize = 8;
            memoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hudDocument.rootVisualElement.Add(memoryLabel);
        }
        memoryLabel.style.display = DisplayStyle.None;


        
        
    }

    private void Update()
    {
        UpdateHealth(playerController.health);
        UpdateMana(playerController.mana);
        
        // Toggle FPS com F7
        if (Input.GetKeyDown(KeyCode.F7))
        {
            showFPS = !showFPS;
            telemetryTitleLabel.style.display = showFPS ? DisplayStyle.Flex : DisplayStyle.None;
            fpsLabel.style.display = showFPS ? DisplayStyle.Flex : DisplayStyle.None;
            msLabel.style.display = showFPS ? DisplayStyle.Flex : DisplayStyle.None;
            memoryLabel.style.display = showFPS ? DisplayStyle.Flex : DisplayStyle.None;
            fpsGraphContainer.style.display = showFPS ? DisplayStyle.Flex : DisplayStyle.None;
            Debug.Log($"Telemetria: {(showFPS ? "Ativada" : "Desativada")}");
        }
        
        // Atualiza FPS
        if (showFPS)
        {
            UpdateFPS();
            UpdateFPSGraph();
        }
    }

    // Update is called once per frame
    public void UpdateHealth(int health)
    {
        healthLabel.text = "Vida: " + health.ToString();
    }

    public void UpdateMana(int mana)
    {
        manaLabel.text = "Mana: " + mana.ToString();
    }
    
    private void UpdateFPS()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        float ms = deltaTime * 1000.0f;
        
        fpsLabel.text = $"FPS: {Mathf.Ceil(fps)}";
        msLabel.text = $"MS: {ms:F2}";
        
        // Memory Usage
        float memoryMB = (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f);
        if (memoryMB >= 1024)
        {
            float memoryGB = memoryMB / 1024f;
            memoryLabel.text = $"Memory: {memoryGB:F2} GB";
        }
        else
        {
            memoryLabel.text = $"Memory: {memoryMB:F2} MB";
        }
        
        // Muda cor baseado no FPS
        if (fps >= 60)
            fpsLabel.style.color = Color.green;
        else if (fps >= 30)
            fpsLabel.style.color = Color.yellow;
        else
            fpsLabel.style.color = Color.red;
    }
    
    private void CreateFPSGraph()
    {
        fpsGraphContainer = new VisualElement();
        fpsGraphContainer.name = "fpsGraph";
        fpsGraphContainer.style.position = Position.Absolute;
        fpsGraphContainer.style.top = 40;
        fpsGraphContainer.style.right = 20;
        fpsGraphContainer.style.width = 200;
        fpsGraphContainer.style.height = 80;
        fpsGraphContainer.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        fpsGraphContainer.style.borderTopWidth = 1;
        fpsGraphContainer.style.borderBottomWidth = 1;
        fpsGraphContainer.style.borderLeftWidth = 1;
        fpsGraphContainer.style.borderRightWidth = 1;
        fpsGraphContainer.style.borderTopColor = Color.white;
        fpsGraphContainer.style.borderBottomColor = Color.white;
        fpsGraphContainer.style.borderLeftColor = Color.white;
        fpsGraphContainer.style.borderRightColor = Color.white;
        fpsGraphContainer.style.display = DisplayStyle.None;
        
        hudDocument.rootVisualElement.Add(fpsGraphContainer);
    }
    
    private void UpdateFPSGraph()
    {
        graphUpdateTimer += Time.unscaledDeltaTime;
        
        if (graphUpdateTimer >= GRAPH_UPDATE_INTERVAL)
        {
            graphUpdateTimer = 0f;
            
            // Adiciona FPS atual ao histórico
            float currentFPS = 1.0f / deltaTime;
            fpsHistory.Add(currentFPS);
            
            // Mantém apenas últimas amostras
            if (fpsHistory.Count > FPS_HISTORY_SIZE)
            {
                fpsHistory.RemoveAt(0);
            }
            
            // Redesenha gráfico
            RedrawFPSGraph();
        }
    }
    
    private void RedrawFPSGraph()
    {
        fpsGraphContainer.Clear();
        
        if (fpsHistory.Count < 2) return;
        
        float maxFPS = 120f; // Escala do gráfico
        float barWidth = 200f / FPS_HISTORY_SIZE;
        
        for (int i = 0; i < fpsHistory.Count; i++)
        {
            float fps = Mathf.Clamp(fpsHistory[i], 0, maxFPS);
            float normalizedHeight = (fps / maxFPS) * 80f;
            
            var bar = new VisualElement();
            bar.style.position = Position.Absolute;
            bar.style.left = i * barWidth;
            bar.style.bottom = 0;
            bar.style.width = barWidth;
            bar.style.height = normalizedHeight;
            
            // Cor baseada no FPS
            if (fps >= 60)
                bar.style.backgroundColor = Color.green;
            else if (fps >= 30)
                bar.style.backgroundColor = Color.yellow;
            else
                bar.style.backgroundColor = Color.red;
            
            fpsGraphContainer.Add(bar);
        }
        
        // Linha de referência 60 FPS
        var line60 = new VisualElement();
        line60.style.position = Position.Absolute;
        line60.style.left = 0;
        line60.style.right = 0;
        line60.style.bottom = (60f / maxFPS) * 80f;
        line60.style.height = 1;
        line60.style.backgroundColor = new Color(0, 1, 0, 0.5f);
        fpsGraphContainer.Add(line60);
        
        // Linha de referência 30 FPS
        var line30 = new VisualElement();
        line30.style.position = Position.Absolute;
        line30.style.left = 0;
        line30.style.right = 0;
        line30.style.bottom = (30f / maxFPS) * 80f;
        line30.style.height = 1;
        line30.style.backgroundColor = new Color(1, 1, 0, 0.5f);
        fpsGraphContainer.Add(line30);
    }
}
