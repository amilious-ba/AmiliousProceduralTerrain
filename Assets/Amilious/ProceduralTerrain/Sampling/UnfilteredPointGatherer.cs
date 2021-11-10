using UnityEngine;
using Amilious.Random;
using System.Threading;
using System.Collections.Generic;

namespace Amilious.ProceduralTerrain.Sampling {

    public class UnfilteredPointGatherer {
        
        // For handling a (jittered) hex grid
        private static readonly float SqrtHalf = Mathf.Sqrt(1.0f / 2.0f);
        private static readonly float TriangleEdgeLength = Mathf.Sqrt(2.0f / 3.0f);
        private static readonly float TriangleHeight = SqrtHalf;
        private static readonly float InverseTriangleHeight = SqrtHalf * 2;
        // ReSharper disable once IdentifierTypo
        private static readonly float TriangleCircumRadius = TriangleHeight * (2.0f / 3.0f);
        private static readonly float JitterAmount = TriangleHeight;
        public static readonly float MaxGridScaleDistanceToClosestPoint = JitterAmount + TriangleCircumRadius;

        // Primes for jitter hash.
        private const int PRIME_X = 7691;
        private const int PRIME_Z = 30869;

        // Jitter in JITTER_VECTOR_COUNT_MULTIPLIER*12 directions, symmetric about the hex grid.
        // cos(t)=sin(t+const) where const=(1/4)*2pi, and N*12 is a multiple of 4, so we can overlap arrays.
        // Repeat the first in every set of three due to how the pseudo-modulo indexer works.
        // I started out with the idea of letting JITTER_VECTOR_COUNT_MULTIPLIER_POWER be configurable,
        // but it may need bit more work to ensure there are enough bits in the selector.
        private const int JITTER_VECTOR_COUNT_MULTIPLIER_POWER = 1;
        private const int JITTER_VECTOR_COUNT_MULTIPLIER = 1 << JITTER_VECTOR_COUNT_MULTIPLIER_POWER;
        private const int N_VECTORS = JITTER_VECTOR_COUNT_MULTIPLIER * 12;
        private const int N_VECTORS_WITH_REPETITION = N_VECTORS * 4 / 3;
        private const int VECTOR_INDEX_MASK = N_VECTORS_WITH_REPETITION - 1;
        private const int JITTER_SINCOS_OFFSET = JITTER_VECTOR_COUNT_MULTIPLIER * 4;
        private static readonly float[] JitterSincos;

        private static readonly int SinCosArraySize;
        private static readonly float SinCosOffsetFactor;

        static UnfilteredPointGatherer() {
            SinCosArraySize = N_VECTORS_WITH_REPETITION * 5 / 4;
            SinCosOffsetFactor = (1.0f / JITTER_VECTOR_COUNT_MULTIPLIER);
            JitterSincos = new float[SinCosArraySize];
            for(int i = 0, j = 0; i < N_VECTORS; i++) {
                JitterSincos[j] = Mathf.Sin((i + SinCosOffsetFactor) * 
                    ((2.0f * Mathf.PI) / N_VECTORS)) * JitterAmount;
                j++;

                // Every time you start a new set, repeat the first entry.
                // This is because the pseudo-modulo formula,
                // which aims for an even selection over 24 values,
                // reallocates the distribution over every four entries
                // from 25%,25%,25%,25% to a,b,33%,33%, where a+b=33%.
                // The particular one used here does 0%,33%,33%,33%.
                if((j & 3) != 1) continue;
                JitterSincos[j] = JitterSincos[j - 1];
                j++;
            }

            for(var j = N_VECTORS_WITH_REPETITION; j < SinCosArraySize; j++) {
                JitterSincos[j] = JitterSincos[j - N_VECTORS_WITH_REPETITION];
            }
        }

        private readonly float _frequency, _inverseFrequency;
        private readonly LatticePoint[] _pointsToSearch;

