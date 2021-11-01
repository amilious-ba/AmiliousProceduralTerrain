using System.Collections.Generic;
using System.Linq;

namespace Amilious.ProceduralTerrain.Map {
    
    //This class is used for pooling chunks.
    public class ChunkPool {

        private readonly MapManager _manager;
        private readonly List<Chunk> _chunkPool;
        private readonly object _poolLock = new object();

        /// <summary>
        /// This constructor is used to create a new <see cref="ChunkPool"/>.
        /// </summary>
        /// <param name="manager">The map manager that will use the <see cref="ChunkPool"/>.</param>
        /// <param name="poolSize">The initial size of the pool.  The pool will dynamically
        /// increase in size, but it is best to create a pool that is big enough from the start.</param>
        /// <param name="fillPool">If true the pool will be filled when the pool is created,
        /// otherwise the pool will fill as chunks are loaded.</param>
        public ChunkPool(MapManager manager, int poolSize, bool fillPool = false) {
            _manager = manager;
            _chunkPool = new List<Chunk>(poolSize);
            if(!fillPool) return;
            for(var i=0;i<poolSize;i++){ _chunkPool.Add(Chunk.CreateNew(_manager));}
        }

         /// <summary>
        /// This method is used to get an available <see cref="Chunk"/> from the <see cref="ChunkPool"/>.
        /// </summary>
        /// <returns>An available <see cref="Chunk"/> that has been pulled from the <see cref="ChunkPool"/>.</returns>
        public Chunk GetAvailableChunk() {
            lock(_poolLock) { //makes sure that we do not return a chunk that is already in use
                foreach(var chunk in _chunkPool) {
                    if(chunk.IsInUse) continue;
                    chunk.PullFromPool();
                    return chunk;
                }
                //if we reach here we need to add a new chunk to the pool
                var newChunk = Chunk.CreateNew(_manager);
                newChunk.PullFromPool();
                _chunkPool.Add(newChunk);
                return newChunk;
            }
        }
        
    }
}