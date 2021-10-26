using UnityEngine;

namespace Amilious.Core.Serializable {
    
    /// <summary>
    /// This class is used to serialize a Vector3Int's values.
    /// </summary>
    [System.Serializable]
    public class SerializableVector3Int {
        
        //private variables
        private readonly int _x;
        private readonly int _y;
        private readonly int _z;

        /// <summary>
        /// This property is used to get a Vector3Int form this SerializedVector3Int.
        /// </summary>
        public Vector3Int Vector3Int => new Vector3Int(_x, _y, _z);

        /// <summary>
        /// This constructor is used to create a SerializableVector3Int from
        /// the given Vector3Int.
        /// </summary>
        /// <param name="vector3">The Vector3Int that you want to make serializable.</param>
        public SerializableVector3Int(Vector3Int vector3) {
            _x = vector3.x;
            _y = vector3.y;
            _z = vector3.z;
        }
        
    }
}