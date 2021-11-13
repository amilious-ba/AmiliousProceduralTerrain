using Amilious.Core.Structs;

namespace Amilious.Core.Interfaces {
    
    /// <summary>
    /// This is an interface for creating a pool.
    /// </summary>
    /// <typeparam name="T">The type of object within the pool.</typeparam>
    public interface IPool<T> {
        
        /// <summary>
        /// This property is used to get the current pool info for this pool.
        /// </summary>
        public PoolInfo PoolInfo { get; }
        
        /// <summary>
        /// This property is used to get the current size of the pool.
        /// </summary>
        public int Size { get; }
        
        /// <summary>
        /// This property is used to get the current number of available objects in the pool.
        /// </summary>
        public int Available { get; }
        
        /// <summary>
        /// This property is used to get the number of checked out objects in the pool.
        /// </summary>
        public int CheckedOut { get; }
        
        /// <summary>
        /// This method is used to pull an item from the available items in the pool, or create a new
        /// item if there are no available items.
        /// </summary>
        /// <returns>The <see cref="T"/> that was created or pulled from the pool.</returns>
        T CheckOut();
        
        /// <summary>
        /// This method is used to add or return a <see cref="T"/> to the pool.
        /// </summary>
        /// <param name="pooledItem">The item you want to add or return to the pool.</param>
        void ReturnToPool(T pooledItem);

    }
    
    /// <summary>
    /// This is an interface for creating a pool.
    /// </summary>
    /// <typeparam name="T">The type of object within the pool.</typeparam>
    /// <typeparam name="T2">A key that is used for setting up an object as it is pulled from the pool.</typeparam>
    public interface IPool<T, in T2> {
        
        /// <summary>
        /// This property is used to get the current pool info for this pool.
        /// </summary>
        public PoolInfo PoolInfo { get; }
        
        /// <summary>
        /// This property is used to get the current size of the pool.
        /// </summary>
        public int Size { get; }
        
        /// <summary>
        /// This property is used to get the current number of available objects in the pool.
        /// </summary>
        public int Available { get; }
        
        /// <summary>
        /// This property is used to get the number of checked out objects in the pool.
        /// </summary>
        public int CheckedOut { get; }
        
        /// <summary>
        /// This method is used to pull an item from the available items in the pool, or create a new
        /// item if there are no available items.
        /// </summary>
        /// <param name="key">A key that is used for setting up the object as it is pulled
        /// from the pool.</param>
        /// <returns>The <see cref="T"/> that was created or pulled from the pool.</returns>
        T BarrowFromPool(T2 key);
        
        /// <summary>
        /// This method is used to add or return a <see cref="T"/> to the pool.
        /// </summary>
        /// <param name="pooledItem">The item you want to add or return to the pool.</param>
        void ReturnToPool(T pooledItem);

    }
    
}