using System;
using UnityEngine;
using System.Collections.Generic;

namespace Amilious.RayCasting {
    
    /// <summary>
    /// This class is used to do raycast with a reusable buffer and with sorting and filtering options.
    /// Using this object for recasting should reduce the garbage collection.
    /// </summary>
    public class BufferedRaycaster : MonoBehaviour {
        
        private readonly RaycastHit[] _raycastBuffer;
        private readonly float[] _distanceBuffer;
        private int _hitSize;
        
        /// <summary>
        /// This method is used to create a new <see cref="BufferedRaycaster"/>.
        /// </summary>
        /// <param name="allowedSize">The max number of <see cref="GameObject"/> that
        /// can be returned from the raycast.</param>
        public BufferedRaycaster(int allowedSize) {
            _raycastBuffer = new RaycastHit[allowedSize];
            _distanceBuffer = new float[allowedSize];
        }

        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a ray.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> Raycast(Ray ray, bool sortByDistance = false, 
            float maxDistance = Mathf.Infinity , int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            //do raycast
            _hitSize = Physics.RaycastNonAlloc(ray, _raycastBuffer, maxDistance, layerMask, queryTriggerInteraction);
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
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a ray.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> Raycast(Vector3 origin, Vector3 direction, bool sortByDistance = false, 
            float maxDistance = Mathf.Infinity , int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            //do raycast
            _hitSize = Physics.RaycastNonAlloc(origin, direction, _raycastBuffer, maxDistance, layerMask,
                queryTriggerInteraction);
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
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a sphere.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> SphereCast(Vector3 origin, float radius, Vector3 direction,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            //do the sphere cast
            _hitSize = Physics.SphereCastNonAlloc(origin, radius, direction, _raycastBuffer, maxDistance, layerMask,
                queryTriggerInteraction);
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
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a sphere.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> SphereCast(Ray ray, float radius,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            //do the sphere cast
            _hitSize = Physics.SphereCastNonAlloc(ray, radius, _raycastBuffer, maxDistance, layerMask,
                queryTriggerInteraction);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }

        /// <summary>
        /// This method is used to preform a box cast using the allocated buffers.
        /// </summary>
        /// <param name="center">Center of the box.</param>
        /// <param name="halfExtents">Half of the size of the box in each direction.</param>
        /// <param name="direction">The direction in which to cast the box.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param> 
        /// <param name="orientation">Rotation of the box.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a box.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> BoxCast(Vector3 center, Vector3 halfExtents, 
            Vector3 direction, bool sortByDistance = false, Quaternion? orientation = null, 
            float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            orientation??=Quaternion.identity;
            //do the box cast
            _hitSize = Physics.BoxCastNonAlloc(center, halfExtents, direction, _raycastBuffer, orientation.Value, 
                maxDistance, layerMask, queryTriggerInteraction);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }

        /// <summary>
        /// This method is used to preform a box cast using the allocated buffers.
        /// </summary>
        /// <param name="point1">The center of the sphere at the start of the capsule.</param>
        /// <param name="point2">The center of the sphere at the end of the capsule.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="direction">The direction in which to cast the capsule.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param> 
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a Capsule.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<RaycastHit> CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, 
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            //do the capsule cast
            _hitSize = Physics.CapsuleCastNonAlloc(point1, point2, radius, direction, _raycastBuffer, maxDistance,
                layerMask, queryTriggerInteraction);
            //sort raycast
            if(sortByDistance) SortResultsByDistance();
            //return the values
            for(var i = 0; i < _hitSize; i++) yield return _raycastBuffer[i];
        }

        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.  Only the components
        /// of the type <see cref="T"/> will be returned.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a ray.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <typeparam name="T">The type of component you want to return from the raycast.</typeparam>
        /// <returns>The result of the raycast.</returns>
        public IEnumerable<T> FilteredRaycast<T>(Ray ray, bool sortByDistance = false, 
            float maxDistance = Mathf.Infinity , int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {    
            foreach(var hit in Raycast(ray, sortByDistance, maxDistance, layerMask,queryTriggerInteraction))
            foreach(var component in hit.transform.GetComponents<T>()) yield return component;
        }
        
        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.  Only the components
        /// of the type <see cref="T"/> will be returned.
        /// </summary> 
        /// <param name="origin">The starting position of the raycast.</param>
        /// <param name="direction">The direction of the raycast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a ray.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <typeparam name="T">The type of component you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> components from the raycast. </returns>
        public IEnumerable<T> FilteredRaycast<T>(Vector3 origin, Vector3 direction, bool sortByDistance = false, 
            float maxDistance = Mathf.Infinity , int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            foreach(var hit in Raycast(origin, direction, sortByDistance, maxDistance, layerMask, queryTriggerInteraction))
            foreach(var component in hit.transform.GetComponents<T>()) yield return component;
        }

        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.  Only the components
        /// of the type <see cref="T"/> will be returned.
        /// </summary>
        /// <param name="origin">The starting position of the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="direction">The direction of the sphere cast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a sphere.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <typeparam name="T">The type of component you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> components from the raycast. </returns>
        public IEnumerable<T> FilteredSphereCast<T>(Vector3 origin, float radius, Vector3 direction,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            foreach(var hit in SphereCast(origin, radius, direction, sortByDistance, maxDistance, 
                layerMask, queryTriggerInteraction))
            foreach(var component in hit.transform.GetComponents<T>()) yield return component;
        }
        
        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.  Only the components
        /// of the type <see cref="T"/> will be returned.
        /// </summary>
        /// <param name="ray">The <see cref="Ray"/> you want to use for the sphere cast.</param>
        /// <param name="radius">The radius of the sphere cast.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a sphere.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <typeparam name="T">The type of component you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> components from the raycast. </returns>
        public IEnumerable<T> FilteredSphereCast<T>(Ray ray, float radius,
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            foreach(var hit in SphereCast(ray, radius, sortByDistance, maxDistance, layerMask, queryTriggerInteraction))
            foreach(var component in hit.transform.GetComponents<T>()) yield return component;
        }

        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.  Only the components
        /// of the type <see cref="T"/> will be returned.
        /// </summary>
        /// <param name="center">Center of the box.</param>
        /// <param name="halfExtents">Half of the size of the box in each direction.</param>
        /// <param name="direction">The direction in which to cast the box.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param> 
        /// <param name="orientation">Rotation of the box.</param>
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a box.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <typeparam name="T">The type of component you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> components from the raycast. </returns>
        public IEnumerable<T> FilteredBoxCast<T>(Vector3 center, Vector3 halfExtents, 
            Vector3 direction, bool sortByDistance = false, Quaternion? orientation = null, 
            float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, 
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            foreach(var hit in BoxCast(center,halfExtents,direction,sortByDistance, orientation,
                maxDistance,layerMask,queryTriggerInteraction))
            foreach(var component in hit.transform.GetComponents<T>()) yield return component;
        }

        /// <summary>
        /// This method is used to preform a raycast using the allocated buffers.  Only the components
        /// of the type <see cref="T"/> will be returned.
        /// </summary>
        /// <param name="point1">The center of the sphere at the start of the capsule.</param>
        /// <param name="point2">The center of the sphere at the end of the capsule.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="direction">The direction in which to cast the capsule.</param>
        /// <param name="sortByDistance">If true the results will be sorted by distance,
        /// otherwise the results will not be sorted.</param> 
        /// <param name="maxDistance">The max length of the cast.</param>
        /// <param name="layerMask">A <see cref="LayerMask"/> that is used to selectively ignore
        /// colliders when casting a Capsule.</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit triggers.</param>
        /// <typeparam name="T">The type of component you want to return from the raycast.</typeparam>
        /// <returns>All the <see cref="T"/> components from the raycast. </returns>
        public IEnumerable<T> FilteredCapsuleCast<T>(Vector3 point1, Vector3 point2, float radius, Vector3 direction, 
            bool sortByDistance = false, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
            foreach(var hit in CapsuleCast(point1,point2,radius,direction,sortByDistance,maxDistance,
                layerMask,queryTriggerInteraction))
            foreach(var component in hit.transform.GetComponents<T>()) yield return component;
        }

        /// <summary>
        /// This method is used to sort the result of
        /// a raycast by the distance of the GameObject.
        /// </summary>
        private void SortResultsByDistance() {
            for(var i = 0; i < _hitSize; i++) _distanceBuffer[i] = _raycastBuffer[i].distance;
            Array.Sort(_distanceBuffer,_raycastBuffer,0,_hitSize);
        }
        
        
    }
}
