using UnityEngine;

namespace Core
{
    /// <summary>
    /// Adicione este script em objetos estáticos (cenário) para combiná-los e reduzir draw calls
    /// Combina meshes de objetos próximos em um único mesh para melhor performance
    /// </summary>
    public class StaticBatcher : MonoBehaviour
    {
        [Header("Configurações")]
        [SerializeField] private bool combineOnStart = true;
        [SerializeField] private bool createCollider = false;
        [SerializeField] private Material sharedMaterial = null;

        [Header("Filtros")]
        [SerializeField] private string[] tagsToInclude = new string[] { "scenario" };
        [SerializeField] private float combineRadius = 10f;

        private void Start()
        {
            if (combineOnStart)
            {
                CombineMeshes();
            }
        }

        [ContextMenu("Combine Meshes")]
        public void CombineMeshes()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0)
            {
                Debug.LogWarning("Nenhum MeshFilter encontrado para combinar");
                return;
            }

            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int validMeshes = 0;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh == null) continue;

                // Verifica distância
                float distance = Vector3.Distance(transform.position, meshFilters[i].transform.position);
                if (distance > combineRadius) continue;

                // Verifica tag
                bool hasValidTag = false;
                foreach (string tag in tagsToInclude)
                {
                    if (meshFilters[i].CompareTag(tag))
                    {
                        hasValidTag = true;
                        break;
                    }
                }

                if (!hasValidTag && tagsToInclude.Length > 0) continue;

                combine[validMeshes].mesh = meshFilters[i].sharedMesh;
                combine[validMeshes].transform = meshFilters[i].transform.localToWorldMatrix;
                
                // Desabilita o mesh original
                meshFilters[i].gameObject.SetActive(false);
                
                validMeshes++;
            }

            if (validMeshes == 0)
            {
                Debug.LogWarning("Nenhum mesh válido para combinar");
                return;
            }

            // Cria mesh combinado
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "CombinedMesh_" + gameObject.name;
            
            System.Array.Resize(ref combine, validMeshes);
            combinedMesh.CombineMeshes(combine, true, true);
            combinedMesh.RecalculateBounds();
            combinedMesh.RecalculateNormals();

            // Cria GameObject com mesh combinado
            GameObject combinedObject = new GameObject("CombinedMesh");
            combinedObject.transform.SetParent(transform);
            combinedObject.transform.localPosition = Vector3.zero;
            combinedObject.transform.localRotation = Quaternion.identity;
            combinedObject.isStatic = true;

            MeshFilter mf = combinedObject.AddComponent<MeshFilter>();
            mf.sharedMesh = combinedMesh;

            MeshRenderer mr = combinedObject.AddComponent<MeshRenderer>();
            if (sharedMaterial != null)
                mr.sharedMaterial = sharedMaterial;

            if (createCollider)
            {
                MeshCollider mc = combinedObject.AddComponent<MeshCollider>();
                mc.sharedMesh = combinedMesh;
            }

            Debug.Log($"Meshes combinados com sucesso! {validMeshes} meshes unidos. Draw calls reduzidos.");
        }
    }
}
