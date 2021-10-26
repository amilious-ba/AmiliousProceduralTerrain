using Amilious.Core.Serializable;
using UnityEngine;

namespace Amilious.Core.Extensions {
    
    
    /// <summary>
    /// This class is used to add extensions to the Vector2 class.
    /// </summary>
    public static class Vector2Extension {
        
        /// <summary>
        /// This method is used to convert a Vector2 into a SerializableVector2.
        /// </summary>
        /// <param name="vector2">The Vector2 that you want to convert.</param>
        /// <returns>A Serializable version of the given Vector2.</returns>
        public static SerializableVector2 ToSerializable(this Vector2 vector2) {
            return new SerializableVector2(vector2);
        }
        
        /// <summary>
        /// This method is used to get the mod result for both x and y.
        /// </summary>
        /// <param name="v2">The vector2 that you want to get the mod values of.</param>
        /// <param name="mod">The mod value that you want to apply to the vector2.</param>
        /// <returns>The mod results.</returns>
        public static Vector2 Mod(this Vector2 v2, float mod) {
            return new Vector2(v2.x % mod, v2.y % mod);
        }

        /// <summary>
        /// This method is used to get the mod result for both x and y.
        /// </summary>
        /// <param name="v2">The vector2 that you want to get the mod values of.</param>
        /// <param name="mod">The mod values that you want to apply to the vector2.</param>
        /// <returns>The mod results.</returns>
        public static Vector2 Mod(this Vector2 v2, Vector2 mod) {
            return new Vector2(v2.x % mod.x, v2.y % mod.y);
        }

        /// <summary>
        /// This method is used round the given vector2 values to the nearest integer.
        /// </summary>
        /// <param name="v2">The vector2 that you want to round.</param>
        /// <returns>The rounded vector2.</returns>
        public static Vector2 Round(this Vector2 v2) {
            return new Vector2(Mathf.Round(v2.x), Mathf.Round(v2.y));
        }
        
    }
    
    
}