using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils
{
    public class MeshUtils
    {
        public static List<Vector3> GetMeshFaceNormals(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Yüz normalleri listesi
            List<Vector3> faceNormals = new List<Vector3>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Her üçgen için üç vertex pozisyonunu alın
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                // Dünya uzayına çevirmek isterseniz:
                // v0 = transform.TransformPoint(v0);
                // v1 = transform.TransformPoint(v1);
                // v2 = transform.TransformPoint(v2);

                // İki kenar vektörünü hesapla
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;

                // Yüz normalini çapraz çarpımla hesapla
                Vector3 faceNormal = Vector3.Cross(edge1, edge2).normalized;

                // Yüz normalini listeye ekle
                faceNormals.Add(faceNormal);
            }
            
            return faceNormals ;
        }
        
        public static List<Vector3> GetMeshFaceCenters(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Yüz normalleri listesi
            List<Vector3> faceCenters = new List<Vector3>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Her üçgen için üç vertex pozisyonunu alın
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];


                // Yüz normalini çapraz çarpımla hesapla
                Vector3 faceCenter = (v0 + v1 + v2) / 3.0f;

                // Yüz normalini listeye ekle
                faceCenters.Add(faceCenter);
            }
            
            return faceCenters;
        }
    }
}