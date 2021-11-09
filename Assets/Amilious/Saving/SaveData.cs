using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amilious.Core.Extensions;
using Amilious.Core.Serializable;
using UnityEngine;

namespace Amilious.Saving {
    
    /// <summary>
    /// This class is the save container that items can be saved
    /// or loaded into.
    /// </summary>
    [System.Serializable]
    public class SaveData {
        
        /// <summary>
        /// This constructor is used to create a new SaveData to collect data from the game.
        /// </summary>
        /// <param name="saveFile">The name of the file that is being saved or loaded.</param>
        public SaveData(string saveFile) {
            DataDictionary = new Dictionary<string, object>();
            SaveFile = saveFile;
        }

        /// <summary>
        /// This constructor is used to create a new SaveData from data that has been loaded
        /// and will be used to restore the game.
        /// </summary>
        /// <param name="saveFile">The name of the file that is being saved or loaded.</param>
        /// <param name="data">The data to restore.</param>
        public SaveData(string saveFile, Dictionary<string, object> data) {
            SaveFile = saveFile;
            DataDictionary = data;
        }

        /// <summary>
        /// This method is used to try get data from the SavedData.
        /// </summary>
        /// <param name="key">A unique key for the data that will be used to
        /// retrieve the data on loading.</param>
        /// <param name="data">The data that you want to try get.</param>
        /// <typeparam name="T">The type of the data you want to get.</typeparam>
        /// <returns>True if the data was retrieved, otherwise returns false.</returns>
        /// <seealso cref="TryRestoreTransformData"/>
        /// <remarks>Auto deserialized types: <see cref="SerializableVector2"/>,
        /// <see cref="SerializableVector3"/>, <see cref="SerializableVector2Int"/>,
        /// <see cref="SerializableVector3Int"/>, <see cref="SerializableQuaternion"/></remarks>
        public bool TryFetchData<T>(string key, out T data) {
            key = AddPrefix(key);
            data = default;
            switch(data) {
                //If trying to get a Vector2 get and convert a serializable version
                case Vector2 _ when DataDictionary.TryGetCastValue(key, out SerializableVector2 sv2): {
                    if(!(sv2.Vector2 is T value1)) return false;
                    data = value1; return true;
                }
                //If trying to get a Vector2Int get and convert a serializable version
                case Vector2Int _ when DataDictionary.TryGetCastValue(key, out SerializableVector2Int sv2): {
                    if(!(sv2.Vector2Int is T value1)) return false;
                    data = value1; return true;
                }
                //If trying to get a Vector3 get and convert a serializable version
                case Vector3 _ when DataDictionary.TryGetCastValue(key, out SerializableVector3 sv3): {
                    if(!(sv3.Vector3 is T value1)) return false;
                    data = value1; return true;
                }
                //If trying to get a Vector3Int get and convert a serializable version
                case Vector3Int  _ when DataDictionary.TryGetCastValue(key, out SerializableVector3Int sv3): {
                    if(!(sv3.Vector3Int is T value1)) return false;
                    data = value1; return true;
                }
                //If trying to get a Quaternion get and convert a serializable version
                case Quaternion  _ when DataDictionary.TryGetCastValue(key, out SerializableQuaternion sv3): {
                    if(!(sv3.Quaternion is T value1)) return false;
                    data = value1; return true;
                }
                case Vector2[] _ when DataDictionary.TryGetCastValue(key,
                        out SerializableVector2[] serializableVector2Array): {
                    var result = new Vector2[serializableVector2Array.Length];
                    for(var i = 0; i <= serializableVector2Array.Length; i++)
                        result[i] = serializableVector2Array[i].Vector2;
                    data = (T)(object)result;
                    return true;
                }
                case Vector2Int[] _ when DataDictionary.TryGetCastValue(key,
                    out SerializableVector2Int[] serializableVector2IntArray): {
                    var result = new Vector2[serializableVector2IntArray.Length];
                    for(var i = 0; i <= serializableVector2IntArray.Length; i++)
                        result[i] = serializableVector2IntArray[i].Vector2Int;
                    data = (T)(object)result;
                    return true;
                }
                case Vector3[] _ when DataDictionary.TryGetCastValue(key,
                    out SerializableVector3[] serializableVector3Array): {
                    var result = new Vector3[serializableVector3Array.Length];
                    for(var i = 0; i <= serializableVector3Array.Length; i++)
                        result[i] = serializableVector3Array[i].Vector3;
                    data = (T)(object)result;
                    return true;
                }
                case Vector3Int[] _ when DataDictionary.TryGetCastValue(key,
                    out SerializableVector3Int[] serializableVector3IntArray): {
                    var result = new Vector3Int[serializableVector3IntArray.Length];
                    for(var i = 0; i <= serializableVector3IntArray.Length; i++)
                        result[i] = serializableVector3IntArray[i].Vector3Int;
                    data = (T)(object)result;
                    return true;
                }
                case Quaternion[] _ when DataDictionary.TryGetCastValue(key,
                    out SerializableQuaternion[] serializableQuaternionArray): {
                    var result = new Quaternion[serializableQuaternionArray.Length];
                    for(var i = 0; i <= serializableQuaternionArray.Length; i++)
                        result[i] = serializableQuaternionArray[i].Quaternion;
                    data = (T)(object)result;
                    return true;
                }
            }
            if(data is Transform) {
                Debug.LogWarning("Do not use the TryFetchData for retrieving Transforms.  " +
                                 "Use the TryRestoreTransformData method instead.");
                return false;
            }
            //try to get any other type of serializable object
            if(DataDictionary.TryGetCastValue(key, out T value)) {
                data = value;
                return true;
            }
            //we were unable to get an object
            data = value;
            return false;
        }

