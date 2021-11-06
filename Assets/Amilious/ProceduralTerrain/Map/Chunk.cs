using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using Amilious.Threading;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Textures;
using Sirenix.OdinInspector;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This class will represent a chunk
    /// </summary>
    [HideMonoScript]
    public class Chunk : MonoBehaviour {
        
        private MapManager _manager;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private BiomeSettings _biomeSettings;
        private MeshSettings _meshSettings;
        private ChunkMesh[] _lodMeshes;
        private LODInfo[] _detailLevels;
        private int _previousLODIndex = -1;
        private bool _heightMapReceived;
        private Vector2 _sampleCenter;
        private Vector2 _position;
        private Bounds _bounds;
        private bool _hasSetCollider;
        private Transform _viewer;
        private BiomeMap _biomeMap;
        private Texture2D _previewTexture;
        private Color[] _preparedColors;
        private int _seed;
        private ChunkPool _chunkPool;
        private ReusableFuture loader;

        public event Action<Chunk, bool> OnVisibilityChanged;
        
        public bool IsInUse { get; private set; } = false;
        public Vector2Int Coordinate { get; private set; }
        
        public bool HasProcessedRelease { get; private set; }

        private Vector2 ViewerPosition => new Vector2 (_viewer.position.x, _viewer.position.z);

        private void Awake() {
            //get the manager
            _manager = GetComponentInParent<MapManager>();
            if(_manager==null) Debug.LogWarning("The chunk is unable to find it's parent.");
            //create the required components
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            //setup loader
            loader = new ReusableFuture();
            loader.OnError(error => Debug.Log("Error: "+error));
            loader.OnProcess(ProcessLoadData).OnSuccess(OnDataLoaded);
            //if not in use disable gameObject
            if(!IsInUse) gameObject.SetActive(false);
        }

        private void OnDestroy() {
            _manager.OnStartUpdate -= StartUpdateCycle;
            _manager.OnUpdateVisible -= UpdateChunk;
            _manager.OnEndUpdate -= ValidateNonUpdatedChunk;
            _manager.OnUpdateCollisionMesh -= UpdateCollisionMesh;
            foreach(var mesh in _lodMeshes) {
                mesh.UpdateCallback -= UpdateChunk;
                mesh.UpdateCallback -= UpdateCollisionMesh;
            }
        }

        /// <summary>
        /// This method is used to update the chunk to be used as a
        /// specified chunk.
        /// </summary>
        /// <param name="coordinate">The chunks coordinate.</param>
        public void Setup(Vector2Int coordinate) {
            Coordinate = coordinate;
            var floatCoord = new Vector2(Coordinate.x, Coordinate.y);
            _sampleCenter = floatCoord * _meshSettings.MeshWorldSize / _meshSettings.MeshScale;
            _position = floatCoord * _meshSettings.MeshWorldSize;
            _bounds = new Bounds(_position, Vector3.one * _meshSettings.MeshWorldSize);
            gameObject.name = $"Chunk ({Coordinate.x},{Coordinate.y})";
            transform.position = new Vector3(_position.x, 0, _position.y);
            HasProcessedRelease = false;
            //Load();
            loader.Process();
        }

        /// <summary>
        /// This method is used to mark the chunk as in use so
        /// that the chunk pool will not issue it again.
        /// </summary>
        /// <param name="setActive"></param>
        public void PullFromPool(bool setActive = false) {
            IsInUse = true;
            _manager.OnStartUpdate += StartUpdateCycle;
            _manager.OnUpdateVisible += UpdateChunk;
            _manager.OnEndUpdate += ValidateNonUpdatedChunk;
            _manager.OnUpdateCollisionMesh += UpdateCollisionMesh;
            if(setActive) gameObject.SetActive(true);
        }

        /// <summary>
        /// This method is used to dispose of a chunk and add it back
        /// to the chunk pool.
        /// </summary>
        public bool ReleaseToPool() {
            if(_manager.SaveEnabled && _heightMapReceived) {
                //var saveData = new SaveData();
                //_biomeMap.Save(saveData);
                //TODO: save the chunk data
            }
            loader.Cancel();
            _manager.OnStartUpdate -= StartUpdateCycle;
            _manager.OnUpdateVisible -= UpdateChunk;
            _manager.OnEndUpdate -= ValidateNonUpdatedChunk;
            _manager.OnUpdateCollisionMesh -= UpdateCollisionMesh;
            gameObject.SetActive(false);
            //reset the mesh values
            foreach(var mesh in _lodMeshes) mesh.Reset();
            _previousLODIndex = -1;
            _hasSetCollider = false;
            _heightMapReceived = false;
            gameObject.name = $"Chunk (pooled)";
            //return to pool
            IsInUse = false;
            HasProcessedRelease = true;
            return _chunkPool.ReturnToPool(this);
        }

        private bool _updated;

        private void StartUpdateCycle() { _updated = false; }

        private void UpdateChunk() => UpdateChunk(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue);
        
        /// <summary>
        /// This method is called when the chunk should be updated.  This
        /// is called if the chunk is visible and the player moved enough
        /// that the chunks need to update.
        /// </summary>
        public void UpdateChunk(int xMin, int xMax, int yMin, int yMax) {
            if(!_heightMapReceived|| !IsInUse) return;
            if(Coordinate.x < xMin || Coordinate.x > xMax || Coordinate.y < yMin || Coordinate.y > yMax) {
                //out of range so make sure it is disabled
                if(!gameObject.activeSelf) return;
                gameObject.SetActive(false);
                OnVisibilityChanged?.Invoke(this,false);
                return;
            }
            _updated = true;
            var distanceFromViewerSq = _bounds.SqrDistance(ViewerPosition);
            var wasVisible =  gameObject.activeSelf;
            var visible = distanceFromViewerSq <= _meshSettings.MaxViewDistanceSq;
            if(visible) UpdateLOD(distanceFromViewerSq);
            if(wasVisible == visible) return;
            gameObject.SetActive(visible);
            OnVisibilityChanged?.Invoke(this,visible);
        }

        public void ValidateNonUpdatedChunk() {
            if(!IsInUse||_updated) return;
            if(_bounds.SqrDistance(ViewerPosition) < _meshSettings.ChunkUnloadDistanceSq) return;
            //The chunk can be unloaded
            ReleaseToPool();
        }
        
        /// <summary>
        /// This method is used to update the chunks level of detail.
        /// </summary>
        /// <param name="distanceFromViewerSq">The chunks distance from the
        /// viewer.</param>
        private void UpdateLOD(float distanceFromViewerSq) {
            var lodIndex = 0;
            for(var i = 0; i < _detailLevels.Length - 1; i++) {
                if(distanceFromViewerSq > _detailLevels[i].SqrVisibleDistanceThreshold)
                    lodIndex = i + 1;
                else break;
            }
            if(lodIndex == _previousLODIndex) return;
            var lodMesh = _lodMeshes[lodIndex];
            if(lodMesh.HasMesh) {
                _previousLODIndex = lodIndex;
                lodMesh.AssignTo(_meshFilter);
            }else if(!lodMesh.HasRequestedMesh) {
                lodMesh.RequestMesh(_biomeMap.HeightMap, _meshSettings, _manager.ApplyHeight);
            }
        }

        private void OnDataLoaded() {
            if(_manager.MapPaintingMode != MapPaintingMode.Material) {
                _previewTexture = _biomeMap.GenerateTexture(_preparedColors,1);
                _meshRenderer.material.mainTexture = _previewTexture;
            }
            _heightMapReceived = true;
            UpdateChunk();
        }

        private bool ProcessLoadData(CancellationToken token) {
            if(_manager.SaveEnabled && _manager.MapSaver.LoadData(Coordinate, out var saveData)) {
                _biomeMap.Load(saveData);
            }else _biomeMap.Generate(_sampleCenter, token);
            _biomeMap.GenerateTextureColors(_preparedColors, _manager.MapPaintingMode, 1);
            return true;
        }

        /// <summary>
        /// This method is used to handle the collision mesh.
        /// </summary>
        public void UpdateCollisionMesh() {
            if(!IsInUse || !gameObject.activeSelf || _hasSetCollider) return;
            var sqrDistanceFromViewer = _bounds.SqrDistance(ViewerPosition);
            if(sqrDistanceFromViewer < _detailLevels[_meshSettings.ColliderLODIndex].SqrVisibleDistanceThreshold)
                if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasRequestedMesh)
                    _lodMeshes[_meshSettings.ColliderLODIndex].RequestMesh(_biomeMap.HeightMap, _meshSettings, _manager.ApplyHeight);
            if(sqrDistanceFromViewer > _manager.SqrColliderGenerationThreshold) return;
            if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasMesh) return;
            _lodMeshes[_meshSettings.ColliderLODIndex].AssignTo(_meshCollider);
            _hasSetCollider = true;
        }

        /// <summary>
        /// This method is used to create and setup a new chunk.
        /// </summary>
        /// <param name="manager">The map manager that will be used
        /// for the chunk.</param>
        /// <returns>The newly generated chunk.</returns>
        public static Chunk CreateNew(MapManager manager, ChunkPool chunkPool) {
            var gameObject = new GameObject($"Chunk (pooled)") {
                transform = { parent = manager.transform }
            };
            var chunk = gameObject.AddComponent<Chunk>();
            chunk._meshSettings = manager.MeshSettings;
            chunk._biomeSettings = manager.BiomeSettings;
            chunk._viewer = manager.Viewer;
            chunk._seed = manager.HashedSeed;
            chunk._biomeMap = new BiomeMap(manager.HashedSeed,manager.MeshSettings.VertsPerLine, manager.BiomeSettings);
            chunk._preparedColors = new Color[chunk._biomeMap.GetBorderCulledValuesCount(1)];
            //create meshes
            chunk._detailLevels = chunk._meshSettings.LevelsOfDetail.ToArray();
            chunk._lodMeshes = new ChunkMesh[chunk._detailLevels.Length];
            chunk._chunkPool = chunkPool;
            for(var i = 0; i < chunk._detailLevels.Length; i++) {
                chunk._lodMeshes[i] = new ChunkMesh(chunk._meshSettings.VertsPerLine,
                    chunk._detailLevels[i].SkipStep, chunk._meshSettings.UseFlatShading ,
                    chunk._detailLevels[i].LOD);
                chunk._lodMeshes[i].UpdateCallback += chunk.UpdateChunk;
                if(i == chunk._meshSettings.ColliderLODIndex)
                    chunk._lodMeshes[i].UpdateCallback += chunk.UpdateCollisionMesh;
            }
            return chunk;
        }
    }
}