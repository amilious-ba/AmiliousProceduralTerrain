using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace Amilious.ProceduralTerrain.Map {
    
    //this class will be used to store map data for a region.

    public class MapRegion {

        public RegionSize RegionSize { get; }
        public Vector2Int RegionCoordinate { get; }
        
        public MapRegion(RegionSize regionSize, Vector2Int regionCoord) {
            RegionSize = regionSize;
            RegionCoordinate = regionCoord;
        }

        public bool ContainsChunk(Vector2Int chunkCoord) {
            return IsChunkWithin(RegionSize, RegionCoordinate, chunkCoord);
        }

        public static bool IsChunkWithin(RegionSize regionSize, Vector2Int regionCoord, Vector2Int chunkCoord) {
            return ChunkToRegion(regionSize, chunkCoord) == regionCoord;
        }

        public static Vector2Int ChunkToRegion(RegionSize regionSize, Vector2Int chunkCoord) {
            return (chunkCoord - Vector2Int.one * ((int)regionSize / 2 - 1)) / (int)regionSize;
        }

        public static Vector2Int[] RequiredRegionsToDraw(RegionSize regionSize, Vector2Int chunkCoord) {
            var regions = new List<Vector2Int>();
            var tlOffset = TopAndLeftOffset(regionSize);
            var brOffset = BottomAndRightOffset(regionSize);
            var mainRegion = ChunkToRegion(regionSize, chunkCoord);
            regions.Add(mainRegion);
            if(chunkCoord.x==mainRegion.x-tlOffset) 
                regions.Add(new Vector2Int(mainRegion.x-1,mainRegion.y));
            if(chunkCoord.x==mainRegion.x+brOffset)
                regions.Add(new Vector2Int(mainRegion.x+1,mainRegion.y));
            if(chunkCoord.y==mainRegion.y-tlOffset) 
                regions.Add(new Vector2Int(mainRegion.x,mainRegion.y-1));
            if(chunkCoord.y==mainRegion.y+brOffset)
                regions.Add(new Vector2Int(mainRegion.x,mainRegion.y+1));
            return regions.ToArray();
        }

        public static int TopAndLeftOffset(RegionSize regionSize) {
            return (int)regionSize / 2 - 1;
        }

        public static int BottomAndRightOffset(RegionSize regionSize) {
            return (int)regionSize / 2;
        }
        
    }
    
}