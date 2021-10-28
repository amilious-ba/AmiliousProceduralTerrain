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

        [SerializeField] private MapType mapType = MapType.Pregenerated;
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

        private float _sqrChunkUpdateThreshold;
        private Vector2 _viewerPosition;
        private Vector2Int _viewerChunk;
        private Vector2 _oldViewerPosition;
        private float? _sqrColliderGenerationThreshold = null;
        private int? _hashedSeed = null;
        private ChunkPool _chunkPool;
        private readonly ConcurrentDictionary<Vector2Int, Chunk> _mapChunks = 
            new ConcurrentDictionary<Vector2Int, Chunk>();
        private readonly List<Chunk> _visibleMapChunks = new List<Chunk>();
        
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
            get {
                return _mapChunks.TryGetValue(coord, out var chunk) ? chunk : null;
            }
        }

        private void Start() {
            _chunkPool = new ChunkPool(this, chunkPoolSize, generateChunksAtStart);
            //we use a squared threshold because it is cheaper to calculate a squared
            //distance than a normal distance.
            _sqrChunkUpdateThreshold = chunkUpdateThreshold * chunkUpdateThreshold;
            UpdateVisibleChunks();
        }

        private void Update() {
            
            //get the player position
            _viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
            var currentChunkX = Mathf.RoundToInt(_viewerPosition.x / meshSettings.MeshWorldSize);
            var currentChunkY = Mathf.RoundToInt(_viewerPosition.y / meshSettings.MeshWorldSize);
            _viewerChunk = new Vector2Int(currentChunkX, currentChunkY);
            
            //check for collision mesh update
            if(_viewerPosition != _oldViewerPosition) {
                foreach(var chunk in _visibleMapChunks) chunk.UpdateCollisionMesh();
            }

            //check if visible chunks need to be updated.
            if((_oldViewerPosition - _viewerPosition).sqrMagnitude > _sqrChunkUpdateThreshold) {
                _oldViewerPosition = _viewerPosition;
                UpdateVisibleChunks();
            }
            
        }

        /// <summary>
        /// This method is used update visible chunks.
        /// </summary>
        private void UpdateVisibleChunks() {
            
            //update visible chunks
            var updated = new HashSet<Vector2>();
            for(int i = _visibleMapChunks.Count - 1; i >= 0; i--) {
                updated.Add(_visibleMapChunks[i].Coordinate);
                _visibleMapChunks[i].UpdateChunk();
            }
            
            //update non-visible or generated chunks that are in range but not visible
            var chunks = meshSettings.ChunksVisibleInViewDistance;
            for(var xOff = - chunks; xOff <= chunks; xOff++)
            for(var yOff = -chunks; yOff <= chunks; yOff++) {
                var chunkCoord = new Vector2Int(_viewerChunk.x + xOff, _viewerChunk.y + yOff);
                if(updated.Contains(chunkCoord)) continue;
                if(_mapChunks.TryGetValue(chunkCoord, out var chunk)) {
                    chunk.UpdateChunk(); continue;
                }
                //var newChunk = new MapChunk(this,chunkCoord);

                //new system
                var newChunk = _chunkPool.GetAvailableChunk().Setup(chunkCoord);
                _mapChunks.TryAdd(chunkCoord, newChunk);
                newChunk.OnVisibilityChanged += OnMapChunkVisibilityChanged;
                //newChunk.Load();
            }
        }

        public bool ReleaseChunkReference(Vector2Int chunkCoord) {
            var result = _mapChunks.TryRemove(chunkCoord, out var chunk);
            _visibleMapChunks.Remove(chunk);
            return result;
        }

        /// <summary>
        /// This method is called when a chunks visibility changes.
        /// </summary>
        /// <param name="chunk">The chunk that changed.</param>
        /// <param name="isVisible">True if the chunk is visible, otherwise false.</param>
        private void OnMapChunkVisibilityChanged(Chunk chunk, bool isVisible) {
            if(isVisible) _visibleMapChunks.Add(chunk);
            else _visibleMapChunks.Remove(chunk);
        }
        
    }
}