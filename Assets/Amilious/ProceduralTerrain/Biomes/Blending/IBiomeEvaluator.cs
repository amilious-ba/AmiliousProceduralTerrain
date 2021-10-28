using System.Collections.Generic;
using Amilious.ProceduralTerrain.Sampling;

namespace Amilious.ProceduralTerrain.Biomes.Blending {
    
    /// <summary>
    /// This interface is used by the biome blender to
    /// get the biome id at the given position.
    /// </summary>
    public interface IBiomeEvaluator {
        
        /// <summary>
        /// This method is used to get the biome
        /// at the given position using the given
        /// seed.
        /// </summary>
        /// <param name="x">The x position that you
        /// want to check.</param>
        /// <param name="z">The z position that you
        /// want to check.</param>
        /// <param name="hashedSeed">The hashed seed
        /// that you want to use to get the biome at
        /// the given position.</param>
        /// <returns>The biome id at the given position
        /// using the given hashed seed.</returns>
        int GetBiomeAt(float x, float z, int hashedSeed);

        bool UsingComputeShader { get; }

        List<int> GetBiomesFromComputeShader(List<SamplePoint<int>> samplePoints, int seed);
    }
}