namespace Amilious.ProceduralTerrain.Mesh.Enums {
    
    /// <summary>
    /// This enum is used to represent the different levels of detail.
    /// </summary>
    public enum LevelsOfDetail {
        
        /// <summary>
        /// This level of detail will draw the mesh with all of the main vertices.
        /// </summary>
        Max = 1,
        
        /// <summary>
        /// This level of detail will draw the mesh with half of the main vertices.
        /// </summary>
        High = 2,
        
        /// <summary>
        /// This level of detail will draw the mesh with a quarter of the main vertices.
        /// </summary>
        Medium = 4,
        
        /// <summary>
        /// This level of detail will draw the mesh with an eight of the main vertices.
        /// </summary>
        Low = 8
    }
}