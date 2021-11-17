using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Amilious.Core.Interfaces;

namespace Amilious.Core.Structs {
    
    [Serializable, InlineProperty]
    public struct DistanceValueInt : IDistanceProvider<int> {
        
        [SerializeField, InlineProperty, HideLabel]
        private int distance;
        private int? _distanceSq;
        
        /// <summary>
        /// This property contains the distance.
        /// </summary>
        public int Distance { get => distance; }

        /// <summary>
        /// This property contains the distance squared.
        /// </summary>
        public int DistanceSq {
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
        public int this[bool squared] => squared?DistanceSq:Distance;

        /// <summary>
        /// This constructor is used to create a new DistanceProvider.
        /// </summary>
        /// <param name="distance">The distance not squared.</param>
        /// <param name="calculateSq">If true the squared distance will be calculated
        /// in the constructor.</param>
        public DistanceValueInt(int distance, bool calculateSq = false) {
            this.distance = distance;
            if(calculateSq) _distanceSq = distance * distance;
            else _distanceSq = null;
        }

        /// <summary>
        /// This constructor is used to create a new DistanceProvider.
        /// </summary>
        /// <param name="maxDistance">The max distance not squared.</param>
        /// <param name="maxDistanceSq">The max distance squared.</param>
        public DistanceValueInt(int maxDistance, int maxDistanceSq) {
            distance = maxDistance;
            _distanceSq = maxDistanceSq;
        }

        /// <summary>
        /// This method is used to check if this is equal to the provided value. 
        /// </summary>
        /// <param name="other">The value you want to compare.</param>
        /// <returns>True if the provided value is equal to this value.</returns>
        public bool Equals(IDistanceProvider<float> other) {
            return other.Distance == Distance;
        }

        /// <summary>
        /// This method is used to check if this is equal to the provided value.
        /// </summary>
        /// <param name="other">The value you want to compare.</param>
        /// <returns>True if the provided value is equal to this value.</returns>
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
            return obj is IDistanceProvider<int> other && Equals(other) ||
                   obj is IDistanceProvider<float> other2 && Equals(other2);
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
        public static bool operator == (DistanceValueInt a, IDistanceProvider<int> b) => a.Equals(b);
        
        /// <summary>
        /// This operator is used to check if the two distance values are equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are equal.</returns>
        public static bool operator == (DistanceValueInt a, IDistanceProvider<float> b) => a.Equals(b);

        /// <summary>
        /// This operator is used to check if the two provided values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are not equal.</returns>
        public static bool operator !=(DistanceValueInt a, IDistanceProvider<int> b) => !(a == b);

        /// <summary>
        /// This operator is used to check if the two provided values are not equal.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the values are not equal.</returns>
        public static bool operator !=(DistanceValueInt a, IDistanceProvider<float> b) => !(a == b);

        /// <summary>
        /// This operator is used to implicitly cast a DistanceValueInt into a DistanceValue.
        /// </summary>
        /// <param name="value">The value that you want to cast.</param>
        /// <returns>The casted value.</returns>
        public static implicit operator DistanceValue(DistanceValueInt value) {
            return new DistanceValue(value.distance, value.DistanceSq);
        }
        
    }
}