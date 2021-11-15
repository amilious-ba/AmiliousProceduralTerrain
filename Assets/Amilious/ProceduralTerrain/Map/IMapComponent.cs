using UnityEngine;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This interface is used to make the map pool generic.  The inheriting
    /// classes must contain a default constructor that is used only to create
    /// an instance that is used to create instances with the <see cref="CreateMapComponent"/>
    /// method.
    /// </summary>
    /// <typeparam name="T">The type of map component.</typeparam>
    public interface IMapComponent<T> where T : class, IMapComponent<T>, new() {

        /// <summary>
        /// This method is used to make a new instance of the map component.
        /// </summary>
        /// <param name="mapManager">The map manager.</param>
        /// <param name="mapPool">The map pool.</param>
        /// <returns>A new instance of the map component.</returns>
        T CreateMapComponent(MapManager mapManager, MapPool<T> mapPool);
        
        /// <summary>
        /// This method is used to process pulling the item from the pool.
        /// </summary>
        /// <param name="setActive">If true the item will be set to active.</param>
        void PullFromPool(bool setActive = false);
        
        /// <summary>
        /// This method is used to setup the map component.
        /// </summary>
        /// <param name="itemId">The item id you want to setup the map component for.</param>
        void Setup(Vector2Int itemId);
        
        /// <summary>
        /// This property is used to get the map components id.
        /// </summary>
        Vector2Int Id { get; }

        /// <summary>
        /// This method is used to process the items release to the pool. At
        /// the end of this method EnqueueItem should be called on the
        /// MapPool.
        /// </summary>
        void ReleaseToPool();

    }
}