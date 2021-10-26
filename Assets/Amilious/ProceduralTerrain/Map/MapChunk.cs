using System;
using System.Linq;
using System.Text;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Noise;
using Amilious.ProceduralTerrain.Textures;
using Amilious.Threading;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Map {
    public class MapChunk {

        private readonly MeshSettings _meshSettings;
        private BiomeSettings _biomeSettings;
        private readonly Transform _viewer;
        private readonly int _seed;
        private NoiseMap _heightMap;
        private BiomeMap _biomeMap;
        private Texture2D _previewTexture;
        private Color[] _preparedColors;
        private bool _heightMapReceived;
        private bool _hasSetCollider;

        private readonly GameObject _meshObject;
        private readonly Vector2 _sampleCenter;
        private readonly Vector2 _position;
        private Bounds _bounds;

        private readonly MeshRenderer _meshRenderer;
        private readonly MeshFilter _meshFilter;
        private readonly MeshCollider _meshCollider;
        private readonly LODInfo[] _detailLevels;
        private readonly LODMesh[] _lodMeshes;
        private int _previousLODIndex = -1;

        public event Action<MapChunk, bool> OnVisibilityChanged;
        
        public Vector2 Coordinate { get; private set; }
        public MapManager MapManager { get; private set; }
        
        
        public MapChunk(MapManager mapManager, Vector2 chunkCoord) {
            MapManager = mapManager;
            Coordinate = chunkCoord;
            _meshSettings = MapManager.MeshSettings;
            _biomeSettings = MapManager.BiomeSettings;
            _viewer = MapManager.Viewer;
            _seed = MapManager.HashedSeed;
            _detailLevels = _meshSettings.LevelsOfDetail.ToArray();

            //TODO: look at this closely.  Why is the bounds not sample center?
            _sampleCenter = Coordinate * _meshSettings.MeshWorldSize / _meshSettings.MeshScale;
            _position = Coordinate * _meshSettings.MeshWorldSize;
            _bounds = new Bounds(_position, Vector3.one * _meshSettings.MeshWorldSize);
            
            //generate game object
            _meshObject = new GameObject($"Chunk ({Coordinate.x},{Coordinate.y})") {
                transform = { position = new Vector3(_position.x, 0, _position.y),
                    parent = MapManager.transform }
            };
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            _meshRenderer.material = _meshSettings.Material;
            SetVisible(false);
            
            //setup lod meshes
            _lodMeshes = new LODMesh[_detailLevels.Length];
            for(var i = 0; i < _detailLevels.Length; i++) {
                _lodMeshes[i] = new LODMesh(_detailLevels[i].LOD);
                _lodMeshes[i].UpdateCallback += UpdateMapChunk;
                if(i == _meshSettings.ColliderLODIndex)
                    _lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
        }

        private void SetVisible(bool visible) => _meshObject.SetActive(visible);
        public bool IsVisible { get { return _meshObject.activeSelf; } }
        
        private Vector2 ViewerPosition => new Vector2 (_viewer.position.x, _viewer.position.z);

        public void UpdateCollisionMesh() {
            if(_hasSetCollider) return;
            var sqrDistanceFromViewer = _bounds.SqrDistance(ViewerPosition);
            if(sqrDistanceFromViewer < _detailLevels[_meshSettings.ColliderLODIndex].SqrVisibleDistanceThreshold)
                if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasRequestedMesh)
                    _lodMeshes[_meshSettings.ColliderLODIndex].RequestMesh(_heightMap, _meshSettings, MapManager.ApplyHeight);
            if(sqrDistanceFromViewer > MapManager.SqrColliderGenerationThreshold) return;
            if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasMesh) return;
            _meshCollider.sharedMesh = _lodMeshes[_meshSettings.ColliderLODIndex].Mesh;
            _hasSetCollider = true;
        }


        public void UpdateMapChunk() {
            if(!_heightMapReceived) return;
            var distanceFromViewer = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));
            var wasVisible = IsVisible;
            var visible = distanceFromViewer <= _meshSettings.MaxViewDistance;
            if(visible) UpdateLOD(distanceFromViewer);
            if(wasVisible == visible) return;
            SetVisible(visible);
            OnVisibilityChanged?.Invoke(this,visible);
        }

        private void UpdateLOD(float distanceFromViewer) {
            var lodIndex = 0;
            for(var i = 0; i < _detailLevels.Length - 1; i++) {
                if(distanceFromViewer > _detailLevels[i].VisibleDistanceThreshold)
                    lodIndex = i + 1;
                else break;
            }
            if(lodIndex == _previousLODIndex) return;
            var lodMesh = _lodMeshes[lodIndex];
            if(lodMesh.HasMesh) {
                _previousLODIndex = lodIndex;
                _meshFilter.mesh = lodMesh.Mesh;
            }else if(!lodMesh.HasRequestedMesh) {
                lodMesh.RequestMesh(_heightMap, _meshSettings, MapManager.ApplyHeight);
            }
        }

        public void Load() {
            var future = new Future<NoiseMap>();
            future.OnSuccess((heightMap) => {
                _heightMap = heightMap.value;
                if(MapManager.MapPaintingMode != MapPaintingMode.Material) {
                    _previewTexture = _biomeMap.GenerateTexture(_preparedColors,1);
                    _meshRenderer.material.mainTexture = _previewTexture;
                }
                _heightMapReceived = true;
                UpdateMapChunk();
            });
            future.OnError(heightMap => {
                Debug.LogError(heightMap.error);
            });
            future.Process(()=> {
                //generate the biome map
                _biomeMap = _biomeSettings.GenerateBiomeMap(_meshSettings.VertsPerLine, _seed, _sampleCenter);
                var heightMap = _biomeMap.GenerateHeightMap();
                _preparedColors = _biomeMap.GenerateTextureColors(heightMap, MapManager.MapPaintingMode, 1);
                return heightMap;
            });
        }


    }
}