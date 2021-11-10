using Amilious.Random;
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
        /// <param name="seed">The seed
        /// that you want to use to get the biome at
        /// the given position.</param>
        /// <returns>The biome id at the given position
        /// using the given hashed seed.</returns>
        int GetBiomeAt(float x, float z, Seed seed);

        /// <summary>
        /// This property is used to check if the biome evaluator should use
        /// a compute shader or not.
        /// </summary>
        bool UsingComputeShader { get; }

        /// <summary>
        /// This method is used to get the biomes at the given positions using a
        /// compute shader.
        /// </summary>
        /// <param name="samplePoints">The points that you want to get the biomes for.</param>
        /// <param name="seed">The seed you are using for generation.</param>
        /// <returns>The biomes at the given sample points.</returns>
        List<int> GetBiomesFromComputeShader(List<SamplePoint<int>> samplePoints, Seed seed);
    }
}