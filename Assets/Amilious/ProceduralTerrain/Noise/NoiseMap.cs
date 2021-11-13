using System;
using UnityEngine;
using Amilious.Saving;

namespace Amilious.ProceduralTerrain.Noise {
    
    /// <summary>
    /// This class is used to hold a noise map.
    /// </summary>
    public class NoiseMap: MapData<float> {

        private const string PREFIX = "noiseMap";
        private const string VALUES = "values";
        private const string POSITION = "position";
        private const string INVALID_KEY = "The provided key does not exist in this map.";
        private const string INVALID_CENTER_KEY =
            "The key you provided is invaid.  The key must exist in the map and it" +
            "cannot be a border value";
        
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
            HasBeenUpdated = true;
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
            HasBeenUpdated = true;
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

        /// <summary>
        /// This method can be used to change the position of the map.
        /// </summary>
        /// <remarks>This method does not regenerate the values.</remarks>
        /// <param name="position">The new position of the map.</param>
        public void ResetPosition(Vector2 position) {
            Position = position;
        }

        /// <summary>
        /// This method is used to save the <see cref="NoiseMap"/>.
        /// </summary>
        /// <param name="saveData">The <see cref="SaveData"/> that will be used to save the map values.</param>
        public bool Save(SaveData saveData) {
            if(!HasBeenUpdated) return false;
            saveData.SetPrefix(PREFIX);
            saveData.StoreData(VALUES, values);
            saveData.StoreData(POSITION, Position);
            saveData.ClearPrefix();
            HasBeenUpdated = false;
            return true;
        }

        /// <summary>
        /// This method is used to load the <see cref="NoiseMap"/>.
        /// </summary>
        /// <param name="saveData">The <see cref="SaveData"/> that will be used to load the map values.</param>
        public void Load(SaveData saveData) {
            saveData.SetPrefix(PREFIX);
            values = saveData.FetchData<float[,]>(VALUES);
            Position = saveData.FetchData<Vector2>(POSITION);
            saveData.ClearPrefix();
            HasBeenUpdated = false;
        }

        /// <summary>
        /// This method is used to get the steepness of a given point in the map.
        /// </summary>
        /// <param name="key">The point you want to get the steepness for.</param>
        /// <param name="useFast">If true the method will be faster but less accurate.</param>
        /// <returns>The calculated steepness value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the key is invalid.</exception>
        public float GetSteepnessUsing3Points(Vector2Int key,bool useFast = false) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, INVALID_KEY);
            var dx = this[key.x + 1, key.y] - this[key];
            var dy = this[key.x, key.y + 1] - this[key];
            return useFast? Mathf.Abs(dx)+Mathf.Abs(dy) : Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// This method is used to get the steepness of a give point in the map using
        /// central differencing.
        /// </summary>
        /// <param name="key">The point that you want to get the steepness for.</param>
        /// <param name="gridSize">The grid size.</param>
        /// <returns>The steepness of the given point.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the key is invalid.</exception>
        public Vector3 GetSteepnessCentralDifferencing(Vector2Int key, float gridSize = 1) {
            if(key.x<gridSize||key.x>=Size-gridSize||key.y<gridSize||key.y>=Size-gridSize)
                throw new ArgumentOutOfRangeException(nameof(key), key, INVALID_CENTER_KEY);
            var t = this[key + Vector2Int.up];
            var b = this[key + Vector2Int.down];
            var l = this[key + Vector2Int.left];
            var r = this[key + Vector2Int.right];
            return new Vector3((r - l) / (2 * gridSize), (t - b) / (2 * gridSize), -1).normalized;
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