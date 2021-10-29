namespace Amilious.ProceduralTerrain.Map {
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