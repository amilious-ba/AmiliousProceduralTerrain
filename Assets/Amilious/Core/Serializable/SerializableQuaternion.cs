using UnityEngine;

namespace Amilious.Core.Serializable {
    
    /// <summary>
    /// This class is used to serialize a quaternion's values.
    /// </summary>
    [System.Serializable]
    public class SerializableQuaternion {
        
        //private variables
        private float _w;
        private float _x;
        private float _y;
        private float _z;

        /// <summary>
        /// This property is used to get a Quaternion form this SerializedQuaternion.
        /// </summary>
        public Quaternion Quaternion => new Quaternion(_x, _y, _z, _w);

        /// <summary>
        /// This constructor is used to create a SerializableQuaternion from
        /// the given Quaternion.
        /// </summary>
        /// <param name="quaternion">The Quaternion that you want to make serializable.</param>
        public SerializableQuaternion(Quaternion quaternion) {
            _w = quaternion.w;
            _x = quaternion.x;
            _y = quaternion.y;
            _z = quaternion.z;
        }
    }
}