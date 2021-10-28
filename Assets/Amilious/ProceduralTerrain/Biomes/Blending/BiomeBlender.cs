using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//https://github.com/KdotJPG/Scattered-Biome-Blender
//https://noiseposti.ng/posts/2021-03-13-Fast-Biome-Blending-Without-Squareness.html

namespace Amilious.ProceduralTerrain.Biomes.Blending {
    
    /// <summary>
    /// This class is used to blend biomes.
    /// </summary>
    public class BiomeBlender {

        private readonly float _halfChunkWidth;
        private readonly int _chunkSize;
        private readonly float _blendRadiusSq;
        private readonly ChunkPointGatherer _gatherer;
        private readonly bool _useComputeShader;
        
        /// <summary>
        /// This constructor is used to create a new biome blender.
        /// </summary>
        /// <param name="samplingFrequency"></param>
        /// <param name="blendRadiusPadding"></param>
        /// <param name="chunkSize"></param>
        public BiomeBlender(float samplingFrequency, float blendRadiusPadding, int chunkSize, bool useComputeShader) {
            _chunkSize = chunkSize;
            _halfChunkWidth = chunkSize / 2f;
            var blendRadius = blendRadiusPadding + GetInternalMinBlendRadiusForFrequency(samplingFrequency);
            _blendRadiusSq = blendRadius * blendRadius;
            _gatherer = new ChunkPointGatherer(samplingFrequency, blendRadius, chunkSize);
            _useComputeShader = useComputeShader;
            var blendRadiusBoundArrayCenter = (int)Mathf.Ceil(blendRadius) - 1;
            var blendRadiusBound = new float[blendRadiusBoundArrayCenter * 2 + 1];
            for (var i = 0; i < blendRadiusBound.Length; i++) {
                var dx = i - blendRadiusBoundArrayCenter;
                var maxDxBeforeTruncate = Mathf.Abs(dx) + 1;
                blendRadiusBound[i] = Mathf.Sqrt(_blendRadiusSq - maxDxBeforeTruncate);
            }
            
        }

        /// <summary>
        /// This method will return the biome weights for each point for the chunk.
        /// </summary>
        /// <param name="seed">The hashed seed.</param>
        /// <param name="position">The chunks position.</param>
        /// <param name="evaluator">This is the object that will return the biome id based
        /// on x, z, and the hashed seed.</param>
        /// <param name="positionIsCenter">This value should be true if the chunk's position
        /// is centered, otherwise false.</param>
        /// <returns>A dictionary of this chunk's biome weights.</returns>
        public Dictionary<int, float[,]> GetChunkBiomeWeights(int seed, Vector2 position,
            IBiomeEvaluator evaluator,  bool positionIsCenter = true) {
            //we need to negate the z because the unfilteredPointGather
            //does not correctly apply the position offset
            position.y *= -1;
            // Get the list of data points in range.
            
            //TODO: do this in compute shader
            var points = positionIsCenter? 
                _gatherer.GetPointsFromChunkCenter(seed, position):
                    _gatherer.GetPointsFromChunkBase(seed,position);

            
            // Evaluate and aggregate all biomes to be blended in this chunk.
            var weightMap = new Dictionary<int, float[,]>();
            if(_useComputeShader) {
                var biomes = evaluator.GetBiomesFromComputeShader(points, seed);
                foreach(var biome in biomes){
                    weightMap.Add(biome, new float[_chunkSize, _chunkSize]);
                }
            } else {
                foreach(var point in points) {
                    // Get the biome for this data point from the callback.
                    var biome = evaluator.GetBiomeAt(point.X, point.Z, seed);
                    //add the biome if it does not exist
                    if(!weightMap.ContainsKey(biome)) weightMap.Add(biome, new float[_chunkSize, _chunkSize]);
                    point.PointData = biome;
                }
            }

            // If there is only one biome in range here, we can skip the actual blending step.
            if(weightMap.Keys.Count == 1) {
                var key = weightMap.Keys.First();
                for(var t=0;t<_chunkSize;t++) for(var v = 0; v < _chunkSize; v++) weightMap[key][t, v] = 1f;
                return weightMap;
            }
            
            //blend the biomes
            float z = position.y, x = position.x;
            if(positionIsCenter) {
                //fix the position if the provided position is centered
                x -= _halfChunkWidth;
                z -= _halfChunkWidth;
            }
            var centerX = position.x;
            var centerY = -position.y;
            if(positionIsCenter) {
                centerX -= _halfChunkWidth;
                centerY -= _halfChunkWidth;
            }
            
            var xStart = x;
            for(var iz = 0; iz < _chunkSize; iz++) {
                for(var ix = 0; ix < _chunkSize; ix++) {
                    // Consider each data point to see if it's inside the radius for this column.
                    var columnTotalWeight = 0.0f;
                    foreach(var point in points) {
                        var dx = x - point.X;
                        var dz = z - point.Z;
                        var distSq = dx * dx + dz * dz;
                        // If it's inside the radius...
                        if(!(distSq < _blendRadiusSq)) continue;
                        // Relative weight = [r^2 - (x^2 + z^2)]^2
                        var weight = _blendRadiusSq - distSq;
                        weight *= weight;
                        weightMap[point.PointData][ix, iz] += weight;
                        columnTotalWeight += weight;
                    }
                    // Normalize so all weights in a column add up to 1.
                    var inverseTotalWeight = 1.0f / columnTotalWeight;
                    foreach(var key in weightMap.Keys) weightMap[key][ix, iz] *= inverseTotalWeight;
                    x++;
                }
                x = xStart;
                z++;
            }
            //return the biome data.
            return weightMap;
        }
        
        /// <summary>
        /// This method is used to get the internal min blend radius for the given frequency.
        /// </summary>
        /// <param name="samplingFrequency">The sampling frequency.</param>
        /// <returns>The internal min blend radius for the given frequency.</returns>
        public static float GetInternalMinBlendRadiusForFrequency(float samplingFrequency) {
            return UnfilteredPointGatherer.MaxGridScaleDistanceToClosestPoint / samplingFrequency;
        }
        
    }
}