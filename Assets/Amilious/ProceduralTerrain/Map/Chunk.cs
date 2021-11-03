using System;
using System.Linq;
using UnityEngine;
using Amilious.Threading;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Noise;
using Amilious.ProceduralTerrain.Textures;
using Sirenix.OdinInspector;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This class will represent a chunk
    /// </summary>
    [HideMonoScript]
    public class Chunk : MonoBehaviour {
        
        #region Chunk Logger
        private bool UseChunkLog { get => _manager.UseChunkLog; }
        [ShowInInspector, DisplayAsString(false), HideLabel, ShowIf(nameof(UseChunkLog))]
        private string _chunkLog = string.Empty;
        private void LogChunk(string message, bool showId = false) {
            if(!_manager.UseChunkLog) return;
            lock(_chunkLog) {
                if(_chunkLog != string.Empty) _chunkLog += '\n';
                var thread = Dispatcher.IsMainThread ? "Main     Thread" : "Worker Thread";
                _chunkLog += $"[{thread}] {(showId ? $"({Coordinate.x},{Coordinate.y})" : "")} {message}";
            }
        }
        #endregion
        
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
        private NoiseMap _heightMap;
        private BiomeMap _biomeMap;
        private Texture2D _previewTexture;
        private Color[] _preparedColors;
        private int _seed;
        
        public event Action<Chunk, bool> OnVisibilityChanged;
        
        public bool IsInUse { get; private set; } = false;
        public Vector2Int Coordinate { get; private set; }
        private Vector2 ViewerPosition => new Vector2 (_viewer.position.x, _viewer.position.z);

        private void Awake() {
            //get the manager
            _manager = GetComponentInParent<MapManager>();
            if(_manager==null) Debug.LogWarning("The chunk is unable to find it's parent.");
            //create the required components
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshCollider = gameObject.AddComponent<MeshCollider>();
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            //if not in use disable gameObject
            if(!IsInUse) gameObject.SetActive(false);
            LogChunk("Added Game Object Components.");
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
        /// <returns>This same chunk so that this method can be called
        /// on the same like as getting the chunk from the pool.</returns>
        public Chunk Setup(Vector2Int coordinate) {
            Coordinate = coordinate;
            LogChunk("Setting Up Chunk", true);
            var floatCoord = new Vector2(Coordinate.x, Coordinate.y);
            _sampleCenter = floatCoord * _meshSettings.MeshWorldSize / _meshSettings.MeshScale;
            _position = floatCoord * _meshSettings.MeshWorldSize;
            _bounds = new Bounds(_position, Vector3.one * _meshSettings.MeshWorldSize);
            gameObject.name = $"Chunk ({Coordinate.x},{Coordinate.y})";
            transform.position = new Vector3(_position.x, 0, _position.y);
            Load();
            return this;
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
            LogChunk("Pulled from pool.");
        }

        /// <summary>
        /// This method is used to dispose of a chunk and add it back
        /// to the chunk pool.
        /// </summary>
        public void ReleaseToPool() {
            if(_manager.SaveEnabled) {
                //TODO: save the chunk data
            }
            _manager.OnStartUpdate -= StartUpdateCycle;
            _manager.OnUpdateVisible -= UpdateChunk;
            _manager.OnEndUpdate -= ValidateNonUpdatedChunk;
            _manager.OnUpdateCollisionMesh -= UpdateCollisionMesh;
            gameObject.SetActive(false);
            //remove the chunk from the dictionary
            _manager.ReleaseChunkReference(Coordinate);
            //reset the mesh values
            foreach(var mesh in _lodMeshes) mesh.Reset();
            _previousLODIndex = -1;
            _hasSetCollider = false;
            _heightMapReceived = false;
            gameObject.name = $"Chunk (pooled)";
            //return to pool
            LogChunk("Returned to pool.");
            IsInUse = false;
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
            if(Coordinate.x < xMin || Coordinate.x > xMax) return;
            if(Coordinate.y < yMin || Coordinate.y > yMax) return;
            _updated = true;
            //var distanceFromViewer = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));
            var distanceFromViewerSq =
                _bounds.SqrDistance(ViewerPosition);
            var wasVisible =  gameObject.activeSelf;
            var visible = distanceFromViewerSq <= _meshSettings.MaxViewDistanceSq;
            if(visible) UpdateLOD(distanceFromViewerSq);
            if(wasVisible == visible) return;
            LogChunk($"SetActive({visible})",true);
            gameObject.SetActive(visible);
            OnVisibilityChanged?.Invoke(this,visible);
        }

        public void ValidateNonUpdatedChunk() {
            if(!IsInUse||gameObject.activeSelf||_updated) return;
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
                LogChunk($"Loaded existing mesh for LOD {lodIndex}!",true);
            }else if(!lodMesh.HasRequestedMesh) {
                lodMesh.RequestMesh(_heightMap, _meshSettings, _manager.ApplyHeight);
                LogChunk($"Requesting mesh for LOD {lodIndex}!",true);
            }
        }
        
        /// <summary>
        /// This method is used to load the chunk.
        /// </summary>
        private void Load() {
            if(_manager.SaveEnabled) {
                //ToDo:Try load from file
            }
            var future = new Future<NoiseMap>();
            future.OnSuccess(heightMap => {
                _heightMap = heightMap.value;
                if(_manager.MapPaintingMode != MapPaintingMode.Material) {
                    _previewTexture = _biomeMap.GenerateTexture(_preparedColors,1);
                    _meshRenderer.material.mainTexture = _previewTexture;
                }
                _heightMapReceived = true;
                LogChunk("Received biome and height maps!", true);
                UpdateChunk();
            });
            future.OnError(x => Debug.LogError(x.error));
            future.Process(()=> {
                //generate the biome map
                LogChunk("Generating biome and height maps!", true);
                _biomeMap = _biomeSettings.GenerateBiomeMap(_meshSettings.VertsPerLine, _seed, _sampleCenter);
                var heightMap = _biomeMap.GenerateHeightMap();
                _preparedColors = _biomeMap.GenerateTextureColors(heightMap, _manager.MapPaintingMode, 1);
                return heightMap;
            });
        }

        /// <summary>
        /// This method is used to handle the collision mesh.
        /// </summary>
        public void UpdateCollisionMesh() {
            if(!IsInUse || !gameObject.activeSelf || _hasSetCollider) return;
            var sqrDistanceFromViewer = _bounds.SqrDistance(ViewerPosition);
            if(sqrDistanceFromViewer < _detailLevels[_meshSettings.ColliderLODIndex].SqrVisibleDistanceThreshold)
                if(!_lodMeshes[_meshSettings.ColliderLODIndex].HasRequestedMesh)
                    _lodMeshes[_meshSettings.ColliderLODIndex].RequestMesh(_heightMap, _meshSettings, _manager.ApplyHeight);
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
        public static Chunk CreateNew(MapManager manager) {
            var gameObject = new GameObject($"Chunk (pooled)") {
                transform = { parent = manager.transform }
            };
            var chunk = gameObject.AddComponent<Chunk>();
            chunk._meshSettings = manager.MeshSettings;
            chunk._biomeSettings = manager.BiomeSettings;
            chunk._viewer = manager.Viewer;
            chunk._seed = manager.HashedSeed;
            //create meshes
            chunk._detailLevels = chunk._meshSettings.LevelsOfDetail.ToArray();
            chunk._lodMeshes = new ChunkMesh[chunk._detailLevels.Length];
            for(var i = 0; i < chunk._detailLevels.Length; i++) {
                chunk._lodMeshes[i] = new ChunkMesh(chunk._meshSettings.VertsPerLine,
                    chunk._detailLevels[i].SkipStep, chunk._meshSettings.UseFlatShading ,
                    chunk._detailLevels[i].LOD);
                chunk._lodMeshes[i].UpdateCallback += chunk.UpdateChunk;
                if(i == chunk._meshSettings.ColliderLODIndex)
                    chunk._lodMeshes[i].UpdateCallback += chunk.UpdateCollisionMesh;
            }
            chunk.LogChunk("Created the chunk.");
            return chunk;
        }
    }
}