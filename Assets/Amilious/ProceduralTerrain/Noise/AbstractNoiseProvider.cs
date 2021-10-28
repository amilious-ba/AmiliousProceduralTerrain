using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Textures;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Noise {
    
    /// <summary>
    /// This class can be extended to used with the terrain generation system.
    /// </summary>
    public abstract class AbstractNoiseProvider : ScriptableObject {

        /// <summary>
        /// This method is used to generate a filled noise map.
        /// </summary>
        /// <param name="size">The length and with of the map that is
        /// being requested.</param>
        /// <param name="hashedSeed">A unique int or hashed string seed used
        /// for the generation.</param>
        /// <param name="position">The center position of the noise that you
        /// want to generate.</param>
        /// <returns>A generated noise map  with the values between negative
        /// one and one.</returns>
        public abstract NoiseMap Generate(int size, int hashedSeed, Vector2? position = null);
        
        /// <summary>
        /// This method is used to get a single noise value at the given position.
        /// </summary>
        /// <param name="x">The x value that you want to evaluate.</param>
        /// <param name="z">The z value that you want to evaluate.</param>
        /// <param name="hashedSeed">A unique int or hashed string seed used
        /// for the generation.</param>
        /// <returns>The value at the given a and z position. This should be
        /// a value between one and zero.</returns>
        public abstract float NoiseAtPoint(float x, float z, int hashedSeed);

        public abstract ColorMap PreviewColors { get; }

        public abstract void SetComputeShaderValues(ComputeShader computeShader, char prefix, int seed);

    }
}