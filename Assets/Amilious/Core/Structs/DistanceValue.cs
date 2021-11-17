using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Amilious.Core.Interfaces;

namespace Amilious.Core.Structs {
    
    /// <summary>
    /// This class is used to represent a distance threshold 
    /// </summary>
    [Serializable, InlineProperty]
    public struct DistanceValue : IDistanceProvider<float> {
        
        [SerializeField, InlineProperty, HideLabel]
        private float distance;
        [DisplayAsString]
        private float? _distanceSq;
        
        /// <summary>
        /// This property contains the distance.
        /// </summary>
        public float Distance { get => distance; }

        /// <summary>
        /// This property contains the distance squared.
        /// </summary>
        public float DistanceSq {
            get {
                _distanceSq ??= distance * distance;
                return _distanceSq.Value;
            }
        }

        /// <summary>
        /// This property is used to get the distance.
        /// </summary>
        /// <param name="squared">If true this will return the squared distance, otherwise
        /// it will return the normal distance.</param>
        public float this[bool squared] => squared?DistanceSq:Distance;

        /// <summary>
        /// This constructor is used to create a new DistanceProvider.
        /// </summary>
        /// <param name="distance">The distance not squared.</param>
        /// <param name="calculateSq">If true the squared distance will be calculated
        /// in the constructor.</param>
        public DistanceValue(float distance, bool calculateSq = false) {
            this.distance = distance;
            if(calculateSq) _distanceSq = distance * distance;
            else _distanceSq = null;
        }

        /// <summary>
        /// This constructor is used to create a new DistanceProvider.
        /// </summary>
        /// <param name="maxDistance">The max distance not squared.</param>
        /// <param name="maxDistanceSq">The max distance squared.</param>
        public DistanceValue(float maxDistance, float maxDistanceSq) {
            distance = maxDistance;
            _distanceSq = maxDistanceSq;
        }

        /// <summary>
        /// This method is used to check if this is equal to the provided value.
        /// </summary>
        /// <param name="other">The value that you want to compare.</param>
        /// <returns>True if the provided value is equal to this, otherwise false.</returns>
        public bool Equals(IDistanceProvider<float> other) {
            return other.Distance == Distance;
        }

        /// <summary>
        /// This method is used to check if this is equal to the provided value.
        /// </summary>
        /// <param name="other">The value that you want to compare.</param>
        /// <returns>True if the provided value is equal to this, otherwise false.</returns>
        public bool Equals(IDistanceProvider<int> other) {
            return other.Distance == Distance;
        }

        /// <summary>
        /// This method is used to check if the given ob
        /// </summary>
        /// <param name="obj">The object you want to compare.</param>
        /// <returns>True if the provided object is a <see cref="IDistanceProvider{T}"/> and
        /// its distance is equal.</returns>
        public override bool Equals(object obj) {
            return obj is IDistanceProvider<float> other && Equals(other);
        }

        /// <summary>
        /// This method is used to get the hash code for the distance.
        /// </summary>
        /// <returns>The hashcode for the distance.</returns>
        public override int GetHashCode() => Distance.GetHashCode();

        /// <summary>
        /// This operator is used to check if the two distance values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are equal.</returns>
        public static bool operator == (DistanceValue a, IDistanceProvider<float> b) => a.Equals(b);

        /// <summary>
        /// This operator is used to check if the two distance values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are equal.</returns>
        public static bool operator ==(DistanceValue a, IDistanceProvider<int> b) => a.Equals(b);

        /// <summary>
        /// This operator is used to check if the two provided values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are not equal.</returns>
        public static bool operator !=(DistanceValue a, IDistanceProvider<float> b) => !(a == b);

        /// <summary>
        /// This operator is used to check if the two provided values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are not equal.</returns>
        public static bool operator !=(DistanceValue a, IDistanceProvider<int> b) => !(a == b);
        
        /// <summary>
        /// This operator is used to implicitly cast a DistanceValue into a DistanceValueInt.
        /// </summary>
        /// <param name="value">The value that you want to cast.</param>
        /// <returns>The casted value.</returns>
        public static implicit operator DistanceValueInt(DistanceValue value) {
            return new DistanceValueInt((int)value.Distance, (int)value.DistanceSq);
        }
        
    }
    
}