using UnityEngine;

namespace Amilious.Core.Serializable {
    
    /// <summary>
    /// This class is used to serialize a Vector2's values.
    /// </summary>
    [System.Serializable]
    public class SerializableVector2 {
        
        //private variables
        private readonly float _x;
        private readonly float _y;

        /// <summary>
        /// This property is used to get a Vector2 form this SerializedVector2.
        /// </summary>
        public Vector2 Vector2 => new Vector2(_x, _y);

        /// <summary>
        /// This constructor is used to create a SerializableVector2 from
        /// the given Vector2.
        /// </summary>
        /// <param name="vector2">The Vector2 that you want to make serializable.</param>
        public SerializableVector2(Vector2 vector2) {
            _x = vector2.x;
            _y = vector2.y;
        }
        
    }
    
}