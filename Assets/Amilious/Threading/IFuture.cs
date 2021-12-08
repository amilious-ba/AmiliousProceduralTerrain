using System;

namespace Amilious.Threading {
    
    /// <summary>
    /// Defines the interface of an object that can be used to track a future value.
    /// </summary>
    /// <typeparam name="T">The type of object being retrieved.</typeparam>
    public interface IFuture<T> {
        
        /// <summary>
        /// Gets the state of the future.
        /// </summary>
        FutureState State { get; }

        /// <summary>
        /// Gets the value if the State is Success.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets the failure exception if the State is Error.
        /// </summary>
        Exception Error { get; }

        /// <summary>
        /// Adds a new callback to invoke if the future value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        IFuture<T> OnSuccess(FutureCallback<T> callback);

        /// <summary>
        /// Adds a new callback to invoke if the future has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        IFuture<T> OnError(FutureCallback<T> callback);

        /// <summary>
        /// Adds a new callback to invoke if the future value is retrieved successfully or has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        IFuture<T> OnComplete(FutureCallback<T> callback);
    }
    
}