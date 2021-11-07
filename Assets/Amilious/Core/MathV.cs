using UnityEngine;

namespace Amilious.Core {
    
    public static class MathV {
        
        /// <summary>
        /// This method is used to get the angle in degrees based on
        /// the given position and direction.
        /// </summary>
        /// <param name="position">The position of the object.</param>
        /// <param name="dir">The angle direction.</param>
        /// <returns>The angle in degrees.</returns>
        public static float AnglesFromDirection(Vector3 position, Vector3 dir) {
            var forwardLimitPos = position + dir;
            var srcAngles = Mathf.Rad2Deg * Mathf.Atan2(
                forwardLimitPos.z - position.z, 
                forwardLimitPos.x - position.x);
            return srcAngles;
        }
        
    }
    
}