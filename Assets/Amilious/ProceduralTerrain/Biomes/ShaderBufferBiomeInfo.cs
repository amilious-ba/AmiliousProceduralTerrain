using UnityEngine;

namespace Amilious.ProceduralTerrain.Biomes {

    /// <summary>
    /// This struct is used to pass and retrieve information from
    /// the compute biome shader.
    /// </summary>
    public struct ShaderBufferBiomeInfo {
        public Vector2 position;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnassignedField.Global
        public int moisture_index;
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnassignedField.Global
        public int heat_index;
    }
}