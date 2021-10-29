using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amilious.ProceduralTerrain {
    
    /// <summary>
    /// This is a base class for the other map data classes.
    /// </summary>
    /// <typeparam name="T">The type of data that is stored in
    /// the map.</typeparam>
    public class MapData<T> : IEnumerable<Vector2Int> {
        
        protected readonly T[,] values;
        
        /// <summary>
        /// This property is true if the position is centered,
        /// otherwise the position is the topLeft.
        /// </summary>
        public bool IsPositionCentered { get; }
        
        /// <summary>
        /// This constructor is used to create a new map data.
        /// </summary>
        /// <param name="size">Both the length and width of the map.</param>
        /// <param name="position">The position of the map.</param>
        /// <param name="isPositionCentered">If true the position is at
        /// the center of the map, otherwise the position should be the
        /// top left corner.</param>
        public MapData(int size, Vector2 position, bool isPositionCentered = true) {
            Size = size;
            HalfSize = size / 2f;
            Position = position;
            NumberOfValues = size * size;
            values = new T[Size, Size];
            IsPositionCentered = isPositionCentered;
        }
        
        #region Values and Value Modification

        /// <summary>
        /// This property is used to get or protected set the values at
        /// the given x and z position.
        /// </summary>
        /// <param name="x">The x position within the map data.</param>
        /// <param name="z">The z position within the map data.</param>
        /// <exception cref="ArgumentOutOfRangeException"> This exception
        /// will be thrown if the give x or z is out of range.</exception>
        public T this[int x, int z] {
            get {
                if(!IsValidX(x)) throw new ArgumentOutOfRangeException(nameof(x), x,
                $"This value should be inclusively between 0 and {Size - 1}");
                if(!IsValidZ(z)) throw new ArgumentOutOfRangeException(nameof(z), z,
                $"This value should be inclusively between 0 and {Size - 1}");
                return values[x, z];
            }
            protected set {
                if(!IsValidX(x))
                    throw new ArgumentOutOfRangeException(nameof(x), x,
                        $"This value should be inclusively between 0 and {Size - 1}");
                if(!IsValidX(x))
                    throw new ArgumentOutOfRangeException(nameof(z), z,
                        $"This value should be inclusively between 0 and {Size - 1}");
                values[x, z] = value;
            }
        }

        /// <summary>
        /// This property is used to get or protected set the values at
        /// the given key.
        /// </summary>
        /// <param name="key">The key where x is the x position and y is
        /// the z position.</param>
        public T this[Vector2Int key] {
            get => this[key.x, key.y];
            protected set => this[key.x, key.y] = value;
        }
        
        /// <summary>
        /// This property is used to get or protected set the values at
        /// the given map data index.
        /// </summary>
        /// <param name="index">The map data index.</param>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown
        /// if the index is invalid.</exception>
        public T this[int index] {
            get {
                if(!IsValidIndex(index)) throw new ArgumentOutOfRangeException(nameof(index), index,
                $"The given index is invalid.  It should be inclusively between 0 and {NumberOfValues-1}");
                return this[IndexToKey(index)];
            }
            protected set {
                if(!IsValidIndex(index)) throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"The given index is invalid.  It should be inclusively between 0 and {NumberOfValues-1}");
                this[IndexToKey(index)] = value;
            }
        }
        
        /// <summary>
        /// This method is used to try get a value from the map data.
        /// </summary>
        /// <param name="key">The key of the value you want to get.</param>
        /// <param name="value">The value of the given key.</param>
        /// <returns>True if the given key exists, otherwise false.</returns>
        public bool TryGetValue(Vector2Int key, out T value) {
            value = default(T);
            if(!ContainsKey(key)) return false;
            value = this[key];
            return true;
        }

        /// <summary>
        /// This method is used to try get a value from the map data.
        /// </summary>
        /// <param name="x">The x position of the value you want to get.</param>
        /// <param name="z">The z position of the value you want to get.</param>
        /// <param name="value">The value of the given x z.</param>
        /// <returns>True if the given key exists, otherwise false.</returns>
        public bool TryGetValue(int x, int z, out T value) {
            var result = TryGetValue(new Vector2Int(x,z), out var val);
            value = val;
            return result;
        }

        /// <summary>
        /// This method is used to convert a map data index into a map data key.
        /// </summary>
        /// <param name="index">The map data index that you want to convert into
        /// a map data key.</param>
        /// <returns>The equivalent map data key for the provided map data index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown if the
        /// provided index is invalid.</exception>
        public Vector2Int IndexToKey(int index) {
            if(!IsValidIndex(index)) throw new ArgumentOutOfRangeException(nameof(index), index,
            $"The given index is invalid.  It should be inclusively between 0 and {NumberOfValues-1}");
            var z = index / Size;
            var x = index - (z*Size);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// This method is used to convert a map key into an index value.
        /// </summary>
        /// <param name="key">The key that you want to convert into an
        /// map data index.</param>
        /// <returns>The equivalent map data index for the given key.</returns>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown if
        /// the map data does not contain the given key.</exception>
        public int KeyToIndex(Vector2Int key) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key,
            "Either the x or the y position was out of bounds.");
            return key.y * Size + key.x;
        }
        
        #endregion
        
        #region Key Validation

        /// <summary>
        /// This method is used to check if the given value is a valid
        /// x position in the map data.
        /// </summary>
        /// <param name="x">The value you want to validate.</param>
        /// <returns>True if the given value is a valid x position in
        /// the map data, otherwise returns false.</returns>
        public bool IsValidX(int x) => x > -1 && x < Size;
        
        /// <summary>
        /// This method is used to check if the given value is a valid
        /// z position in the map data.
        /// </summary>
        /// <param name="z">The value you want to validate.</param>
        /// <returns>True if the given value is a valid z position in
        /// the map data, otherwise returns false.</returns>
        public bool IsValidZ(int z) => z > -1 && z < Size;

        /// <summary>
        /// This method is used to check if the given index is a valid
        /// position within the map data.
        /// </summary>
        /// <param name="index">The index that you want to validate.</param>
        /// <returns>True if the given index is a valid position within the
        /// map data, otherwise returns false.</returns>
        public bool IsValidIndex(int index) => index > -1 && index < NumberOfValues;

        /// <summary>
        /// This method is used to check if the map data contains the given
        /// x and z positions.
        /// </summary>
        /// <param name="x">The x map data position you want to check.</param>
        /// <param name="z">The x map data position you want to check.</param>
        /// <returns>True if the map data contains the give x and z positions,
        /// otherwise returns false.</returns>
        public bool ContainsKey(int x, int z) => IsValidX(x)&&IsValidZ(z);

        /// <summary>
        /// This method is used to check if the map data contains the given key.
        /// </summary>
        /// <param name="key">The key you want to check.  The x value will be used
        /// for x, and the y value will be used for z.</param>
        /// <returns></returns>
        public bool ContainsKey(Vector2Int key) => ContainsKey(key.x, key.y);
        
        #endregion
        
        #region Sizes
        
        /// <summary>
        /// This property is used to get the size of the map data.  This
        /// size is both the width and height of the data.
        /// </summary>
        public int Size { get; }
        
        /// <summary>
        /// This property is used to get the half size of the map data.  This
        /// size is half of the width or height.
        /// </summary>
        public float HalfSize { get; }
        
        /// <summary>
        /// This property is used to get the number of values that are stored
        /// int the map data.
        /// </summary>
        public int NumberOfValues { get; }
        
        /// <summary>
        /// This property is used to get the position of the map data.
        /// </summary>
        public Vector2 Position { get; }
        
        /// <summary>
        /// This method is used to get the size using the given borderCulling.
        /// </summary>
        /// <param name="borderCulling">The amount you want to remove from
        /// all sides.</param>
        /// <returns>The size subtracting the given value twice.</returns>
        public int GetBorderCulledSize(int borderCulling) {
            return Size - (borderCulling * 2);
        }
        
        /// <summary>
        /// This method is used to get the number of values in the map data after
        /// when using the given borderCulling.
        /// </summary>
        /// <param name="borderCulling">The amount you want to remove from all
        /// sides.</param>
        /// <returns>The number of values in the map after removing the given amount
        /// from all of the sides.</returns>
        public int GetBorderCulledValuesCount(int borderCulling) {
            return GetBorderCulledSize(borderCulling) * GetBorderCulledSize(borderCulling);
        }
        
        #endregion
        
        #region Enumerators

        /// <summary>
        /// This method is used to return an IEnumerable collection of all of
        /// the valid keys within the map data.
        /// </summary>
        /// <param name="borderCulling">This optional value allows you to cull
        /// border keys.  This will return x and y values that start at the
        /// borderCulling value and end with the Size - borderCulling - 1.</param>
        /// <returns>The keys that can be iterated over.</returns>
        public IEnumerable<Vector2Int> BorderCulledKeys(int borderCulling = 0) {
            for(var y=borderCulling;y<Size-borderCulling;y++) 
            for(var x = borderCulling; x < Size-borderCulling; x++)
                yield return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// This can be used to enumerate through all of the map data's keys.
        /// </summary>
        /// <returns>The keys for the map data.</returns>
        public IEnumerator<Vector2Int> GetEnumerator() {
            for(var y=0;y<Size;y++) for(var x = 0; x < Size; x++) {
                yield return new Vector2Int(x, y);
            }
        }

        /// <summary>
        /// This method is used to get the enumerator.
        /// </summary>
        /// <returns>The key enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        
        #endregion

    }
    
}