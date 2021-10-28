using System.Collections.Generic;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Mesh {
    
    public class AmiliousMesh {

        private readonly UnityEngine.Mesh _mesh;
        
        public readonly Vector3[] vertices;
        public readonly int[] triangles;
        public readonly List<Vector2[]> uvs;

        public readonly int vertexCount;
        public readonly int triangleCount;
        public readonly int uvChannels;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertexCount"></param>
        /// <param name="triangleCount"></param>
        /// <param name="uvChannels">This should be a value between 0 and 8.</param>
        public AmiliousMesh(int vertexCount, int triangleCount, int uvChannels) {

            if(uvChannels < 0) uvChannels = 0;
            if(uvChannels > 8) uvChannels = 8;
            
            _mesh = new UnityEngine.Mesh();
            this.vertexCount = vertexCount;
            this.triangleCount = triangleCount;
            this.uvChannels = uvChannels;

            vertices = new Vector3[vertexCount];
            triangles = new int[triangleCount];
            if(uvChannels <= 0) return;
            uvs = new List<Vector2[]>(uvChannels);
            for(var i = 0; i < uvChannels; i++) uvs[i] = new Vector2[vertexCount];

        }
        

        public void AssignTo(MeshFilter meshFilter) {
            meshFilter.sharedMesh = _mesh;
        }

        public void AssignTo(MeshCollider meshCollider) {
            meshCollider.sharedMesh = _mesh;
        }


        public void Upload(bool recalculateBounds) {
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            if(recalculateBounds) _mesh.RecalculateBounds();
        }

        public void UpdateUvs() {
            if(uvChannels < 1) return;
            if(uvChannels > 0) _mesh.uv = uvs[0];
            if(uvChannels > 1) _mesh.uv = uvs[1];
            if(uvChannels > 2) _mesh.uv = uvs[2];
            if(uvChannels > 3) _mesh.uv = uvs[3];
            if(uvChannels > 4) _mesh.uv = uvs[4];
            if(uvChannels > 5) _mesh.uv = uvs[5];
            if(uvChannels > 6) _mesh.uv = uvs[6];
            if(uvChannels > 7) _mesh.uv = uvs[7];
        }


    }
    
    
}