using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils
{
    public class MeshCombiner : MonoBehaviour
    {
        public MeshFilter CombinedMeshRenderer;
        
        public void CombineMeshes(MeshFilter[] meshFilters, bool deactivateChildren)
        {
            // 2) CombineInstance listesi
            List<CombineInstance> combineInstances = new List<CombineInstance>();

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter mf = meshFilters[i];

                // Kendi MeshFilter'ımızı (bu scriptin olduğu obje) atlama:
                if (mf == GetComponent<MeshFilter>())
                    continue;

                // Eğer child mesh yoksa veya paylaşılan mesh eksikse atla
                if (mf.sharedMesh == null)
                    continue;

                // Bir CombineInstance oluştur
                CombineInstance ci = new CombineInstance();
                ci.mesh = mf.sharedMesh;
                // Bu child’ın localToWorldMatrix ile değil,
                // parent’a göre local matrix gerekir.
                // Fakat CombineMeshes local space'te birleştireceği için
                // "child.worldMatrix * parent.worldToLocalMatrix" mantığı kullanacağız.
                ci.transform = mf.transform.localToWorldMatrix;
                // => Bu, combine işleminde child’ın global konumunu hesaba katması için.

                // Child eğer farklı materyaller kullanıyorsa submesh seçmen gerekebilir
                // ama basit haliyle submesh=0 denebilir (tek materyal varsayıyoruz).
                ci.subMeshIndex = 0;

                combineInstances.Add(ci);
            }
            
            // 3) Sonuç mesh
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "CombinedMesh";

            // Basit kullanım => combineInstances’daki meshler tek materyal olduğu varsayılır
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            // 4) Sonucu kendi MeshFilter’ımıza ata
            MeshFilter parentMF = CombinedMeshRenderer;
            parentMF.sharedMesh = combinedMesh;

            // 5) Eğer “deactivateChildren” true ise, alt objeleri kapat
            if (deactivateChildren)
            {
                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf == parentMF) 
                        continue;
                    mf.gameObject.SetActive(false);
                }
            }

        }
    }
}