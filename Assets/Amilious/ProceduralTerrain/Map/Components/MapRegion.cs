using System.Collections.Generic;
using Amilious.ProceduralTerrain.Map.Enums;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Map.Components {
    
    //this class will be used to store map data for a region.
    public class MapRegion : IMapComponent<MapRegion> {

        private MapManager _mapManager;
        private MapPool<MapRegion> _mapPool;

        #region Properties
        
        /// <summary>
        /// This property is used to get the size of a region.
        /// </summary>
        public RegionSize RegionSize { get; }
        
        /// <summary>
        /// The regions id.
        /// </summary>
        public Vector2Int Id { get; private set; }
        
        public bool HasProcessedRelease { get; }
        
        public bool Active { get; }
        
        #endregion
        
        #region Constructor
        
        
        public MapRegion(MapManager mapManager, MapPool<MapRegion> mapPool) {
            _mapManager = mapManager;
            _mapPool = mapPool;
            RegionSize = mapManager.MeshSettings.RegionSize;
        }

        /// <summary>
        /// This constructor is only to be used by the <see cref="MapPool{T}"/> class.  It should not
        /// be used anywhere else.
        /// </summary>
        public MapRegion() {}

        #endregion
        
        /// <summary>
        /// This method is used to check if a chunk exists in this region.
        /// </summary>
        /// <param name="chunkId"></param>
        /// <returns></returns>
        public virtual bool ContainsChunk(Vector2Int chunkId) {
            return IsChunkWithin(RegionSize, Id, chunkId);
        }

        /// <summary>
        /// This method is used to check exists within the given region.
        /// </summary>
        /// <param name="regionSize">The size of the region.</param>
        /// <param name="regionId">The region id.</param>
        /// <param name="chunkId">The chunk id.</param>
        /// <returns>True if the chunk is within the region, otherwise false.</returns>
        public static bool IsChunkWithin(RegionSize regionSize, Vector2Int regionId, Vector2Int chunkId) {
            return ChunkToRegion(regionSize, chunkId) == regionId;
        }

        /// <summary>
        /// This method is used to get the region from the chunk id.
        /// </summary>
        /// <param name="regionSize">This size of the regions.</param>
        /// <param name="chunkId">The chunk id you want to get the region for.</param>
        /// <returns>The region id for the given chunk id.</returns>
        public static Vector2Int ChunkToRegion(RegionSize regionSize, Vector2Int chunkId) {
            return (chunkId - Vector2Int.one * ((int)regionSize / 2 - 1)) / (int)regionSize;
        }

        /// <summary>
        /// This method is used to get the regions that are required to draw the given chunk.
        /// </summary>
        /// <param name="regionSize">The size of the regions.</param>
        /// <param name="chunkId">The chunk you want to draw.</param>
        /// <returns>An array of the regions that need to be loaded to draw the given chunk.</returns>
        public static Vector2Int[] RequiredRegionsToDraw(RegionSize regionSize, Vector2Int chunkId) {
            var regions = new List<Vector2Int>();
            var tlOffset = TopAndLeftOffset(regionSize);
            var brOffset = BottomAndRightOffset(regionSize);
            var mainRegion = ChunkToRegion(regionSize, chunkId);
            regions.Add(mainRegion);
            if(chunkId.x==mainRegion.x-tlOffset) 
                regions.Add(new Vector2Int(mainRegion.x-1,mainRegion.y));
            if(chunkId.x==mainRegion.x+brOffset)
                regions.Add(new Vector2Int(mainRegion.x+1,mainRegion.y));
            if(chunkId.y==mainRegion.y-tlOffset) 
                regions.Add(new Vector2Int(mainRegion.x,mainRegion.y-1));
            if(chunkId.y==mainRegion.y+brOffset)
                regions.Add(new Vector2Int(mainRegion.x,mainRegion.y+1));
            return regions.ToArray();
        }

        /// <summary>
        /// This method is used to get the regions top and left offset based on the
        /// provided region size.
        /// </summary>
        /// <param name="regionSize">The size of the regions.</param>
        /// <returns>The top and left offset.</returns>
        public static int TopAndLeftOffset(RegionSize regionSize) {
            return (int)regionSize / 2 - 1;
        }

        /// <summary>
        /// This method is used to get the regions bottom and right offset based
        /// on the provided region size.
        /// </summary>
        /// <param name="regionSize">The size of the regions.</param>
        /// <returns>The bottom and right offset.</returns>
        public static int BottomAndRightOffset(RegionSize regionSize) {
            return (int)regionSize / 2;
        }

        public MapRegion CreateMapComponent(MapManager mapManager, MapPool<MapRegion> mapPool) {
            return new MapRegion(mapManager, mapPool);
        }

        public void PullFromPool(bool setActive = false) {
            
        }

        public void Setup(Vector2Int regionId) {
            Id = regionId;
        }

        public void ReleaseToPool() {
            
        }

    }
    
}