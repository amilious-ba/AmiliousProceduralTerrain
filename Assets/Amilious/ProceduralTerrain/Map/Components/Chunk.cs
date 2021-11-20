using System.Linq;
using UnityEngine;
using Amilious.Saving;
using System.Threading;
using Amilious.Threading;
using Amilious.Core.Structs;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Saving;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Textures;
using Amilious.ProceduralTerrain.Map.Enums;
using Amilious.ProceduralTerrain.Mesh.Enums;

namespace Amilious.ProceduralTerrain.Map.Components {
    
    /// <summary>
    /// This class will represent a chunk
    /// </summary>
    public class Chunk : IMapComponent<Chunk> {

        private const string CHUNK_POOLED = "Chunk (pooled)"; 

        #region Instance Variables
        
        private readonly MapManager _mapManager;
        private GameObject _gameObject;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private readonly MeshSettings _meshSettings;
        private readonly ChunkMesh[] _lodMeshes;
        private readonly LODInfo[] _detailLevels;
        private readonly BiomeMap _biomeMap;
        private readonly Color[] _preparedColors;
        private Texture2D _previewTexture;
        private readonly MapSaver _mapSaver;
        private readonly MapPool<Chunk> _mapPool;
        private readonly ReusableFuture _loader;
        private readonly ReusableFuture<bool, bool> _saver;
        private int _previousLODIndex = -1;
        private bool _heightMapReceived;
        private Vector2 _sampleCenter;
        private bool _hasSetCollider;
        private bool _startedToRelease;
        private bool _updated;
        private bool _isActive = true;
        private Vector3 _transformPosition = Vector3.zero;
        private string _name;
        private bool _appliedMeshMaterial;
        private SaveData _saveData;
        private readonly int _numCancels;
        //###############################################################
        //update methods variables if the update calls are
        //moved to a separate thead these values will need to be local to
        //the methods that are using them.
        private int _updateLODIndex;
        private int _updateOldLODIndex;
        private ChunkMesh _updateLODMesh;
        private Vector2 _floatCoord;
        private DistanceValue? _distanceFromViewerChunk;
        private float _updateDistFromViewerSq;
        private bool _updateWasVisible;
        private bool _updateVisible;
        private int _cancelCalls;
        //###############################################################
        #endregion

        #region Events
        
        /// <summary>
        /// This event is triggered when a chunks visibility changes.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static OnChunkVisibilityChangedDelegate onVisibilityChanged;

        /// <summary>
        /// This event is triggered when a chunks lod changes.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static OnLodChangedDelegate onLodChanged;

        /// <summary>
        /// This event is triggered when a chunk is loaded.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static OnChunkLoadedDelegate onChunkLoaded;

