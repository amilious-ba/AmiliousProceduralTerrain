using System.Linq;
using UnityEngine;
using System.Threading;
using Amilious.Threading;
using Sirenix.OdinInspector;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Textures;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This class will represent a chunk
    /// </summary>
    [HideMonoScript]
    public class Chunk {

        private GameObject _gameObject;
        private Transform _transform;
        private readonly MapManager _manager;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private readonly MeshSettings _meshSettings;
        private readonly ChunkMesh[] _lodMeshes;
        private readonly LODInfo[] _detailLevels;
        private readonly Transform _viewer;
        private readonly BiomeMap _biomeMap;
        private readonly Color[] _preparedColors;
        private readonly ChunkPool _chunkPool;
        private readonly ReusableFuture _loader;
        private readonly ReusableFuture<bool, bool> _saver;
        
        private int _previousLODIndex = -1;
        private bool _heightMapReceived;
        private Vector2 _sampleCenter;
        private Vector2 _position;
        private Bounds _bounds;
        private bool _hasSetCollider;
        private Texture2D _previewTexture;
        private bool _startedToRelease;
        private bool _updated;
        private bool _isActive = true;
        private Vector3 _transformPosition = Vector3.zero;
        private string _name;
        private bool _appliedMeshMaterial;

        /// <summary>
        /// This event is triggered when a chunks visibility changes.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static Delegates.OnChunkVisibilityChangedDelegate onVisibilityChanged;

        /// <summary>
        /// This event is triggered when a chunks lod changes.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static Delegates.OnLodChangedDelegate onLodChanged;

        /// <summary>
        /// This event is triggered when a chunk is loaded.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static Delegates.OnChunkLoadedDelegate onChunkLoaded;

        /// <summary>
        /// This event is triggered when a chunk is saved.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static Delegates.OnChunkSavedDelegate onChunkSaved;
        
        /// <summary>
        /// This property contains true if the chunk is being used,
        /// otherwise contains false.
        /// </summary>
        public bool IsInUse { get; private set; }

        /// <summary>
        /// This property is used to check if the chunk's gameObject is active.  It
        /// can also be used to protected set if the chunk's gameObject is active.
        /// </summary>
        public bool Active {
            get => _isActive;
            protected set {
                Dispatcher.InvokeAsync(()=> {
                    _gameObject.SetActive(value);
                    _isActive = value;
                });
            }
        }

        /// <summary>
        /// This property is used to get the chunk's gameObject's name.  It can
        /// also be used to protected set the gameObject's name.
        /// </summary>
        public string Name {
            get => _name;
            protected set {
                Dispatcher.InvokeAsync(()=> {
                    _gameObject.name = value;
                    _name = value;
                });
            }
        }

        /// <summary>
        /// This property is used to get the chunk's transform position.  It can also
        /// be used to protected set the transforms position.
        /// </summary>
        public Vector3 TransformPosition {
            get => _transformPosition;
            set {
                Dispatcher.InvokeAsync(() => {
                    _gameObject.transform.position = value;
                    _transformPosition = value;
                });
            }
        }
        
        /// <summary>
        /// This property contains the chunk id.
        /// </summary>
        public Vector2Int ChunkId { get; private set; }
        
        /// <summary>
        /// This property contains true it the chunks has been processed to be released.Updated
        /// </summary>
        public bool HasProcessedRelease { get; private set; }

        /// <summary>
        /// This property is used to get the player's position as a <see cref="Vector2"/>.
        /// </summary>
        private Vector2 ViewerPosition => new Vector2 (_viewer.position.x, _viewer.position.z);
        
        public Chunk(MapManager manager, ChunkPool chunkPool) {
            //make sure the chunk gameObject and components get
            //created on the main thead
            Dispatcher.Invoke(() => {
                _gameObject = new GameObject() {
                    transform = { parent = manager.transform }
                };
                _meshFilter = _gameObject.AddComponent<MeshFilter>();
                _meshCollider = _gameObject.AddComponent<MeshCollider>();
                _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
                _transformPosition = _gameObject.transform.position;
                _transform = _gameObject.transform;
            });
            
            Name = $"Chunk (pooled)";
            _meshSettings = manager.MeshSettings;
            _viewer = manager.Viewer;
            _biomeMap = new BiomeMap(manager.HashedSeed,manager.MeshSettings.VertsPerLine, manager.BiomeSettings);
            _preparedColors = new Color[_biomeMap.GetBorderCulledValuesCount(1)];
            //create meshes
            _detailLevels = _meshSettings.LevelsOfDetail.ToArray();
            _lodMeshes = new ChunkMesh[_detailLevels.Length];
            _chunkPool = chunkPool;
            for(var i = 0; i < _detailLevels.Length; i++) {
                _lodMeshes[i] = new ChunkMesh(_meshSettings, 
                    _detailLevels[i].SkipStep, _detailLevels[i].LevelsOfDetail);
                _lodMeshes[i].UpdateCallback += UpdateChunk;
                if(i == _meshSettings.ColliderLODIndex)
                    _lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
            //get the manager
            _manager = manager;
            //setup loader
            _loader = new ReusableFuture();
            _loader.OnError(Debug.LogError);
            _loader.OnProcess(ProcessLoad).OnSuccess(OnLoadComplete);
            //setup saver
            _saver = new ReusableFuture<bool, bool>();
            _saver.OnError(Debug.LogError);
            _saver.OnProcess(ProcessSave).OnSuccess(SaveComplete);
            //if not in use disable gameObject
            if(!IsInUse) Active = false;
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
        /// <param name="chunkId">The chunks id.</param>
        public void Setup(Vector2Int chunkId) {
            ChunkId = chunkId;
            var floatCoord = new Vector2(ChunkId.x, ChunkId.y);
            _sampleCenter = floatCoord * _meshSettings.MeshWorldSize / _meshSettings.MeshScale;
            _position = floatCoord * _meshSettings.MeshWorldSize;
            _bounds = new Bounds(_position, Vector3.one * _meshSettings.MeshWorldSize);
            Name = $"Chunk ({ChunkId.x},{ChunkId.y})";
            TransformPosition = new Vector3(_position.x, 0, _position.y);
            HasProcessedRelease = false;
            _loader.Process();
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
            //Active = true;
        }

        /// <summary>
        /// This method is called after the save process has been
        /// successfully completed.
        /// </summary>
        /// <param name="releaseFromPool">True if the chunk should be
        /// released to the pool, otherwise false.</param>
        private void SaveComplete(bool releaseFromPool) {
            onChunkSaved?.Invoke(ChunkId);
            if(releaseFromPool) ReleaseToPool();
        }

        /// <summary>
        /// This method is used to process a save operation.
        /// </summary>
        /// <param name="releaseFromPool">If this value is true the chunk
        /// will be released to the pool after the save is complete.</param>
        /// <param name="token">This is a cancellation token that can be used
        /// to check if the save has been cancelled.</param>
        /// <returns>True if the chunk should be released to the pool, otherwise
        /// false.</returns>
        private bool ProcessSave(bool releaseFromPool, CancellationToken token) {
            if(!_manager.SaveEnabled && releaseFromPool) return true;
            if(!_manager.SaveEnabled || !_heightMapReceived) return releaseFromPool;
            var saveData = _manager.MapSaver.NewChunkSaveData(ChunkId);
            _biomeMap.Save(saveData);
            _manager.MapSaver.SaveData(ChunkId, saveData);
            return releaseFromPool;
        }

        /// <summary>
        /// This method is used to dispose of a chunk and add it back
        /// to the chunk pool.
        /// </summary>
        public bool ReleaseToPool() {
            _loader.Cancel();
            _manager.OnStartUpdate -= StartUpdateCycle;
            _manager.OnUpdateVisible -= UpdateChunk;
            _manager.OnEndUpdate -= ValidateNonUpdatedChunk;
            _manager.OnUpdateCollisionMesh -= UpdateCollisionMesh;
            Active = false;
            Name = $"Chunk (pooled)";
            //reset the mesh values
            foreach(var mesh in _lodMeshes) mesh.Reset();
            _previousLODIndex = -1;
            _hasSetCollider = false;
            _heightMapReceived = false;
            //return to pool
            IsInUse = false;
            _startedToRelease = false;
            HasProcessedRelease = true;
            return _chunkPool.ReturnToPool(this);
        }

        /// <summary>
        /// This method is called when the <see cref="MapManager"/> starts the update cycle.
        /// </summary>
        private void StartUpdateCycle() { _updated = false; }

        /// <summary>
        /// This is a helper method that can be used to call the update chunk from within this class.
        /// </summary>
        private void UpdateChunk() => UpdateChunk(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue);
        
        /// <summary>
        /// This method is called when the chunk should be updated.  This
        /// is called if the chunk is visible and the player moved enough
        /// that the chunks need to update.
        /// </summary>
        public void UpdateChunk(int xMin, int xMax, int yMin, int yMax) {
            if(!_heightMapReceived|| !IsInUse) return;
            if(ChunkId.x < xMin || ChunkId.x > xMax || ChunkId.y < yMin || ChunkId.y > yMax) {
                //out of range so make sure it is disabled
                if(!Active) return;
                Active = false;
                onVisibilityChanged?.Invoke(ChunkId,false);
                return;
            }
            _updated = true;
            var distanceFromViewerSq = _bounds.SqrDistance(ViewerPosition);
            var wasVisible =  Active;
            var visible = distanceFromViewerSq <= _meshSettings.MaxViewDistanceSq;
            if(visible) UpdateLOD(distanceFromViewerSq);
            if(wasVisible == visible) return;
            Active = visible;
            onVisibilityChanged?.Invoke(ChunkId,visible);
        }

        /// <summary>
        /// This method is called when the <see cref="MapManager"/> is ending the update cycle.
        /// </summary>
        public void ValidateNonUpdatedChunk() {
            if(!IsInUse||_updated||_startedToRelease) return;
            if(_bounds.SqrDistance(ViewerPosition) < _meshSettings.ChunkUnloadDistanceSq) return;
            _startedToRelease = true;
            _saver.Process(true);
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
                var oldLod = _previousLODIndex;
                _previousLODIndex = lodIndex;
                lodMesh.AssignTo(_meshFilter);
                onLodChanged?.Invoke(ChunkId,oldLod,lodIndex);
            }else if(!lodMesh.HasRequestedMesh) {
                lodMesh.RequestMesh(_biomeMap.HeightMap, _manager.ApplyHeight);
            }
        }

        /// <summary>
        /// This method is called after the load process has been completed
        /// successfully.
        /// </summary>
        private void OnLoadComplete() {
            if(!_appliedMeshMaterial) {
                _meshRenderer.material = _meshSettings.Material;
                _appliedMeshMaterial = true;
            }
            if(_meshSettings.PaintingMode != TerrainPaintingMode.Material) {
                _previewTexture = _biomeMap.GenerateTexture(_preparedColors,1);
                _meshRenderer.material.mainTexture = _previewTexture;
            }
            _heightMapReceived = true;
            UpdateChunk();
            onChunkLoaded?.Invoke(ChunkId);
        }

        /// <summary>
        /// This method is executed from the loader.
        /// </summary>
        /// <param name="token">This is a cancellation token that can be used to
        /// check if the load operation has been canceled.</param>
        /// <returns>True if everything was processed correctly, otherwise an error
        /// will be thrown if false.</returns>
        private bool ProcessLoad(CancellationToken token) {
            //try to load or generate biome data
            if(_manager.SaveEnabled && _manager.MapSaver.LoadData(ChunkId, out var saveData)) {
                _biomeMap.Load(saveData);
            }else _biomeMap.Generate(_sampleCenter, token);
            //generate texture
            _biomeMap.GenerateTextureColors(_preparedColors, _meshSettings.PaintingMode, 1);
            return true;
        }

        /// <summary>
        /// This method is used to handle the collision mesh.
        /// </summary>
        public void UpdateCollisionMesh() {
            if(!IsInUse || !Active || _hasSetCollider) return;
            var sqrDistanceFromViewer = _bounds.SqrDistance(ViewerPosition);
            if(sqrDistanceFromViewer < _detailLevels[_meshSettings.ColliderLODIndex].SqrVisibleDistanceThreshold)
                if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasRequestedMesh)
                    _lodMeshes[_meshSettings.ColliderLODIndex].RequestMesh(_biomeMap.HeightMap, _manager.ApplyHeight);
            if(sqrDistanceFromViewer > _manager.SqrColliderGenerationThreshold) return;
            if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasMesh) return;
            _lodMeshes[_meshSettings.ColliderLODIndex].AssignTo(_meshCollider);
            _hasSetCollider = true;
        }
        
    }
}