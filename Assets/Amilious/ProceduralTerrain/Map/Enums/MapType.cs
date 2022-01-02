namespace Amilious.ProceduralTerrain.Map.Enums {
    
    /// <summary>
    /// This enum is used to represent the type of map.
    /// </summary>
    public enum MapType {
        
        /// <summary>
        /// This value indicates that the map should be endlessly generated by chunks.
        /// </summary>
        EndlessChunkBased,
        
        /// <summary>
        /// This value indicates that the map should be endlessly generated by regions.
        /// </summary>
        EndlessRegionBased,
        
        /// <summary>
        /// This value indicates that the world has a set size and is generated by chunks.
        /// </summary>
        SetSizeChunkBased,
        
        /// <summary>
        /// This value indicates that the world has a set size and is generated by regions.
        /// </summary>
        SetSizeRegionBased
    }
    
    /// <summary>
    /// This class is used for adding utility methods to the MapType enum.
    /// </summary>
    public static class MapTypeExtenstion{

        /// <summary>
        /// This method is used to check if the given <see cref="MapType"/> is region based.
        /// </summary>
        /// <param name="mapType">The type that you want to check.</param>
        /// <returns>True if the given type is region based, otherwise returns false.</returns>
        public static bool RegionBased(this MapType mapType) {
            return mapType == MapType.EndlessRegionBased || mapType == MapType.SetSizeRegionBased;
        }

        /// <summary>
        /// This method is used to check if the given <see cref="MapType"/> is chunk based.
        /// </summary>
        /// <param name="mapType">The type that you want to check.</param>
        /// <returns>True if the give type is chunk based, otherwise returns false.</returns>
        public static bool ChunkBased(this MapType mapType) {
            return mapType == MapType.EndlessChunkBased || mapType == MapType.SetSizeChunkBased;
        }

        /// <summary>
        /// This method is used to check if the given <see cref="MapType"/> is endless.
        /// </summary>
        /// <param name="mapType">The type that you want to check.</param>
        /// <returns>True if the type is endless, otherwise returns false.</returns>
        public static bool IsEndless(this MapType mapType) {
            return mapType == MapType.EndlessChunkBased || mapType == MapType.EndlessRegionBased;
        }

        /// <summary>
        /// This method is used to check if the given <see cref="MapType"/> is a set size.
        /// </summary>
        /// <param name="mapType">The type that you want to check.</param>
        /// <returns>True if the type is a set size, otherwise returns false.</returns>
        public static bool IsSetSize(this MapType mapType) {
            return mapType == MapType.SetSizeChunkBased || mapType == MapType.SetSizeRegionBased;
        }
        
    }
}