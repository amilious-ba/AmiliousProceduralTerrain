using System;

namespace Amilious.Core.Interfaces {
    
    public interface IDistanceProvider<out T> where T : IEquatable<T>, IComparable<T>, IFormattable, IConvertible {
        
        /// <summary>
        /// This property is used to get the distance.
        /// </summary>
        T Distance { get; }
        
        /// <summary>
        /// This property is used to get the distance squared.
        /// </summary>
        T DistanceSq { get; }
        
        /// <summary>
        /// This property is used to get the distance or the distance squared.
        /// </summary>
        /// <param name="squared">If true the property will return the distance squared, otherwise
        /// it will return the distance.</param>
        T this[bool squared] { get; }
        
    }
    
}