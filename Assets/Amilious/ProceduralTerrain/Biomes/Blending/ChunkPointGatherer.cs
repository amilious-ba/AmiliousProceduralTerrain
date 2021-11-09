using System.Collections.Generic;
using System.Threading;
using Amilious.ProceduralTerrain.Sampling;
using Amilious.Random;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Biomes.Blending {
    
    /// <summary>
    /// This class is used to gather points for the
    /// <see cref="BiomeBlender"/>.
    /// </summary>
    public class ChunkPointGatherer {

        private static readonly float ChunkRadiusRatio = Mathf.Sqrt(1.0f / 2.0f);
        private readonly float _halfChunkWidth;
        private readonly int _chunkSize;
        private readonly float _maxPointContributionRadius;
        private readonly float _maxPointContributionRadiusSq;
        private readonly UnfilteredPointGatherer _unfilteredPointGatherer;
    
        /// <summary>
        /// This method is used to create a new <see cref="ChunkPointGatherer"/>.
        /// </summary>
        /// <param name="frequency">The sample frequency.</param>
        /// <param name="maxPointContributionRadius">The max point contribution radius.</param>
        /// <param name="chunkSize">This value should be both the width and the height
        /// of a chunk.</param>
        public ChunkPointGatherer(float frequency, float maxPointContributionRadius, int chunkSize) {
            _chunkSize = chunkSize;
            _halfChunkWidth = chunkSize / 2f;
            _maxPointContributionRadius = maxPointContributionRadius;
            _maxPointContributionRadiusSq = maxPointContributionRadius * maxPointContributionRadius;
            _unfilteredPointGatherer = new UnfilteredPointGatherer(frequency,
                    maxPointContributionRadius + chunkSize * ChunkRadiusRatio);
        }

        /// <summary>
        /// This method is used to get the <see cref="SamplePoint{T}"/>s for the
        /// <see cref="BiomeBlender"/> using the chunks top left position.
        /// </summary>
        /// <param name="seed">The seed to use for the <see cref="SamplePoint{T}"/> generation.</param>
        /// <param name="topLeftPosition">The top left position of the chunk.</param>
        /// <param name="token">A cancellation token that can be used to cancel the process.</param>
        /// <returns>The <see cref="SamplePoint{T}"/>s that will be used in the
        /// <see cref="BiomeBlender"/>.</returns>
        public List<SamplePoint<int>> GetPointsFromChunkBase(Seed seed, Vector2 topLeftPosition,
            CancellationToken token) {
            var centerPosition = topLeftPosition;
            centerPosition.x+= _halfChunkWidth;
            centerPosition.y+= _halfChunkWidth;
            return GetPointsFromChunkCenter(seed, centerPosition, token);
        }

        /// <summary>
        /// This method is used to get the <see cref="SamplePoint{T}"/>s for the
        /// <see cref="BiomeBlender"/> using the chunks center position.
        /// </summary>
        /// <param name="seed">The seed to use for the <see cref="SamplePoint{T}"/> generation.</param>
        /// <param name="centerPosition">The top left position of the chunk.</param>
        /// <param name="token">A cancellation token that can be used to cancel the process.</param>
        /// <returns>The <see cref="SamplePoint{T}"/>s that will be used in the
        /// <see cref="BiomeBlender"/>.</returns>
        public List<SamplePoint<int>> GetPointsFromChunkCenter(Seed seed, Vector2 centerPosition,
            CancellationToken token) {
            var worldPoints =
                    _unfilteredPointGatherer.GetPoints<int>(seed, centerPosition.x, centerPosition.y,token);
            for (var i = 0; i < worldPoints.Count; i++) {
                var point = worldPoints[i];
                // Check if point contribution radius lies outside any coordinate in the chunk
                var axisCheckValueX = Mathf.Abs(point.X - centerPosition.x) - _halfChunkWidth;
                var axisCheckValueZ = Mathf.Abs(point.Z - centerPosition.y) - _halfChunkWidth;
                if(!(axisCheckValueX >= _maxPointContributionRadius) && 
                   !(axisCheckValueZ >= _maxPointContributionRadius) && 
                   (!(axisCheckValueX > 0) || !(axisCheckValueZ > 0) || 
                   !(axisCheckValueX * axisCheckValueX + axisCheckValueZ * axisCheckValueZ >=
                   _maxPointContributionRadiusSq))) continue;
                // If so, remove it.
                var lastIndex = worldPoints.Count - 1;
                worldPoints[i] = worldPoints[lastIndex];
                worldPoints.RemoveAt(lastIndex);
                i--;
                token.ThrowIfCancellationRequested();
            }
            
            return worldPoints;
        }

    }
    
}