        /// <summary>
        /// This method is used to get data.
        /// </summary>
        /// <param name="key">The name of the data you want to fetch.</param>
        /// <typeparam name="T">The type of data you are requesting.</typeparam>
        /// <returns>The requested data.</returns>
        /// <exception cref="InvalidDataException">This is thrown if the data was invalid.</exception>
        public T FetchData<T>(string key) {
            var result = TryFetchData(key, out T data);
            if(result) return data;
            throw new InvalidDataException("The data was invalid or did not exist for the given key.");
        }
        
        /// <summary>
        /// This method is used to try add data to the SaveData.
        /// </summary>
        /// <param name="key">A unique key for the data that will be used to
        /// retrieve the data on loading.</param>
        /// <param name="data">The data that you want to try add.  This data
        /// should be serializable or one of the auto serializable types.</param>
        /// <typeparam name="T">The type of the data that you want to add.</typeparam>
        /// <returns></returns>
        public bool TryStoreData<T>(string key, T data) {
            key = AddPrefix(key);
            switch(data) {
                case Vector2 v2: //convert Vector2 to serializable
                    DataDictionary[key] = v2.ToSerializable();
                    return true;
                case Vector2Int v2: //convert Vector2Int to serializable
                    DataDictionary[key] = v2.ToSerializable();
                    return true;
                case Vector3 v3: //convert Vector3 to serializable
                    DataDictionary[key] = v3.ToSerializable();
                    return true;
                case Vector3Int v3: //convert Vector3Int to serializable
                    DataDictionary[key] = v3.ToSerializable();
                    return true;
                case Quaternion q: //convert Quaternion to serializable
                    DataDictionary[key] = q.ToSerializable();
                    return true;
                case Transform t: //convert Transform to serializable
                    DataDictionary[key] = t.ToSerializable();
                    return true;
                case Vector2[] vector2Array: //convert vector2 array
                    var serializedVector2Array = new SerializableVector2[vector2Array.Length];
                    for(var i = 0; i <= vector2Array.Length; i++) 
                        serializedVector2Array[i] = vector2Array[i].ToSerializable();
                    DataDictionary[key] = serializedVector2Array;
                    return true;
                case Vector2Int[] vector2IntArray: //convert vector2Int array
                    var serializedVector2IntArray = new SerializableVector2Int[vector2IntArray.Length];
                    for(var i = 0; i <= vector2IntArray.Length; i++) 
                        serializedVector2IntArray[i] = vector2IntArray[i].ToSerializable();
                    DataDictionary[key] = serializedVector2IntArray;
                    return true;
                case Vector3[] vector3Array: //convert vector3 array
                    var serializedVector3Array = new SerializableVector3[vector3Array.Length];
                    for(var i = 0; i <= vector3Array.Length; i++) 
                        serializedVector3Array[i] = vector3Array[i].ToSerializable();
                    DataDictionary[key] = serializedVector3Array;
                    return true;
                case Vector3Int[] vector3IntArray: //convert vector3Int array
                    var serializedVector3IntArray = new SerializableVector3Int[vector3IntArray.Length];
                    for(var i = 0; i <= vector3IntArray.Length; i++) 
                        serializedVector3IntArray[i] = vector3IntArray[i].ToSerializable();
                    DataDictionary[key] = serializedVector3IntArray;
                    return true;
                case Quaternion[] quaternionArray: //convert quaternionArray
                    var serializedQuaternionArray = new SerializableQuaternion[quaternionArray.Length];
                    for(var i = 0; i <= quaternionArray.Length; i++) 
                        serializedQuaternionArray[i] = quaternionArray[i].ToSerializable();
                    DataDictionary[key] = serializedQuaternionArray;
                    return true;
            }
            if(!typeof(T).IsSerializable) {
                Debug.LogWarning("SaveData must be fully serializable!");
                return false;
            }
            DataDictionary[key] = data;
            return true;
        }

