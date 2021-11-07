namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This enum is used to set the method for painting
    /// textures on the <see cref="ChunkMesh"/>.
    /// </summary>
    public enum TerrainPaintingMode {
        
        /// <summary>
        /// This value indicates that only the material should be applied.
        /// </summary>
        Material = 0,
        
        /// <summary>
        /// This value indicates that the meshes should be colored based on their biome.
        /// </summary>
        BiomeColors = 1, 
        
        /// <summary>
        /// This value indicates that the meshes should be colored based on their blended biome.
        /// </summary>
        BlendedBiomeColors = 2,
        
        /// <summary>
        /// This value indicates that the meshes should be colored based on their noise preview colors.
        /// </summary>
        NoisePreviewColors = 3,
        
        /// <summary>
        /// This value indicates that the meshes should be colored based on the biome blended noise preivew colors.
        /// </summary>
        BlendedNoisePreviewColors = 4,
    }
}