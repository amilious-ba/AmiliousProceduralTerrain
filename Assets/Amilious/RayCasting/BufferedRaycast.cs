using System;
using System.Collections.Generic;
using UnityEngine;

namespace Amilious.RayCasting {
    public class BufferedRaycast : MonoBehaviour {
        private readonly RaycastHit[] _raycastBuffer;
        private readonly float[] _distanceBuffer;
        private int _hitSize;
        private readonly int _allowedSize;
        
        /// <summary>
        /// This method is used to create a new <see cref="BufferedRaycast"/>.
        /// </summary>
        /// <param name="allowedSize">The max number of <see cref="GameObject"/> that
        /// can be returned from the raycast.</param>
        public BufferedRaycast(int allowedSize) {
            _allowedSize = allowedSize;
            _raycastBuffer = new RaycastHit[allowedSize];
            _distanceBuffer = new float[allowedSize];
        }

        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> Raycast(Ray ray, bool sortByDistance = false, 
            float maxDistance = Mathf.Infinity , int? layerMask = null) {
            //do raycast
            DoRaycast(ray,maxDistance,layerMask);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }
        
        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.
        /// </summary>
        /// <param name="origin">The starting position of the raycast.</param>
        /// <param name="direction">The direction of the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> Raycast(Vector3 origin, Vector3 direction, bool sortByDistance = false, 
            float maxDistance = Mathf.Infinity , int? layerMask = null) {
            //do raycast
            DoRaycast(origin,direction,maxDistance,layerMask);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }
        
