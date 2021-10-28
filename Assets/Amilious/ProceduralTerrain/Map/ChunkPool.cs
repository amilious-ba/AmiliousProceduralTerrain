using System.Collections.Generic;
using System.Linq;

namespace Amilious.ProceduralTerrain.Map {
    
    //this class will be used for pooling chunks
    public class ChunkPool {

        private readonly MapManager _manager;
        private readonly List<Chunk> _chunkPool;
        private readonly object _poolLock = new object();

        public ChunkPool(MapManager manager, int poolSize, bool fillPool = false) {
            _manager = manager;
            _chunkPool = new List<Chunk>(poolSize);
            if(!fillPool) return;
            for(var i=0;i<poolSize;i++){ _chunkPool.Add(Chunk.CreateNew(_manager));}
        }

        public Chunk GetAvailableChunk() {
            lock(_poolLock) { //makes sure that we do not return a chunk that is already in use
                foreach(var chunk in _chunkPool.Where(chunk => !chunk.IsInUse)) {
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