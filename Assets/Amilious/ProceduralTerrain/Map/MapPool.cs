using UnityEngine;
using Amilious.Core.Structs;
using Amilious.Core.Interfaces;
using System.Collections.Concurrent;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This class is used for pooling <see cref="IMapComponent{T}"/>s.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IMapComponent{T}"/> that the pool should use.</typeparam>
    public class MapPool<T>: IPool<T,Vector2Int> where T : class, IMapComponent<T>, new() {

        #region Private Instance Variables

        private readonly T _referenceItem;
        private readonly MapManager _manager;
        private readonly ConcurrentDictionary<Vector2Int, T> _loadedItems = 
            new ConcurrentDictionary<Vector2Int, T>();
        private readonly ConcurrentQueue<T> _poolQueue;
        
        #endregion

        #region Public Properties
        
        /// <summary>
        /// This property is used to get a loaded <typeparamref name="T"/> using it's Id.  If
        /// the <typeparamref name="T"/> is not loaded this will return null.
        /// </summary>
        /// <param name="itemId">The id of the item you want to get.</param>
        public T this[Vector2Int itemId] {
            get => _loadedItems.TryGetValue(itemId, out var chunk) ? chunk : null;
        }

        /// <summary>
        /// This property is used to get a loaded <typeparamref name="T"/> using it's itemId x and z values.
        /// If the <typeparamref name="T"/> is not loaded this will return null.
        /// </summary>
        /// <param name="x">The x value of the itemId.</param>
        /// <param name="z">The z (uses y) value of the itemId.</param>
        public T this[int x, int z] { get => this[new Vector2Int(x, z)]; }
        
        /// <summary>
        /// This property returns the total number of used and unused <typeparamref name="T"/>s in this pool.
        /// </summary>
        public int Size { get => Available + CheckedOut; }
        
        /// <summary>
        /// This property returns the number of <typeparamref name="T"/>s that are loaded but not being used.
        /// </summary>
        public int Available { get => _poolQueue.Count; }
        
        /// <summary>
        /// This property returns the number of loaded <typeparamref name="T"/>s.
        /// </summary>
        public int CheckedOut { get => _loadedItems.Count; }

        /// <summary>
        /// This property is used to get the pool info for this pool.
        /// </summary>
        public PoolInfo PoolInfo { get => PoolInfo.FromCheckedOutAndAvailable(CheckedOut,Available); }

        /// <summary>
        /// This property is used to check if the <typeparamref name="T"/> with the given id is visible.
        /// </summary>
        /// <param name="itemId">The id of the <typeparamref name="T"/> that you want to check.</param>
        /// <returns>True if the <typeparamref name="T"/> with the given id is loaded and visible.</returns>
        public bool IsVisible(Vector2Int itemId) => 
            _loadedItems.TryGetValue(itemId, out var chunk) && chunk is { Active: true };
        
        #endregion

        #region Constructors
        
        /// <summary>
        /// This constructor is used to create a new <see cref="MapPool{T}"/>.
        /// </summary>
        /// <param name="manager">The <see cref="MapManager"/> that will use this
        /// <see cref="MapPool{T}"/>.</param>
        /// <param name="preloadSize">If this value is set, the pool will preload the
        /// given number of <typeparamref name="T"/>s.</param>
        public MapPool(MapManager manager, int? preloadSize = null) {
            _manager = manager;
            _poolQueue = new ConcurrentQueue<T>();
            _referenceItem = new T();
            if(!preloadSize.HasValue) return;
            for(var i=0;i<preloadSize.Value;i++)
                _poolQueue.Enqueue(  _referenceItem.CreateMapComponent(manager,this));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to load a <typeparamref name="T"/>.  This will use an available <typeparamref name="T"/>
        /// if one exists in the pool, otherwise it will create a new <typeparamref name="T"/>.
        /// </summary>
        /// <param name="itemId">This id of the <typeparamref name="T"/> that you want to load.</param>
        /// <returns>The existing, loaded, or generated <typeparamref name="T"/> with the given itemId.</returns>
        public T BarrowFromPool(Vector2Int itemId) {
            //if the item is already loaded return it.
            if(_loadedItems.TryGetValue(itemId, out var existing)) return existing;
            //try to get an available item
            _poolQueue.TryDequeue(out var item);
            //if the item is null create a new one.
            item ??= _referenceItem.CreateMapComponent(_manager, this);
            //setup the item
            item.PullFromPool();
            item.Setup(itemId);
            _loadedItems[itemId] = item;
            //return the item
            return item;
        }

        /// <summary>
        /// This method is used to return a chunk to the pool.
        /// </summary>
        /// <param name="item">The <typeparamref name="T"/> that you want to return to the pool.</param>
        public void ReturnToPool(T item) {
            if(item == null) return;
            if(!item.HasProcessedRelease) {
                item.ReleaseToPool();
                return;
            }
            _loadedItems.TryRemove(item.Id, out _);
            _poolQueue.Enqueue(item);
        }
        
        #endregion

    }
}