        /// <summary>
        /// This method is used to preform a sphere cast using the allocated buffers.
        /// </summary>
        /// <param name="origin">The starting position of the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="direction">The direction of the sphere cast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> SphereCast(Vector3 origin, float radius, Vector3 direction,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            //do the sphere cast
            DoSphereCast(origin,radius,direction,maxDistance,layerMask);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }

        /// <summary>
        /// This method is used to preform a sphere cast using the allocated buffers.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> SphereCast(Ray ray, float radius,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            //do the sphere cast
            DoSphereCast(ray,radius,maxDistance,layerMask);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }

        /// <summary>
        /// This method is used to get all of the <see cref="MonoBehaviour"/> of the given type.
        /// <see cref="T"/> of all the <see cref="GameObject"/> hit by the raycast.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <typeparam name="T">The type of <see cref="MonoBehaviour"/> you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> <see cref="MonoBehaviour"/> from the raycast. </returns>
        public IEnumerable<T> FilteredRaycast<T>(Ray ray, bool sortByDistance = false,
            float maxDistance = Mathf.Infinity , int? layerMask = null) {
            foreach(var hit in Raycast(ray, sortByDistance, maxDistance, layerMask))
            foreach(var component in hit.transform.GetComponents<T>())
                yield return component;
        }
        
        /// <summary>
        /// This method is used to get all of the <see cref="MonoBehaviour"/> of the given type.
        /// <see cref="T"/> of all the <see cref="GameObject"/> hit by the raycast.
        /// </summary>
        /// <param name="origin">The starting position of the raycast.</param>
        /// <param name="direction">The direction of the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <typeparam name="T">The type of <see cref="MonoBehaviour"/> you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> <see cref="MonoBehaviour"/> from the raycast. </returns>
        public IEnumerable<T> FilteredRaycast<T>(Vector3 origin, Vector3 direction, bool sortByDistance = false,
            float maxDistance = Mathf.Infinity , int? layerMask = null) {
            foreach(var hit in Raycast(origin, direction, sortByDistance, maxDistance, layerMask))
            foreach(var component in hit.transform.GetComponents<T>())
                yield return component;
        }

        /// <summary>
        /// This method is used to get all of the <see cref="MonoBehaviour"/> of the given type.
        /// <see cref="T"/> of all the <see cref="GameObject"/> hit by the raycast.
        /// </summary>
        /// <param name="origin">The starting position of the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="direction">The direction of the sphere cast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <typeparam name="T">The type of <see cref="MonoBehaviour"/> you want to return from the sphere cast.</typeparam>
        /// <returns>All the <see cref="T"/> <see cref="MonoBehaviour"/> from the sphere cast. </returns>
        public IEnumerable<T> FilteredSphereCast<T>(Vector3 origin, float radius, Vector3 direction,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            foreach(var hit in SphereCast(origin, radius, direction, sortByDistance, maxDistance, layerMask))
            foreach(var component in hit.transform.GetComponents<T>())
                yield return component;
        }
        
        /// <summary>
        /// This method is used to get all of the <see cref="MonoBehaviour"/> of the given type.
        /// <see cref="T"/> of all the <see cref="GameObject"/> hit by the raycast.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        /// <typeparam name="T">The type of <see cref="MonoBehaviour"/> you want to return from the sphere cast.</typeparam>
        /// <returns>All the <see cref="T"/> <see cref="MonoBehaviour"/> from the sphere cast. </returns>
        public IEnumerable<T> FilteredSphereCast<T>(Ray ray, float radius,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            foreach(var hit in SphereCast(ray, radius, sortByDistance, maxDistance, layerMask))
            foreach(var component in hit.transform.GetComponents<T>())
                yield return component;
        }
        
        /// <summary>
        /// This method is used to sort the result of
        /// a raycast by the distance of the GameObject.
        /// </summary>
        private void SortResultsByDistance() {
            for(var i = 0; i < _hitSize; i++) {
                _distanceBuffer[i] = _raycastBuffer[i].distance;
            }
            Array.Sort(_distanceBuffer,_raycastBuffer,0,_hitSize);
        }
        
        /// <summary>
        /// This method is used to do the actual raycast.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the raycast.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        private void DoRaycast(Ray ray, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            _hitSize = layerMask.HasValue ? 
                Physics.RaycastNonAlloc(ray, _raycastBuffer, maxDistance, layerMask.Value) : 
                Physics.RaycastNonAlloc(ray, _raycastBuffer, maxDistance);
        }
        
        /// <summary>
        /// This method is used to do the actual raycast.
        /// </summary>
        /// <param name="origin">The starting position of the ray cast.</param>
        /// <param name="direction">The direction of the ray cast.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        private void DoRaycast(Vector3 origin, Vector3 direction, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            _hitSize = layerMask.HasValue ? 
                Physics.RaycastNonAlloc(origin, direction, _raycastBuffer, maxDistance, layerMask.Value) : 
                Physics.RaycastNonAlloc(origin, direction, _raycastBuffer, maxDistance);
        }

        /// <summary>
        /// This method is used to do the actual sphere cast.
        /// </summary>
        /// <param name="origin">The starting position of the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="direction">The direction of the sphere cast.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        private void DoSphereCast(Vector3 origin, float radius, Vector3 direction,
            float maxDistance = Mathf.Infinity, int? layerMask = null) {
            _hitSize = layerMask.HasValue
                ? Physics.SphereCastNonAlloc(origin, radius, direction, _raycastBuffer, maxDistance, layerMask.Value)
                : Physics.SphereCastNonAlloc(origin, radius, direction, _raycastBuffer, maxDistance);
        }
        
        /// <summary>
        /// This method is used to do the actual sphere cast.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="maxDistance">The maximum distance to travel along the <see cref="Ray"/>.</param>
        /// <param name="layerMask">(Optional)The <see cref="LayerMask"/> that will be used for
        /// the raycast.</param>
        private void DoSphereCast(Ray ray, float radius, float maxDistance = Mathf.Infinity, int? layerMask = null) {
            _hitSize = layerMask.HasValue
                ? Physics.SphereCastNonAlloc(ray, radius, _raycastBuffer, maxDistance, layerMask.Value)
                : Physics.SphereCastNonAlloc(ray, radius, _raycastBuffer, maxDistance);
        }
    }
}
