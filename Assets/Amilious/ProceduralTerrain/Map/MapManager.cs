using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.Random;
using Amilious.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This class will be used for generating terrain.  This will be the brain
    /// that loads and unloads chunks from the world.  It is also the class that
    /// will generate and hold all of the world.
    /// </summary>
    [RequireComponent(typeof(Dispatcher)), HideMonoScript]
    public class MapManager : MonoBehaviour {

        /*
         * Generate chunk batches that will be saved together in the same file
         * Load and unload chunks based on distance from viewer.  Move the generator
         * when the player moved to far away from the origin.
         */

        [SerializeField] private MapType mapType = MapType.PreGenerated;
        [SerializeField] private bool enableSavingAndLoading = false;
        [SerializeField] private int chunkPoolSize = 100;
        [SerializeField] private bool generateChunksAtStart;
        [SerializeField, Tooltip("This is the distance the player needs to move before the chunk will update.")]
        private float chunkUpdateThreshold = 25f;
        [SerializeField] private float colliderGenerationThreshold = 5;
        [SerializeField, Required] private string seed;
        [SerializeField] private MapPaintingMode mapPaintingMode;
        [SerializeField] private bool applyHeight = true;
        [SerializeField, Required] private MeshSettings meshSettings;
        [SerializeField, Required] private BiomeSettings biomeSettings;
        [SerializeField] private Transform viewer;

        public event Action OnStartUpdate;
        public event Action<int,int,int,int> OnUpdateVisible;
        public event Action OnEndUpdate;
        public event Action OnUpdateCollisionMesh;
        
        private float _sqrChunkUpdateThreshold;
        private Vector2 _viewerPosition;
        private Vector2Int _viewerChunk;
        private Vector2 _oldViewerPosition;
        private float? _sqrColliderGenerationThreshold = null;
        private int? _hashedSeed = null;
        private ChunkPool _chunkPool;
        private readonly ConcurrentDictionary<Vector2Int, Chunk> _mapChunks = 
            new ConcurrentDictionary<Vector2Int, Chunk>();
        
        public bool ApplyHeight { get => applyHeight; }
        
        public MeshSettings MeshSettings { get => meshSettings; }
        public BiomeSettings BiomeSettings { get => biomeSettings; }
        
        public MapPaintingMode MapPaintingMode { get => mapPaintingMode; }

        public float SqrColliderGenerationThreshold {
            get {
                _sqrColliderGenerationThreshold??= colliderGenerationThreshold * colliderGenerationThreshold;
                return _sqrColliderGenerationThreshold.Value;
            }
        }

        public int HashedSeed {
            get {
                _hashedSeed ??= SeedGenerator.GetSeedInt(seed);
                return _hashedSeed.Value;
            }
        }
        
        public MapType MapType { get => mapType; }
        
        public bool SaveEnabled { get => enableSavingAndLoading; }
        
        public Transform Viewer { get => viewer; }
        
        public string Seed { get => seed; }

        /// <summary>
        /// This can be used to get a loaded chunk based on it's coordinate.
        /// </summary>
        /// <param name="coord">Returns the chunk if it is loaded, otherwise
        /// returns null.</param>
        public Chunk this[Vector2Int coord] {
            get => _mapChunks.TryGetValue(coord, out var chunk) ? chunk : null;
        }

        public bool IsVisible(Vector2Int key) => _mapChunks.TryGetValue(key, out var chunk) 
            && chunk != null && chunk.gameObject.activeSelf;

        private void Start() {
            _chunkPool = new ChunkPool(this, chunkPoolSize, generateChunksAtStart);
            //we use a squared threshold because it is cheaper to calculate a squared
            //distance than a normal distance.
            _sqrChunkUpdateThreshold = chunkUpdateThreshold * chunkUpdateThreshold;
            UpdateVisibleChunks();
        }

        private void Update() {
            
            //get the player position
            var position = viewer.position;
            _viewerPosition = new Vector2(position.x, position.z);
            _viewerChunk = ChunkAtPoint(_viewerPosition);
            
            //check for collision mesh update
            if(_viewerPosition != _oldViewerPosition) {
                OnUpdateCollisionMesh?.Invoke();
            }

            //check if visible chunks need to be updated.
            if(!((_oldViewerPosition - _viewerPosition).sqrMagnitude > _sqrChunkUpdateThreshold)) return;
            _oldViewerPosition = _viewerPosition;
            UpdateVisibleChunks();
        }

        public Vector2Int ChunkAtPoint(Vector3 point) {
            return new Vector2Int(
                Mathf.RoundToInt(point.x / meshSettings.MeshWorldSize),
                Mathf.RoundToInt(point.z / meshSettings.MeshWorldSize)
            );
        }

        /// <summary>
        /// This method is used to get the chunk id for the chunk
        /// at the given <see cref="Vector2"/> position.
        /// </summary>
        /// <param name="point">The position you want to get the chunk of.</param>
        /// <returns>The chunk id or coordinate at the given position.</returns>
        public Vector2Int ChunkAtPoint(Vector2 point) {
            return new Vector2Int(
                Mathf.RoundToInt(point.x / meshSettings.MeshWorldSize),
                Mathf.RoundToInt(point.y / meshSettings.MeshWorldSize)
            );
        }

        /// <summary>
        /// This method is used update visible chunks.
        /// </summary>
        private void UpdateVisibleChunks() {
            OnStartUpdate?.Invoke();
            var chunks = meshSettings.ChunksVisibleInViewDistance;
            OnUpdateVisible?.Invoke(_viewerChunk.x-chunks,_viewerChunk.x+chunks,
                _viewerChunk.y-chunks,_viewerChunk.y+chunks);
            for(var xOff = - chunks; xOff <= chunks; xOff++)
            for(var yOff = -chunks; yOff <= chunks; yOff++) {
                var chunkCoord = new Vector2Int(_viewerChunk.x + xOff, _viewerChunk.y + yOff);
                if(_mapChunks.ContainsKey(chunkCoord)) continue;
                var newChunk = _chunkPool.GetAvailableChunk().Setup(chunkCoord);
                _mapChunks.TryAdd(chunkCoord, newChunk);
            }
            OnEndUpdate?.Invoke();
        }

        /// <summary>
        /// This method is called to remove a chunk from the loaded world.
        /// </summary>
        /// <param name="chunkCoord">The coordinate of the chunk that
        /// you want to remove from the loaded world.</param>
        /// <returns>True if the chunk was removed from the loaded world,
        /// otherwise returns false if the given chunk was not loaded.</returns>
        public bool ReleaseChunkReference(Vector2Int chunkCoord) {
            return _mapChunks.TryRemove(chunkCoord, out _);
        }
        
    }
}