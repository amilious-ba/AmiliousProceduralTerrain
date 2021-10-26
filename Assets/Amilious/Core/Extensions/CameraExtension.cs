using UnityEngine;

namespace Amilious.Core.Extensions {
    
    /// <summary>
    /// This class is used to add extensions to the Camera class.
    /// </summary>
    public static class CameraExtension {
        
        /// <summary>
        /// This method is used to get the world position from the pointer position.
        /// </summary>
        /// <param name="camera">The camera that you want to use to get the position.</param>
        /// <param name="pointerPos">The position of the pointer.</param>
        /// <returns><see cref="Vector3"/> containing the world position if it was found, otherwise
        /// it returns <value>null</value>.</returns>
        public static Vector3? GetWorldPoint(this Camera camera, Vector3 pointerPos) {
            if(camera == null) return null;
            var ray = camera.ScreenPointToRay(pointerPos);
            if(Physics.Raycast(ray, out var hit)) return hit.point;
            return null;
        }

        /// <summary>
        /// This method is used to get the world position from the pointer position.
        /// </summary>
        /// <param name="camera">The camera that you want to use to get the position.</param>
        /// <param name="pointerPos">The position of the pointer.</param>
        /// <param name="maxDistance">The maximum distance for the <see cref="Ray"/>.</param>
        /// <returns><see cref="Vector3"/> containing the world position if it was found, otherwise
        /// it returns <value>null</value>.</returns>
        public static Vector3? GetWorldPoint(this Camera camera, Vector3 pointerPos, float maxDistance) {
            if(camera == null) return null;
            var ray = camera.ScreenPointToRay(pointerPos);
            if(Physics.Raycast(ray, out var hit,maxDistance)) return hit.point;
            return null;
        }

        /// <summary>
        /// This method is used to get the world position from the pointer position.
        /// </summary>
        /// <param name="camera">The camera that you want to use to get the position.</param>
        /// <param name="pointerPos">The position of the pointer.</param>
        /// <param name="layerMask">The <see cref="LayerMask"/> that you want to use for the <see cref="Ray"/>.</param>
        /// <returns><see cref="Vector3"/> containing the world position if it was found, otherwise
        /// it returns <value>null</value>.</returns>
        public static Vector3? GetWorldPoint(this Camera camera, Vector3 pointerPos, LayerMask layerMask) {
            if(camera == null) return null;
            var ray = camera.ScreenPointToRay(pointerPos);
            if(Physics.Raycast(ray, out var hit,layerMask)) return hit.point;
            return null;
        }
        
        /// <summary>
        /// This method is used to get the world position from the pointer position.
        /// </summary>
        /// <param name="camera">The camera that you want to use to get the position.</param>
        /// <param name="pointerPos">The position of the pointer.</param>
        /// <param name="maxDistance">The maximum distance for the <see cref="Ray"/>.</param>
        /// <param name="layerMask">The <see cref="LayerMask"/> that you want to use for the <see cref="Ray"/>.</param>
        /// <returns><see cref="Vector3"/> containing the world position if it was found, otherwise
        /// it returns <value>null</value>.</returns>
        public static Vector3? GetWorldPoint(this Camera camera, Vector3 pointerPos, float maxDistance, LayerMask layerMask) {
            if(camera == null) return null;
            var ray = camera.ScreenPointToRay(pointerPos);
            if(Physics.Raycast(ray, out var hit,maxDistance,layerMask)) return hit.point;
            return null;
        }
        
    }
}