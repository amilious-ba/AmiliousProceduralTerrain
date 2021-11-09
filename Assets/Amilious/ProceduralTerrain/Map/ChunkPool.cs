using UnityEngine;
using System.Collections.Concurrent;

namespace Amilious.ProceduralTerrain.Map {
    
    //This class is used for pooling chunks.
    public class ChunkPool {

        #region Private Instance Variables
        
        private readonly MapManager _manager;
        private readonly ConcurrentDictionary<Vector2Int, Chunk> _loadedChunks = 
            new ConcurrentDictionary<Vector2Int, Chunk>();
        private readonly ConcurrentQueue<Chunk> _chunkQueue;
        
        #endregion

        #region Public Properties
        
        /// <summary>
        /// This property is used to get a loaded chunk using it's chunkId.  If
        /// the chunk is not loaded this will return null.
        /// </summary>
        /// <param name="chunkId">Returns the chunk if it is loaded, otherwise
        /// returns null.</param>
        public Chunk this[Vector2Int chunkId] {
            get => _loadedChunks.TryGetValue(chunkId, out var chunk) ? chunk : null;
        }

        /// <summary>
        /// This property is used to get a loaded chunk using it's chunkId x and z values.
        /// If the chunk is not loaded this will return null.
        /// </summary>
        /// <param name="x">The x value of the chunkId.</param>
        /// <param name="z">The z (uses y) value of the chunkId.</param>
        public Chunk this[int x, int z] { get => this[new Vector2Int(x, z)]; }

        /// <summary>
        /// This property returns the number of chunks that are loaded but not being used.
        /// </summary>
        public int NumberOfUnusedChunks => _chunkQueue.Count;

        /// <summary>
        /// This property returns the number of loaded chunks.
        /// </summary>
        public int NumberOfLoadedChunks => _loadedChunks.Count;

        /// <summary>
        /// This property returns the total number of used and unused chunks in this pool.
        /// </summary>
        public int PoolSize => NumberOfUnusedChunks + NumberOfLoadedChunks;
        
        /// <summary>
        /// This property is used to check if the <see cref="Chunk"/> with the given id is visible.
        /// </summary>
        /// <param name="chunkId">The id of the <see cref="Chunk"/> that you want to check.</param>
        /// <returns>True if the <see cref="Chunk"/> with the given id is loaded and visible.</returns>
        public bool IsVisible(Vector2Int chunkId) => 
            _loadedChunks.TryGetValue(chunkId, out var chunk) && chunk is { Active: true };
        
        #endregion


        #region Constructors
        
        /// <summary>
        /// This constructor is used to create a new <see cref="ChunkPool"/>.
        /// </summary>
        /// <param name="manager">The <see cref="MapManager"/> that will use this
        /// <see cref="ChunkPool"/>.</param>
        /// <param name="preloadSize">If this value is set, the pool will preload the
        /// given number of <see cref="Chunk"/>s.</param>
        public ChunkPool(MapManager manager, int? preloadSize = null) {
            _manager = manager;
            _chunkQueue = new ConcurrentQueue<Chunk>();
            if(!preloadSize.HasValue) return;
            for(var i=0;i<preloadSize.Value;i++)
                _chunkQueue.Enqueue(new Chunk(_manager, this));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to load a chunk.  This will use an available <see cref="Chunk"/>
        /// if one exists in the pool, otherwise it will create a new <see cref="Chunk"/>.
        /// </summary>
        /// <param name="chunkId">This id of the <see cref="Chunk"/> that you want to load.</param>
        /// <param name="chunk">The loaded chunk.</param>
        /// <returns>True if the chunk was loaded, otherwise returns false if the chunk was
        /// already loaded.</returns>
        public bool LoadChunk(Vector2Int chunkId, out Chunk chunk) {
            if(_loadedChunks.TryGetValue(chunkId, out chunk)) return false;
             if(!_chunkQueue.TryDequeue(out chunk)){
                 //no available chunk so create new chunk
                 chunk = new Chunk(_manager,this);
             }
            if(chunk == null) return false;
             chunk.PullFromPool();
             chunk.Setup(chunkId);
             _loadedChunks[chunkId] = chunk;
             return true;
         }

        /// <summary>
        /// This method is used to load a chunk.  This will use an available <see cref="Chunk"/>
        /// if one exists in the pool, otherwise it will create a new <see cref="Chunk"/>.
        /// </summary>
        /// <param name="chunkId">This id of the <see cref="Chunk"/> that you want to load.</param>
        /// <returns>True if the chunk was loaded, otherwise returns false if the chunk was
        /// already loaded.</returns>
        public bool LoadChunk(Vector2Int chunkId) {
            if(_loadedChunks.TryGetValue(chunkId, out _)) return false;
            if(!_chunkQueue.TryDequeue(out var chunk)){
                //no available chunk so create new chunk
                chunk = new Chunk(_manager,this);
            }
            if(chunk == null) return false;
            chunk.PullFromPool();
            chunk.Setup(chunkId);
            _loadedChunks[chunkId] = chunk;
            return true;
        }

        /// <summary>
        /// This method is used to return a chunk to the pool.
        /// </summary>
        /// <param name="chunk">The chunk that you want to return to the pool.</param>
        /// <returns>True if the chunk was removed from the loaded chunks, otherwise
        /// returns false.</returns>
        public bool ReturnToPool(Chunk chunk) {
             if(chunk == null) return false;
             if(!chunk.HasProcessedRelease) return chunk.ReleaseToPool();
             var removed = _loadedChunks.TryRemove(chunk.ChunkId, out _);
             _chunkQueue.Enqueue(chunk);
             return removed;
         }
         
        /// <summary>
        /// This method is used to return a chunk to the pool.
        /// </summary>
        /// <param name="chunkId">The id of the chunk that you want to return to the pool.</param>
        /// <returns>True if the chunk was removed from the loaded chunks, otherwise
        /// returns false.</returns>
        public bool ReturnToPool(Vector2Int chunkId) {
             return _loadedChunks.TryGetValue(chunkId, out var chunk) && ReturnToPool(chunk);
         }
        
        #endregion

    }
}