        /// <summary>
        /// This event is triggered when a chunk is saved.
        /// </summary>
        /// ReSharper disable once UnassignedField.Global
        public static OnChunkSavedDelegate onChunkSaved;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// This property is used to get the distance from the viewer's chunk.  This
        /// value is cleared at the end of each update.
        /// </summary>
        private DistanceValue Distance{ get {
            _distanceFromViewerChunk??= new DistanceValue(0, (Id - _mapManager.ViewerChunk).sqrMagnitude);
            return _distanceFromViewerChunk.Value;
        }}
        
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
                    if(_gameObject!=null)_gameObject.SetActive(value);
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
                    if(_gameObject!=null) _gameObject.name = value;
                    _name = value;
                });
            }
        }
        
        /// <summary>
        /// This property is used to get the chunk's transform position.  It can also
        /// be used to protected set the transforms position.  This property insures that
        /// setting or getting the Transform's position is thread safe.
        /// </summary>
        public Vector3 TransformPosition {
            get => _transformPosition;
            set {
                Dispatcher.InvokeAsync(() => {
                    if(_gameObject!=null&&_gameObject.transform!=null)
                        _gameObject.transform.position = value;
                    _transformPosition = value;
                });
            }
        }
        
        /// <summary>
        /// This property contains the chunk id.
        /// </summary>
        public Vector2Int Id { get; private set; }
        
        /// <summary>
        /// This property is used to check if any of the chunk data has been updated.
        /// </summary>
        public bool HasBeenUpdated => _lodMeshes.Any(x => x.HasBeenUpdated) 
                || _biomeMap.HasBeenUpdated || _biomeMap.HeightMap.HasBeenUpdated;
        
        #endregion

        #region Constructors
        
        /// <summary>
        /// This is only used for the reference version
        /// </summary>
        public Chunk() { }

        /// <summary>
        /// This constructor is used to create a new reusable chunk.
        /// </summary>
        /// <param name="mapManager">The map manager that will use this chunk.</param>
        /// <param name="mapPool">The chunk pool that contains this chunk.</param>
        public Chunk(MapManager mapManager, MapPool<Chunk> mapPool) {
            //make sure the chunk gameObject and components get
            //created on the main thead
            Dispatcher.Invoke(() => {
                _gameObject = new GameObject{
                    transform = { parent = mapManager.transform }
                };
                _meshFilter = _gameObject.AddComponent<MeshFilter>();
                _meshCollider = _gameObject.AddComponent<MeshCollider>();
                _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
                _transformPosition = _gameObject.transform.position;
            });
            Name = CHUNK_POOLED;
            _meshSettings = mapManager.MeshSettings;
            _biomeMap = new BiomeMap(mapManager.Seed,mapManager.MeshSettings.VertsPerLine, mapManager.BiomeSettings);
            _preparedColors = new Color[_biomeMap.GetBorderCulledValuesCount(1)];
            //create meshes
            _detailLevels = _meshSettings.LevelsOfDetail.ToArray();
            _lodMeshes = new ChunkMesh[_detailLevels.Length];
            _mapPool = mapPool;
            _mapSaver = mapManager.MapSaver;
            for(var i = 0; i < _detailLevels.Length; i++) {
                _lodMeshes[i] = new ChunkMesh(_meshSettings, _detailLevels[i].LevelsOfDetail);
                _lodMeshes[i].UpdateCallback += UpdateChunk;
                if(i == _meshSettings.ColliderLODIndex)
                    _lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
            _numCancels = _lodMeshes.Length+1;
            //get the manager
            _mapManager = mapManager;
            //setup loader
            _loader = new ReusableFuture();
            _loader.OnProcess(ProcessLoad).OnSuccess(OnLoadComplete).OnCancel(FutureCancel,false);
            //setup saver
            _saver = new ReusableFuture<bool, bool>();
            _saver.OnProcess(ProcessSave).OnSuccess(SaveComplete).OnCancel(FutureCancel,false);
            //subscribe to the events
            _mapManager.OnUpdateVisible += UpdateChunk;
            _mapManager.OnEndUpdate += ValidateNonUpdatedChunk;
            _mapManager.OnUpdateCollisionMesh += UpdateCollisionMesh;
            //if not in use disable gameObject
            if(!IsInUse) Active = false;
        }

        #endregion
        
        #region Pool Methods

        /// <summary>
        /// This method is used by the MapPool to create a new chunk.
        /// </summary>
        /// <param name="mapManager">The map manager to assign to the chunk.</param>
        /// <param name="mapPool">The chunks map pool.</param>
        /// <returns>The newly created chunk.</returns>
        public Chunk CreateMapComponent(MapManager mapManager, MapPool<Chunk> mapPool) {
            return new Chunk(mapManager, mapPool);
        }
        
        /// <summary>
        /// This method is used to update the chunk to be used as a
        /// specified chunk.
        /// </summary>
        /// <param name="chunkId">The chunks id.</param>
        public void Setup(Vector2Int chunkId) {
            Id = chunkId;
            _saveData = _mapSaver.NewChunkSaveData(Id);
            _floatCoord = new Vector2(Id.x, Id.y) * _meshSettings.MeshWorldSize;
            _sampleCenter = _floatCoord / _meshSettings.MeshScale;
            Name = $"Chunk ({Id.x},{Id.y})";
            TransformPosition = new Vector3(_floatCoord.x, 0, _floatCoord.y);
            _loader.Process();
        }

        /// <summary>
        /// This method is used to mark the chunk as in use so
        /// that the chunk pool will not issue it again.
        /// </summary>
        /// <param name="setActive">If true the GameObject will be
        /// set to active.</param>
        public void PullFromPool(bool setActive = false) {
            IsInUse = true;
            if(setActive) Active = true;
        }

        /// <summary>
        /// This method is used to dispose of a chunk and add it back
        /// to the chunk pool.
        /// </summary>
        public void ReleaseToPool() {
            if(_startedToRelease) return;
            _startedToRelease = true;
            _cancelCalls = 0;
            //cancel current actions
            _loader.Cancel();
            //save if saving is enabled
            if(_mapSaver.SavingEnabled&&HasBeenUpdated) {
                _saver.Process(true);
                return;
            }
            //if not saving
            SendToPool();
        }
        
        /// <summary>
        /// This method should only be called from <see cref="ReleaseToPool"/> or
        /// <see cref="SaveComplete"/>.  If you want to return the object to the
        /// <see cref="MapPool{T}"/> see <see cref="ReleaseToPool"/>.
        /// </summary>
        private void SendToPool() {
            Active = false;
            Name = CHUNK_POOLED;
            //reset the mesh values
            foreach(var mesh in _lodMeshes) mesh.Reset(FutureCancel);
            //the reset and cancellation methods will call the FutureCancel method and
            //that is were this process continues.
        }

        /// <summary>
        /// This method is called when any futures related to this chunk are canceled.
        /// </summary>
        private void FutureCancel() {
            if(!_startedToRelease) return;
            _cancelCalls++;
            if(_cancelCalls != _numCancels) return;
            //this used to be in the send to pool method ///////
            _previousLODIndex = -1;
            _hasSetCollider = false;
            _heightMapReceived = false;
            //return to pool
            IsInUse = false;
            _startedToRelease = false;
            _mapPool.EnqueueItem(this);
            ////////////////////////////////////////////////////
        }
        
        #endregion
        
        #region Saving
        
        /// <summary>
        /// This method is called after the save process has been
        /// successfully completed.
        /// </summary>
        /// <param name="releaseFromPool">True if the chunk should be
        /// released to the pool, otherwise false.</param>
        private void SaveComplete(bool releaseFromPool) {
            onChunkSaved?.Invoke(Id);
            if(releaseFromPool) SendToPool();
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
            if(!_mapSaver.SavingEnabled && releaseFromPool || !_heightMapReceived) 
                return releaseFromPool;
            //save the biome data
            var updated = _biomeMap.Save(_saveData);
            //save mesh data
            if(_mapSaver.SaveMeshData) {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach(var mesh in _lodMeshes) updated = updated || mesh.Save(_saveData);
            }
            //if anything has been updated save the chunk
            if(updated) _mapManager.MapSaver.SaveData(Id, _saveData);
            return releaseFromPool;
        }

        #endregion
        
        #region Loading
        
        /// <summary>
        /// This method is called after the load process has been completed
        /// successfully.
        /// </summary>
        private void OnLoadComplete() {
            if(!_appliedMeshMaterial) {
                if(_meshRenderer!=null)_meshRenderer.material = _meshSettings.Material;
                _appliedMeshMaterial = true;
            }
            if(_meshSettings.PaintingMode != TerrainPaintingMode.Material) {
                _previewTexture = _biomeMap.GenerateTexture(_preparedColors,1);
                if(_meshRenderer!=null)_meshRenderer.material.mainTexture = _previewTexture;
            }
            _heightMapReceived = true;
            UpdateChunk();
            onChunkLoaded?.Invoke(Id);
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
            if(_mapSaver.SavingEnabled && _mapManager.MapSaver.LoadData(Id, out _saveData)) {
                //the save data was found so load the data
                _biomeMap.Load(_saveData);
                if(_mapSaver.SaveMeshData) {
                    foreach(var mesh in _lodMeshes) {
                        token.ThrowIfCancellationRequested();
                        mesh.Load(_saveData);
                    }
                }
            }else {
                //nothing has been loaded so generate new
                token.ThrowIfCancellationRequested();
                _biomeMap.Generate(_sampleCenter, token);
                if(_mapSaver.SaveOnGenerate) {
                    //save the generated data
                    _biomeMap.Save(_saveData);
                    _mapSaver.SaveData(Id, _saveData);
                }
                //generate the mesh data using the same process
                if(_meshSettings.CalculateMeshDataOnLoad) {
                    foreach(var mesh in _lodMeshes) {
                        token.ThrowIfCancellationRequested();
                        mesh.RequestMesh(_biomeMap.HeightMap, _loader, token, _mapManager.ApplyHeight);
                    }
                }
            }
            //generate texture
            token.ThrowIfCancellationRequested();
            _biomeMap.GenerateTextureColors(_preparedColors, _meshSettings.PaintingMode, 1);
            return true;
        }
        
        #endregion
        
        #region Update Methods
        
        /// <summary>
        /// This is a helper method that can be used to call the update chunk from within this class.
        /// </summary>
        private void UpdateChunk() => UpdateChunk(ChunkRange.maxRange);
        
        /// <summary>
        /// This method is called when the chunk should be updated.  This
        /// is called if the chunk is visible and the player moved enough
        /// that the chunks need to update.
        /// </summary>
        /// <remarks>This method uses variables outside of the method and is not thread safe.</remarks>
        public void UpdateChunk(ChunkRange chunkRange) {
            if(!_heightMapReceived|| !IsInUse) return;
            if(!chunkRange.IsInRange(Id)) {
                //out of range so make sure it is disabled
                if(!_isActive) return;
                Active = false;
                onVisibilityChanged?.Invoke(Id,false);
                return;
            }
            _updated = true;
            _updateDistFromViewerSq = Distance[true];
            _updateWasVisible =  _isActive;
            _updateVisible = _updateDistFromViewerSq <= _meshSettings.MaxViewDistance[true];
            if(_updateVisible) UpdateLOD(_updateDistFromViewerSq);
            if(_updateWasVisible == _updateVisible) return;
            Active = _updateVisible;
            onVisibilityChanged?.Invoke(Id,_updateVisible);
        }

        /// <summary>
        /// This method is called when the <see cref="MapManager"/> is ending the update cycle.
        /// </summary>
        public void ValidateNonUpdatedChunk() {
            if(!IsInUse || _updated || _startedToRelease) { _updated = false;return; }
            _updated = false;
            if(Distance[true] < _meshSettings.UnloadDistance[true]) return;
            _mapPool.ReturnToPool(this);
        }
        
        /// <summary>
        /// This method is used to handle the collision mesh.
        /// </summary>
        public void UpdateCollisionMesh() {
            if(!IsInUse) return;
            _distanceFromViewerChunk = null;
            if(!_isActive || _hasSetCollider) return;
            _updateDistFromViewerSq = Distance[true];
            if(_updateDistFromViewerSq < _detailLevels[_meshSettings.ColliderLODIndex].DistanceSq)
                if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasRequestedMesh)
                    _lodMeshes[_meshSettings.ColliderLODIndex].RequestMeshAsync(_biomeMap.HeightMap, _mapManager.ApplyHeight);
            if(_updateDistFromViewerSq > _mapManager.ColliderGenerationThreshold[true]) return;
            if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasMeshData) return;
            _lodMeshes[_meshSettings.ColliderLODIndex].AssignTo(_meshCollider);
            _hasSetCollider = true;
        }
        
        #endregion

        /// <summary>
        /// This method is used to update the chunks level of detail.
        /// </summary>
        /// <param name="distanceFromViewerSq">The chunks distance from the
        /// viewer.</param> 
        /// <remarks>This method uses variables outside of the method and is not thread safe.</remarks>
        private void UpdateLOD(float distanceFromViewerSq) {
            _updateLODIndex = 0;
            for(var i = 0; i < _detailLevels.Length - 1; i++) {
                if(distanceFromViewerSq > _detailLevels[i].DistanceSq)
                    _updateLODIndex = i + 1;
                else break;
            }
            if(_updateLODIndex == _previousLODIndex) return;
            _updateLODMesh = _lodMeshes[_updateLODIndex];
            if(_updateLODMesh.HasMeshData) {
                _updateOldLODIndex = _previousLODIndex;
                _previousLODIndex = _updateLODIndex;
                _updateLODMesh.AssignTo(_meshFilter);
                onLodChanged?.Invoke(Id,_updateOldLODIndex,_updateLODIndex);
            }else if(!_updateLODMesh.HasRequestedMesh) {
                _updateLODMesh.RequestMeshAsync(_biomeMap.HeightMap, _mapManager.ApplyHeight);
            }
        }

    }
}