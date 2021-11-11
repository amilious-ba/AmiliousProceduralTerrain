using UnityEngine;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This struct is used to represent a chunk range.
    /// </summary>
    public readonly struct ChunkRange {

        public static ChunkRange maxRange = new ChunkRange(Vector2Int.zero, int.MaxValue);
        public static ChunkRange minRange = new ChunkRange(Vector2Int.zero, 0);
        
        /// <summary>
        /// This property contains the <see cref="ChunkRange"/>'s min x and y values.
        /// </summary>
        public Vector2Int MinValues { get;}
        
        /// <summary>
        /// This property contains the <see cref="ChunkRange"/>'s max x and y values.
        /// </summary>
        public Vector2Int MaxValues { get;}
        
        /// <summary>
        /// This constructor is used to create a new <see cref="ChunkRange"/>.
        /// </summary>
        /// <param name="centerPoint">This is the center point of the range.</param>
        /// <param name="singleDirectionLength">This is the distance from the center
        /// in every direction that should be included in the range.</param>
        public ChunkRange(Vector2Int centerPoint, int singleDirectionLength) {
            var size = Vector2Int.one * singleDirectionLength;
            MinValues = centerPoint - size;
            MaxValues = centerPoint + size;
        }

        /// <summary>
        /// This constructor is used to create a new <see cref="ChunkRange"/>.
        /// </summary>
        /// <param name="point1">A corner point of the range.</param>
        /// <param name="point2">The corner point diagonal from point1.</param>
        public ChunkRange(Vector2Int point1, Vector2Int point2) {
            MinValues = Vector2Int.Min(point1,point2);
            MaxValues = Vector2Int.Max(point1,point2);
        }

        /// <summary>
        /// This method is used to check the given chunkId is present in
        /// this range.
        /// </summary>
        /// <param name="chunkId">The chunk id that you want to see if is in the range.</param>
        /// <returns>True if the chunk with the given chunkId is in the range, otherwise
        /// returns false.</returns>
        public bool IsInRange(Vector2Int chunkId) {
            if(chunkId.x < MinValues.x || chunkId.x > MaxValues.x) return false;
            return chunkId.y >= MinValues.y && chunkId.y <= MaxValues.y;
        }

    }
    
}