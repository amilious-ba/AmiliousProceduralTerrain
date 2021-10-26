using System;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using Amilious.Threading;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Mesh {
    
    public class LODMesh {
        
        public event Action UpdateCallback;
        private readonly int _lod;
        
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh { get; private set; }
        
        public UnityEngine.Mesh Mesh { get; private set; }
        
        public LODMesh(int lod) {
            _lod = lod;
        }

        public void RequestMesh(NoiseMap heightMap, MeshSettings meshSettings, bool applyHeight = true) {
            HasRequestedMesh = true;
            var future = new Future<MeshData>();
            future.OnSuccess(meshData=> {
                Mesh = meshData.value.CreateMesh();
                HasMesh = true;
                UpdateCallback?.Invoke();
            });
            future.Process(()=> MeshChunkGenerator.Generate(heightMap, meshSettings, _lod, applyHeight));
        }
        
    }
    
    
}