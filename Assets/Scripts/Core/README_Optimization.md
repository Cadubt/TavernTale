# Sistema de Otimização de Performance - Tavern Tale

## 📋 Visão Geral

Sistema completo de otimização que reduz drasticamente o uso de recursos desabilitando/reduzindo qualidade de objetos fora da câmera.

## 🚀 Implementação Rápida

### 1. Configurar o OptimizationManager

1. Crie um GameObject vazio na cena chamado "OptimizationManager"
2. Adicione o componente `OptimizationManager` (Scripts/Core/OptimizationManager.cs)
3. Configure:
   - **Main Camera**: Arraste sua câmera principal
   - **Update Interval**: 0.2 (atualiza 5x por segundo)
   - **Culling Distance**: 30 (objetos além de 30 unidades serão desabilitados)
   - **Enable Distance Based Quality**: ✓ (reduz qualidade de objetos distantes)

### 2. Marcar Objetos como Otimizáveis

**Opção A - Individual:**
1. Selecione objetos do cenário (paredes, chão, decorações)
2. Adicione o componente `OptimizableObject`
3. Deixe "Auto Register" marcado

**Opção B - Em Massa (Recomendado):**
Execute este script no Editor:
```csharp
// Menu: Tools/Optimize All Scenario Objects
[MenuItem("Tools/Optimize All Scenario Objects")]
static void OptimizeScenarioObjects()
{
    GameObject[] allObjects = FindObjectsOfType<GameObject>();
    int count = 0;
    
    foreach (GameObject obj in allObjects)
    {
        // Adiciona em objetos com tag "scenario"
        if (obj.CompareTag("scenario") && obj.GetComponent<OptimizableObject>() == null)
        {
            obj.AddComponent<OptimizableObject>();
            count++;
        }
    }
    
    Debug.Log($"OptimizableObject adicionado em {count} objetos!");
}
```

### 3. Combinar Meshes Estáticos (Opcional)

Para reduzir ainda mais draw calls:

1. Crie GameObject vazio chamado "StaticBatcher_Floor" (ou Wall, etc)
2. Adicione componente `StaticBatcher`
3. Configure:
   - **Tags To Include**: "scenario"
   - **Combine Radius**: 10
4. Agrupe objetos similares (mesmo material) como filhos
5. Clique com botão direito no componente → "Combine Meshes"

## 🎯 Recursos do Sistema

### OptimizationManager
- ✅ Culling por distância (objetos longes desabilitados)
- ✅ Frustum culling (objetos fora da câmera desabilitados)
- ✅ Sistema de qualidade por distância (LOD)
- ✅ Estatísticas em tempo real no Game View
- ✅ Atualização otimizada (não roda todo frame)

### OptimizableObject
- ✅ Controla Renderers (visibilidade)
- ✅ Controla Colliders (opcional)
- ✅ Controla Scripts customizados
- ✅ 3 níveis de qualidade (High/Medium/Low)
- ✅ Ajuste automático de sombras por qualidade
- ✅ Gizmos no Editor para debug

### StaticBatcher
- ✅ Combina múltiplos meshes em um único
- ✅ Reduz drasticamente draw calls
- ✅ Filtragem por tag e distância
- ✅ Mantém objetos originais (desabilitados)

## 📊 Ganhos Esperados de Performance

### Antes da Otimização:
- Draw Calls: 500-1000+
- FPS: 20-30
- Objetos renderizados: Todos

### Depois da Otimização:
- Draw Calls: 50-150
- FPS: 60+
- Objetos renderizados: Apenas visíveis

## ⚙️ Configurações Recomendadas

### Para Dungeons Pequenas (< 50 objetos):
- Culling Distance: 20
- Update Interval: 0.3
- Quality Distance: Desabilitado

### Para Dungeons Médias (50-200 objetos):
- Culling Distance: 25
- Update Interval: 0.2
- Low Quality Distance: 15
- Medium Quality Distance: 8

### Para Dungeons Grandes (200+ objetos):
- Culling Distance: 30
- Update Interval: 0.15
- Low Quality Distance: 20
- Medium Quality Distance: 10

## 🔧 Outras Otimizações Recomendadas

### No Unity Editor:
1. **Edit → Project Settings → Quality**
   - Shadow Distance: 30
   - Shadow Cascades: Two Cascades
   - Pixel Light Count: 2

2. **Window → Rendering → Lighting**
   - Auto Generate: OFF (gerar manualmente)
   - Baked GI: ON
   - Realtime GI: OFF

3. **Edit → Project Settings → Physics**
   - Auto Sync Transforms: OFF

### No Build:
1. **File → Build Settings → Player Settings**
   - API Compatibility: .NET Standard 2.1
   - Managed Stripping Level: Medium
   - IL2CPP Code Generation: Faster Runtime

## 🐛 Troubleshooting

### Objetos importantes desaparecendo:
- Aumente `Culling Distance` no OptimizationManager
- Remova OptimizableObject de objetos críticos (Player, NPCs)

### FPS ainda baixo:
- Use Static Batching em objetos com mesmo material
- Reduza `Update Interval` (menos atualizações)
- Considere usar Occlusion Culling do Unity

### Objetos piscando:
- Aumente `Update Interval` (0.3 ou 0.5)
- Desative `Enable Distance Based Quality`

## 📝 Dicas Importantes

1. **NÃO adicione OptimizableObject em:**
   - Player
   - Monstros/NPCs
   - Objetos com física ativa
   - UI Elements

2. **ADICIONE OptimizableObject em:**
   - ✅ Paredes
   - ✅ Chão
   - ✅ Decorações
   - ✅ Objetos estáticos
   - ✅ Props

3. **Use Static Batching para:**
   - Objetos com mesmo material
   - Objetos que nunca se movem
   - Cenário repetitivo

## 🎮 Monitoramento

Pressione F7 para ver estatísticas do OptimizationManager:
- Total Objects: Objetos registrados
- Active Objects: Objetos sendo renderizados
- Culled Objects: Objetos desabilitados

## 📚 Referências

- Documentação Unity: Occlusion Culling
- Documentação Unity: Draw Call Batching
- Unity Best Practices: Performance Optimization
