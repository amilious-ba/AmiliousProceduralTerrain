namespace Amilious.ProceduralTerrain.Map.Enums {
    
    /// <summary>
    /// This enum is used to represent the type of map.
    /// </summary>
    public enum MapType {
        
        /// <summary>
        /// When using this mode the terrain will be generated as the player
        /// moves around in the world.
        /// </summary>
        Endless,
        
        /// <summary>
        /// When using this mode the terrain will be generated the first time
        /// that a player enters the world.
        /// </summary>
        PreGenerated
    }
}