        /// <summary>
        /// This method is used to store data.
        /// </summary>
        /// <param name="key">The name of the data you want to store.</param>
        /// <param name="data">The data you want to store.</param>
        /// <typeparam name="T">The type of data you want to store.</typeparam>
        /// <exception cref="InvalidDataException">Thrown if unable to serialize the given data.</exception>
        public void StoreData<T>(string key, T data) {
            var result = TryStoreData(key, data);
            if(!result) throw new InvalidDataException("The provided data could not be serialized!");
        }
        
        /// <summary>
        /// This method is used to restore values to the given transform.
        /// </summary>
        /// <param name="key">The unique key that was used to save the data.</param>
        /// <param name="transform">The transform that you want to apply the transform
        /// data to.</param>
        /// <param name="applyLocalScale">If true the local scale will also be applied.</param>
        /// <returns>True if the transform data was set, otherwise returns false.</returns>
        public bool TryRestoreTransformData(string key, Transform transform, bool applyLocalScale = false) {
            if(!TryFetchData(AddPrefix(key), out SerializableTransform st)) return false;
            st.UpdateTransform(transform, applyLocalScale);
            return true;
        }

        /// <summary>
        /// This property is used to get the SaveData as an object dictionary.
        /// </summary>
        public Dictionary<string, object> DataDictionary { get; }

        /// <summary>
        /// This property is used to get the save file.
        /// </summary>
        public string SaveFile { get; private set; }

        /// <summary>
        /// This property is used to get the path of the save file.
        /// </summary>
        public string SavePath => SavingSystem.GetSaveFilePath(SaveFile);
        
        /// <summary>
        /// This property contains the currently set prefix.
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// This method is used to set the prefix.
        /// </summary>
        /// <param name="prefix">The string that you want to use as a prefix.</param>
        public void SetPrefix(string prefix) => Prefix = prefix;

        /// <summary>
        /// This method is used to clear the prefix.
        /// </summary>
        public void ClearPrefix() => Prefix = null;

        /// <summary>
        /// This method is used to add the prefix to the provided key.
        /// </summary>
        /// <param name="key">The key you want to add the prefix to.</param>
        /// <returns>The key proceeded by the prefix.</returns>
        private string AddPrefix(string key) => Prefix == null ? key : $"{Prefix}_{key}";

    }
}