        public UnfilteredPointGatherer(float frequency, float maxPointContributionRadius) {
            this._frequency = frequency;
            this._inverseFrequency = 1.0f / frequency;

            // How far out in the jittered hex grid we need to look for points.
            // Assumes the jitter can go any angle, which should only very occasionally
            // cause us to search one more layer out than we need.
            var maxContributingDistance = maxPointContributionRadius * frequency
                                          + MaxGridScaleDistanceToClosestPoint;
            var maxContributingDistanceSq = maxContributingDistance * maxContributingDistance;
            var latticeSearchRadius = maxContributingDistance * InverseTriangleHeight;

            // Start at the central point, and keep traversing bigger hexagonal layers outward.
            // Exclude almost all points which can't possibly be jittered into range.
            // The "almost" is again because we assume any jitter angle is possible,
            // when in fact we only use a small set of uniformly distributed angles.
            var pointsToSearchList = new List<LatticePoint>();
            pointsToSearchList.Add(new LatticePoint(0, 0));
            for(var i = 1; i < latticeSearchRadius; i++) {
                var xsv = i;
                var zsv = 0;

                while(zsv < i) {
                    var point = new LatticePoint(xsv, zsv);
                    if(point.xv * point.xv + point.zv * point.zv < maxContributingDistanceSq)
                        pointsToSearchList.Add(point);
                    zsv++;
                }

                while(xsv > 0) {
                    var point = new LatticePoint(xsv, zsv);
                    if(point.xv * point.xv + point.zv * point.zv < maxContributingDistanceSq)
                        pointsToSearchList.Add(point);
                    xsv--;
                }

                while(xsv > -i) {
                    var point = new LatticePoint(xsv, zsv);
                    if(point.xv * point.xv + point.zv * point.zv < maxContributingDistanceSq)
                        pointsToSearchList.Add(point);
                    xsv--;
                    zsv--;
                }

                while(zsv > -i) {
                    var point = new LatticePoint(xsv, zsv);
                    if(point.xv * point.xv + point.zv * point.zv < maxContributingDistanceSq)
                        pointsToSearchList.Add(point);
                    zsv--;
                }

                while(xsv < 0) {
                    var point = new LatticePoint(xsv, zsv);
                    if(point.xv * point.xv + point.zv * point.zv < maxContributingDistanceSq)
                        pointsToSearchList.Add(point);
                    xsv++;
                }

                while(zsv < 0) {
                    var point = new LatticePoint(xsv, zsv);
                    if(point.xv * point.xv + point.zv * point.zv < maxContributingDistanceSq)
                        pointsToSearchList.Add(point);
                    xsv++;
                    zsv++;
                }
            }

            _pointsToSearch = pointsToSearchList.ToArray();
        }
        
        public List<SamplePoint<T>> GetPoints<T>(Seed seedValue, float x, float z, CancellationToken cancellationToken) {
            x *= _frequency;
            z *= _frequency;
            var seed = seedValue.LongValue;

            // Simplex 2D Skew.
            var s = (x + z) * 0.366025403784439f;
            float xs = x + s, zs = z + s;

            // Base vertex of compressed square.
            var xsb = (int)xs;
            if(xs < xsb) xsb -= 1;
            var zsb = (int)zs;
            if(zs < zsb) zsb -= 1;
            float xsi = xs - xsb, zsi = zs - zsb;

            // Find closest vertex on triangle lattice.
            var p = 2 * xsi - zsi;
            var q = 2 * zsi - xsi;
            var r = xsi + zsi;
            if(r > 1) {
                if(p < 0) zsb += 1;
                else if(q < 0) xsb += 1;
                else { xsb += 1; zsb += 1; }
            }else {
                if(p > 1) xsb += 1;
                else if(q > 1) zsb += 1;
            }

            // Pre-multiply for hash.
            var xsbp = xsb * PRIME_X;
            var zsbp = zsb * PRIME_Z;

            // Unskewed coordinate of the closest triangle lattice vertex.
            // Everything will be relative to this.
            var bt = (xsb + zsb) * -0.211324865405187f;
            float xb = xsb + bt, zb = zsb + bt;

            // Loop through pregenerated array of all points which could be in range, relative to the closest.
            var worldPointsList = new List<SamplePoint<T>>(_pointsToSearch.Length);
            foreach(var point in _pointsToSearch) {
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // Prime multiplications for jitter hash
                var xsvp = xsbp + point.xsvp;
                var zsvp = zsbp + point.zsvp;

                // Compute the jitter hash
                var hash = xsvp ^ zsvp;
                hash = (((int)(seed & 0xFFFFFFFFL) ^ hash) * 668908897)
                       ^ (((int)(seed >> 32) ^ hash) * 35311);

                // Even selection within 0-24, using pseudo-modulo technique.
                var indexBase = (hash & 0x3FFFFFF) * 0x5555555;
                var index = (indexBase >> 26) & VECTOR_INDEX_MASK;
                var remainingHash = indexBase & 0x3FFFFFF; // The lower bits are still good as a normal hash.

                // Jittered point, not yet unscaled for frequency
                var scaledX = xb + point.xv + JitterSincos[index];
                var scaledZ = zb + point.zv + JitterSincos[index + JITTER_SINCOS_OFFSET];

                // Unscale the coordinate and add it to the list.
                // "Unfiltered" means that, even if the jitter took it out of range, we don't check for that.
                // It's up to the user to handle out-of-range points as if they weren't there.
                // This is so that a user can implement a more limiting check (e.g. confine to a chunk square),
                // without the added overhead of this less limiting check.
                // A possible alternate implementation of this could employ a callback function,
                // to avoid adding the points to the list in the first place.
                var worldPoint = new SamplePoint<T>(scaledX * _inverseFrequency,
                    scaledZ * _inverseFrequency, remainingHash);
                worldPointsList.Add(worldPoint);
            }

            return worldPointsList;
        }
        
       
        private class LatticePoint {
            public int xsvp, zsvp;
            public float xv, zv;

            public LatticePoint(int xsv, int zsv) {
                xsvp = xsv * PRIME_X;
                zsvp = zsv * PRIME_Z;
                var t = (xsv + zsv) * -0.211324865405187f;
                xv = xsv + t;
                zv = zsv + t;
            }
        }
    }
}