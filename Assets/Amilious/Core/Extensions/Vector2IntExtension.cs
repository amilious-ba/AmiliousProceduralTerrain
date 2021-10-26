using Amilious.Core.Serializable;
using UnityEngine;

namespace Amilious.Core.Extensions {
    
    /// <summary>
    /// This class is used to add extensions to the Vector2Int class.
    /// </summary>
    public static class Vector2IntExtension {
        
        /// <summary>
        /// This method is used to convert a Vector2Int into a SerializableVector2Int.
        /// </summary>
        /// <param name="vector2Int">The Vector2 that you want to convert.</param>
        /// <returns>A Serializable version of the given Vector2Int.</returns>
        public static SerializableVector2Int ToSerializable(this Vector2Int vector2Int) {
            return new SerializableVector2Int(vector2Int);
        }
        
    }
}