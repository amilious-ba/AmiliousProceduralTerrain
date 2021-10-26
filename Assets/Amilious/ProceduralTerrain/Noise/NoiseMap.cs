using System;
using UnityEngine;
using Amilious.ProceduralTerrain.Map;

namespace Amilious.ProceduralTerrain.Noise {
    
    /// <summary>
    /// This class is used to hold a noise map.
    /// </summary>
    public class NoiseMap: MapData<float> {
        
        #region Properties
        
        /// <summary>
        /// This property is used to get min and max ranges of the generated
        /// values before modification.
        /// </summary>
        public Vector2 GeneratedMinMax { get; }

        #endregion

        
        #region Constructor

        /// <summary>
        /// This is used to create a new noise map container.
        /// </summary>
        /// <param name="size">This size of the noise map.  This is both the width and the height.</param>
        /// <param name="position">The position or offset of the noise.</param>
        /// <param name="minMax">A vector 2 with x as the min and y as the max.</param>
        /// <param name="isPositionCentered">If true the provided position is centered, otherwise
        /// it is the top left.</param>
        public NoiseMap(int size, Vector2 position, Vector2? minMax = null, bool isPositionCentered = true):
            base(size,position,isPositionCentered){
            minMax??=Vector2.up;
            GeneratedMinMax = minMax.Value;
        }
        
        #endregion


        #region Methods

        /// <summary>
        /// This method is used to remove the given amount clamping the result
        /// between the min and max values that were passed into the constructor.
        /// </summary>
        /// <param name="key">The key of the value you want to add to.</param>
        /// <param name="amount">The amount you want to remove from the give key's value.</param>
        /// <returns>True if the key was valid, otherwise returns false.</returns>
        public bool ClampReduce(Vector2Int key, float amount) {
            if(!ContainsKey(key)) return false;
            this[key] = Mathf.Clamp(this[key] - amount, -1, 1);
            return true;
        }

        /// <summary>
        /// This method will add the given amount clamping the result
        /// between the min and max values that were passed into the constructor.
        /// </summary>
        /// <param name="key">The key of the value you want to add to.</param>
        /// <param name="amount">The amount you want to add to the give key's value.</param>
        /// <returns>True if the key was valid, otherwise returns false.</returns>
        public bool ClampGain(Vector2Int key, float amount) {
            if(!ContainsKey(key)) return false;
            this[key] = Mathf.Clamp(this[key] + amount, -1, 1);
            return true;
        }

        /// <summary>
        /// This method is used to try set the data map value at the
        /// given position.
        /// </summary>
        /// <param name="x">The x position of the data map.</param>
        /// <param name="z">The z position of the data map.</param>
        /// <param name="value">The value that you want to set at
        /// the given position.</param>
        /// <returns>True if the value was set, otherwise returns false
        /// if the provided position was invalid.</returns>
        public bool TrySetValue(int x, int z, float value) {
            if(!ContainsKey(x, z)) return false;
            this[x, z] = value;
            return true;
        }

        /// <summary>
        /// This method is used to try set the data map value at the
        /// given position.
        /// </summary>
        /// <param name="key">The data map key.  The x value will be used
        /// as the map data's x position and the y value will be used as
        /// the map data's z position.</param>
        /// <param name="value">The value that you want to set at
        /// the given position.</param>
        /// <returns>True if the value was set, otherwise returns false
        /// if the provided position was invalid.</returns>
        public bool TrySetValue(Vector2Int key, float value) {
            if(!ContainsKey(key)) return false;
            this[key] = value;
            return true;
        }

        #endregion

        
        #region Operators
        
        /// <summary>
        /// This operator does returns a new map with the edited values.
        /// </summary>
        /// <param name="left">The NoiseMap that you want to preform the operation on.</param>
        /// <param name="right">The multiply value.</param>
        /// <returns>The modified original NoiseMap.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NoiseMap operator *(NoiseMap left, float right) {
            if(left == null) throw new ArgumentNullException(nameof(left), "You can't multiply a null NoiseMap.");
            var newMap = new NoiseMap(left.Size, left.Position);
            foreach(var key in left) {
                newMap.TrySetValue(key,left[key]*right);
            }
            return newMap;
        }
        
        /// <summary>
        /// This operator does returns a new map with the edited values.
        /// </summary>
        /// <param name="left">The NoiseMap that you want to preform the operation on.</param>
        /// <param name="right">The divisor value. </param>
        /// <returns>The modified original NoiseMap.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DivideByZeroException"></exception>
        public static NoiseMap operator /(NoiseMap left, float right) {
            if(left == null) throw new ArgumentNullException(nameof(left), "You can't divide a null NoiseMap.");
            if(right == 0) throw new DivideByZeroException("You can't divide a NoiseMap by zero.");
            var newMap = new NoiseMap(left.Size, left.Position);
            foreach(var key in left) {
                newMap.TrySetValue(key,left[key]/right);
            }
            return newMap;
        }
        
        /// <summary>
        /// This operator does returns a new map with the edited values.
        /// </summary>
        /// <param name="left">The NoiseMap that you want to preform the operation on.</param>
        /// <param name="right">The addition value.</param>
        /// <returns>The modified original NoiseMap.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NoiseMap operator +(NoiseMap left, float right) {
            if(left == null) throw new ArgumentNullException(nameof(left), "You can't add to a null NoiseMap.");
            if(right == 0) return left;
            var newMap = new NoiseMap(left.Size, left.Position);
            foreach(var key in left) {
                newMap.TrySetValue(key,left[key]+right);
            }
            return newMap;
        }
        
        /// <summary>
        /// This operator does returns a new map with the edited values.
        /// </summary>
        /// <param name="left">The NoiseMap that you want to preform the operation on.</param>
        /// <param name="right">The subtraction value.</param>
        /// <returns>The modified original NoiseMap.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NoiseMap operator -(NoiseMap left, float right) {
            if(left == null) throw new ArgumentNullException(nameof(left), "You can't subtract from a null NoiseMap.");
            if(right == 0) return left;
            var newMap = new NoiseMap(left.Size, left.Position);
            foreach(var key in left) {
                newMap.TrySetValue(key,left[key]-right);
            }
            return newMap;
        }
        
        /// <summary>
        /// This operator does returns a new map with the edited values.
        /// </summary>
        /// <param name="left">The NoiseMap that you want to preform the operation on.</param>
        /// <param name="right">The mod divisor value.</param>
        /// <returns>The modified original NoiseMap.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NoiseMap operator %(NoiseMap left, float right) {
            if(left == null) throw new ArgumentNullException(nameof(left), "You can't mod a null NoiseMap.");
            if(right == 0) return left;
            var newMap = new NoiseMap(left.Size, left.Position);
            foreach(var key in left) {
                newMap.TrySetValue(key,left[key]%right);
            }
            return newMap;
        }
        
        #endregion
    }
}