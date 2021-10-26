using UnityEngine;

namespace Amilious.Core.Serializable {
    
    /// <summary>
    /// This class is used to serialize a Vector3's values.
    /// </summary>
    [System.Serializable]
    public class SerializableVector3 {
        
        //private variables
        private readonly float _x;
        private readonly float _y;
        private readonly float _z;

        /// <summary>
        /// This property is used to get a Vector3 form this SerializedVector3.
        /// </summary>
        public Vector3 Vector3 => new Vector3(_x, _y, _z);

        /// <summary>
        /// This constructor is used to create a SerializableVector3 from
        /// the given Vector3.
        /// </summary>
        /// <param name="vector3">The Vector3 that you want to make serializable.</param>
        public SerializableVector3(Vector3 vector3) {
            _x = vector3.x;
            _y = vector3.y;
            _z = vector3.z;
        }
        
    }
    
}