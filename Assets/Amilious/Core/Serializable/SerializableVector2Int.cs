using UnityEngine;

namespace Amilious.Core.Serializable {
    
    /// <summary>
    /// This class is used to serialize a Vector2Int's values.
    /// </summary>
    [System.Serializable]
    public class SerializableVector2Int {
        
        //private variables
        private readonly int _x;
        private readonly int _y;

        /// <summary>
        /// This property is used to get a Vector2Int form this SerializedVector2Int.
        /// </summary>
        public Vector2Int Vector2Int => new Vector2Int(_x, _y);

        /// <summary>
        /// This constructor is used to create a SerializableVector2Int from
        /// the given Vector2Int.
        /// </summary>
        /// <param name="vector2Int">The Vector2Int that you want to make serializable.</param>
        public SerializableVector2Int(Vector2Int vector2Int) {
            _x = vector2Int.x;
            _y = vector2Int.y;
        }
        
    }
}