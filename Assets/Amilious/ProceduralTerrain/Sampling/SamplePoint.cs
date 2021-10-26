using Amilious.ProceduralTerrain.Biomes.Blending;

namespace Amilious.ProceduralTerrain.Sampling {
    
    /// <summary>
    /// This class is used as a reference point that
    /// is used to blend the biomes using the <see cref="BiomeBlender"/>.
    /// </summary>
    public class SamplePoint<T> {

        /// <summary>
        /// This constructor is used to create a new <see cref="SamplePoint{T}"/>
        /// using the given values.
        /// </summary>
        /// <param name="x">The sample x position.</param>
        /// <param name="z">The sample y position.</param>
        /// <param name="hash">The remaining hash used in generation.</param>
        public SamplePoint(float x, float z, float hash) {
            X = x;
            Z = z;
            Hash = hash;
        }
    
        /// <summary>
        /// This property is used to get the remaining hash used in
        /// the <see cref="UnfilteredPointGatherer"/>.
        /// </summary>
        public float Hash { get; private set; }
        
        /// <summary>
        /// This property is used to get the <see cref="SamplePoint{T}"/>'s
        /// X position.
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// This property is used to get the <see cref="SamplePoint{T}"/>'s
        /// Z position.
        /// </summary>
        public float Z { get; set; }
        
        /// <summary>
        /// This property is used to get and set reference data to this <see cref="SamplePoint{T}"/>
        /// </summary>
        public T PointData { get; set; }
        
    }
}