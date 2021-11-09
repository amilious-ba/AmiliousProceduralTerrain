namespace Amilious.ProceduralTerrain.Biomes {
    
    /// <summary>
    /// This enum is used to select the biome preview type.
    /// </summary>
    public enum BiomePreviewType{
        
        /// <summary>
        /// This preview type will be used to see the heat map that
        /// will be used to generate a biome map.
        /// </summary>
        HeatMap = 0, 
        
        /// <summary>
        /// This preview type will be used to see the moisture map
        /// that will be used to generate a biome map.
        /// </summary>
        MoistureMap = 1, 
        
        /// <summary>
        /// This preview type will be used to see the biome map
        /// that will be generated from the other maps.
        /// </summary>
        BiomeMap = 2,
        
        /// <summary>
        /// This preview type is used to see the blended biome map
        /// that will be generated from the other maps.
        /// </summary>
        BlendedBiomeMap = 3

    }
    
}