using System.Collections.Generic;
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
            }
            if(!typeof(T).IsSerializable) {
                Debug.LogWarning("SaveData must be fully serializable!");
                return false;
            }
            DataDictionary[key] = data;
            return true;
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
        
        public string Prefix { get; private set; }

        public void SetPrefix(string prefix) => Prefix = prefix;

        public void ClearPrefix() => Prefix = null;

        private string AddPrefix(string key) => (Prefix == null) ? key : $"{Prefix}_{key}";

    }
}