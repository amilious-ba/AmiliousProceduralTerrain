using Amilious.ProceduralTerrain.Map;
using UnityEngine;

namespace Amilious.ProceduralTerrain {
    
    /// <summary>
    /// This class is used to store delegates used in the procedural terrain.
    /// </summary>
    public static class Delegates {

        /// <summary>
        /// This delegate is used for the onVisibilityChanged Chunk event.
        /// </summary>
        /// <param name="chunkId">This value contains the id of the chunk whose visibility changed.</param>
        /// <param name="visible">This value contains true if the chunk is now visible, otherwise false.</param>
        public delegate void OnChunkVisibilityChangedDelegate(Vector2Int chunkId, bool visible);

        /// <summary>
        /// This delegate is used for the on onLodChanged Chunk event.
        /// </summary>
        /// <param name="chunkId">This value contains the id of the chunk whose lod was updated.</param>
        /// <param name="oldLod">This value contains the lod that was being used before.</param>
        /// <param name="newLod">This value contains the lod that is being used now.</param>
        public delegate void OnLodChangedDelegate(Vector2Int chunkId, int oldLod, int newLod);

        /// <summary>
        /// This delegate is used for the onChunkLoaded Chunk event.
        /// </summary>
        /// <param name="chunkId">This value contains the id of the chunk that was loaded.</param>
        public delegate void OnChunkLoadedDelegate(Vector2Int chunkId);

        /// <summary>
        /// This delegate is used for the onChunkSaved Chunk event.
        /// </summary>
        /// <param name="chunkId">This value contains the id of the chunk that was saved.</param>
        public delegate void OnChunkSavedDelegate(Vector2Int chunkId);
        
        /// <summary>
        /// This delegate is used for the onViewerChangedChunk MapManager event.
        /// <param name="viewer">The viewer that changed chunks.</param>
        /// <param name="oldChunkId">The chunk the viewer left.</param>
        /// <param name="newChunkId">The viewer's current chunk.</param>
        /// </summary>
        public delegate void OnViewerChangedChunkDelegate(Transform viewer, Vector2Int oldChunkId, Vector2Int newChunkId);
        
        /// <summary>
        /// This delegate is used for the onChunksUpdated MapManager event.
        /// <param name="chunkPool">The <see cref="ChunkPool"/> that was used for the update.</param>
        /// <param name="ms">The time the update took in milliseconds.</param>
        /// </summary>
        public delegate void OnChunksUpdatedDelegate(ChunkPool chunkPool, long ms);

    }
}