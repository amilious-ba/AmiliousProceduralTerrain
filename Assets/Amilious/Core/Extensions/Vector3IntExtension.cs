using Amilious.Core.Serializable;
using UnityEngine;

namespace Amilious.Core.Extensions {

    /// <summary>
    /// This class is used to add extensions to the Vector3Int class.
    /// </summary>
    public static class Vector3IntExtension {
        
        /// <summary>
        /// This method is used to convert a Vector3Int into a SerializableVector3Int.
        /// </summary>
        /// <param name="vector3Int">The Vector3Int that you want to convert.</param>
        /// <returns>A Serializable version of the given Vector3Int.</returns>
        public static SerializableVector3Int ToSerializable(this Vector3Int vector3Int) {
            return new SerializableVector3Int(vector3Int);
        }
        
    }
}