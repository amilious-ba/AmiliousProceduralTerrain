using System;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Amilious.Core;
using Amilious.Core.Extensions;
using Amilious.Threading;

namespace Amilious.Saving {
    
    /// <summary>
    /// This class is used to load and save a game state.
    /// </summary>
    public class SavingSystem : MonoBehaviour {
        
        #region Private Variables
        
        private static readonly ConcurrentDictionary<string, object> LastLoadedScene = new ConcurrentDictionary<string, object>();
        private static readonly ConcurrentDictionary<string, object> FileLocks = new ConcurrentDictionary<string, object>();
        private const string LAST_LOADED = "LastLoadedSceneIdentifier";
        private static long _actionID = 0;
        private static string _persistentDataPath;
        
        #endregion

        #region Events
        
        /// <summary>
        /// This delegate is used for when a save or load operation begins.
        /// </summary>
        /// <param name="saveFile">The name of the save file.</param>
        /// <param name="actionID">The action id that identifies the action. This
        /// is the same id that is passed when a save or load operation completes.</param>
        public delegate void SaveNotificationDelegate(string saveFile,long actionID);
        
        /// <summary>
        /// This delegate is used for when a save or load operation is completed.
        /// </summary>
        /// <param name="saveFile">The name of the save file.</param>
        /// <param name="actionID">The action id that identifies the action.  This
        /// is the same id that is passed when a save or load operation begins.</param>
        /// <param name="result"></param>
        public delegate void SaveNotificationCompleteDelegate(string saveFile,long actionID, bool result);
        
        /// <summary>
        /// This event is triggered when a save process has started.
        /// </summary>
        public static event SaveNotificationDelegate OnSaveStart;
        
        /// <summary>
        /// This event is triggered when a save process has completed.
        /// </summary>
        public static event SaveNotificationCompleteDelegate OnSaveComplete;
        
        /// <summary>
        /// This event is triggered when a load process has started.
        /// </summary>
        public static event SaveNotificationDelegate OnLoadStart;
        
        /// <summary>
        /// This event is triggered when a load process has completed.
        /// </summary>
        public static event SaveNotificationCompleteDelegate OnLoadComplete;
        
        #endregion

        /// <summary>
        /// This method is needed for set up so that worker threads can use
        /// this saving system.
        /// </summary>
        protected virtual void Awake() {
            //make sure that the saving system has a dispatcher so
            //that we can read and write from a different thread.
            var dispatcher = FindObjectOfType<Dispatcher>();
            if(dispatcher != null) return;
            gameObject.AddComponent<Dispatcher>();
            _persistentDataPath ??= Application.persistentDataPath;
        }

        /// <summary>
        /// This property is used to get the Application.persistentDataPath in a safe way.  It will
        /// get and cache the path using the main thread.
        /// </summary>
        public static string PersistentDataPath {
            get {
                if(_persistentDataPath == null) {
                    Dispatcher.Invoke(() => _persistentDataPath = Application.persistentDataPath);
                }
                return _persistentDataPath;
            }
        }

        /// <summary>
        /// This method is used to retrieve the last loaded scene.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="sceneIdentifier">The value you use to represent a scene, this
        /// could be the scene id or the AssetGUID of a AssetReference.</param>
        /// <param name="forceReSave">If true, the new value will be saved immediately without
        /// saving other changes to the game state.</param>
        public static void SetLastLoadedSceneIdentifier(string saveFile, object sceneIdentifier, bool forceReSave = false) {
            LastLoadedScene[saveFile] = sceneIdentifier;
            if(forceReSave) Save(saveFile, false);
        }

        /// <summary>
        /// This method is used to get the last loaded scene identifier.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="forceReLoad">If true, the save file will be reloaded before
        /// getting the last loaded scene identifier, otherwise will return the last cached
        /// scene identifier.</param>
        /// <returns>The last loaded scene identifier, this
        /// could be the scene id or the AssetGUID of a AssetReference.</returns>
        public static object GetLastLoadedSceneIdentifier(string saveFile, bool forceReLoad = false) {
            if(forceReLoad) Load(saveFile, false);
            return LastLoadedScene.TryGetValue(saveFile, out var value) ? value : null;
        }

        /// <summary>
        /// Get the scene identifier for the given save file.
        /// </summary>
        /// <param name="saveFile">The save file name.</param>
        /// <returns>The scene identifier for the given save file.</returns>
        public static object GetCurrentSceneIdentifier(string saveFile) {
            return LastLoadedScene.TryGetValue(saveFile,out var id) ? id : null;
        }

        /// <summary>
        /// This method is used to save the game state.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="captureState">If true, will request a new state before
        /// saving, otherwise will only update the last loaded scene identifier.</param>
        public static void Save(string saveFile, bool captureState = true) {
            var id = ++_actionID;
            OnSaveStart?.Invoke(saveFile,id);
            var state = LoadFile(saveFile);
            if(captureState) CaptureState(saveFile, state);
            state[LAST_LOADED] = 
                LastLoadedScene.TryGetValue(saveFile, out var value) ? value : null;
            var result = SaveFile(saveFile,state);
            OnSaveComplete?.Invoke(saveFile, id, result);
        }

        /// <summary>
        /// This method is used to collect the save state, before saving the data to the
        /// save file asynchronously.
        /// </summary>
        /// <param name="saveFile"></param>
        /// <param name="captureState"></param>
        public static void SaveAsync(string saveFile, bool captureState = true) {
            var id = ++_actionID;
            OnSaveStart?.Invoke(saveFile,id);
            var state = LoadFile(saveFile);
            if(captureState) CaptureState(saveFile, state);
            state[LAST_LOADED] = 
                LastLoadedScene.TryGetValue(saveFile, out var value) ? value : null;
            SaveFileAsync(saveFile,state, SaveComplete);
            void SaveComplete(bool success) {
                OnSaveComplete?.Invoke(saveFile, id, success);
            }
        }
        
        /// <summary>
        /// This method is used to load the game state.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="restoreState">If true, the loaded state will be restored
        /// to the game, otherwise only the last loaded scene will be updated.</param>
        public static void Load(string saveFile, bool restoreState = true) {
            var id = ++_actionID;
            OnLoadStart?.Invoke(saveFile,id);
            var state = LoadFile(saveFile);
            LastLoadedScene[saveFile] =
                (state.TryGetValue(LAST_LOADED, out object value)) ? value : null;
            if(restoreState) RestoreState(saveFile, state);
            OnLoadComplete?.Invoke(saveFile,id, true);
        }

        /// <summary>
        /// This method is used to asynchronously load the file data before restoring
        /// the data on the main thread is restore state is true.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="restoreState">If true, the loaded state will be restored
        /// to the game, otherwise only the last loaded scene will be updated.</param>
        public static void LoadAsync(string saveFile, bool restoreState = true) {
            var id = ++_actionID;
            OnLoadStart?.Invoke(saveFile,id);
            LoadFileAsync(saveFile, LoadedData);
            void LoadedData(Dictionary<string, object> state) {
                LastLoadedScene[saveFile] = 
                    state.TryGetValue(LAST_LOADED, out var value) ? value : null;
                if(restoreState) RestoreState(saveFile, state);
                OnLoadComplete?.Invoke(saveFile,id, true);
            }
        }

        /// <summary>
        /// This method is used to get the save path for the given save file.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path or extension.</param>
        /// <returns>The full path to the save file with the extension.</returns>
        public static string GetSaveFilePath(string saveFile) {
            return Path.Combine(PersistentDataPath, saveFile + ".sav");
        }

        /// <summary>
        /// This method is used to get the path of the save directory.
        /// </summary>
        /// <param name="subDir">An optional sub directory that you want to be
        /// added to the path.</param>
        /// <returns>The save directory path.</returns>
        public static string GetSaveDirectory(string subDir = null) {
            return subDir == null ? 
                PersistentDataPath : 
                Path.Combine(PersistentDataPath, subDir);
        }

        /// <summary>
        /// This method loads a game state from the save file.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <returns>A dictionary containing the loaded game state.</returns>
        public static Dictionary<string,object> LoadFile(string saveFile) {
            var path = GetSaveFilePath(saveFile);
            lock(GetLock(path)) { //lock the file only allowing one instance to read or write at a single time.
                if(!File.Exists(path)) return new Dictionary<string, object>();
                using var fileSteam = File.Open(path, FileMode.Open);
                var formatter = new BinaryFormatter();
                return (Dictionary<string, object>)formatter.Deserialize(fileSteam);
            }
        }

        /// <summary>
        /// This method is used to load a file asynchronously.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="callback">The method that will be called once the load is complete.</param>
        public static void LoadFileAsync(string saveFile, Action<Dictionary<string, object>> callback) {
            var path = GetSaveFilePath(saveFile);
            var future = new Future<Dictionary<string, object>>();
            future.OnError(x => {
                Debug.LogError(x.Error);
                callback(null);
            });
            future.OnSuccess(data => callback(data.Value));
            future.Process(() => {
                lock(GetLock(path)) { //lock the file only allowing one instance to read or write at a single time.
                    if(!File.Exists(path)) return new Dictionary<string, object>();
                    using var fileSteam = File.Open(path, FileMode.Open);
                    var formatter = new BinaryFormatter();
                    return (Dictionary<string, object>)formatter.Deserialize(fileSteam);
                }
            });
        }
        
        /// <summary>
        /// This method is used to get the lock object for the given save file.
        /// </summary>
        /// <param name="saveFilePath">This is the path of the save file you
        /// want to get the lock for.</param>
        /// <returns>The lock for the given save file.</returns>
        private static object GetLock(string saveFilePath) {
            if(FileLocks.TryGetValue(saveFilePath, out var fileLock)) return fileLock;
            fileLock = new object();
            FileLocks.TryAdd(saveFilePath,fileLock);
            return GetLock(saveFilePath);
        }

        /// <summary>
        /// This method saves a game state to the save file.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="state">The game state that you want to save to the
        /// save file.</param>
        public static bool SaveFile(string saveFile, Dictionary<string,object> state) {
            var path = GetSaveFilePath(saveFile);
            var tmpPath = GetSaveFilePath(Path.GetRandomFileName());
            var dir = new FileInfo(path).DirectoryName;
            lock(GetLock(path)) { //lock the file only allowing one instance to read or write at a single time.
                try {
                    //make sure the directory exists
                    if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    //write to a temp file then copy the file to the correct path
                    using var fileStream = File.Open(tmpPath, FileMode.Create);
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fileStream, state);
                    fileStream.Close();
                    File.Copy(tmpPath,path,true);
                    try { File.Delete(tmpPath); }catch { }
                    return true;
                }catch(Exception ex) {
                    Debug.LogError(ex);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// This method is used to save a file asynchronously.
        /// </summary>
        /// <param name="saveFile">The name of the save file without the path.</param>
        /// <param name="state">The game state that you want to save to the
        /// save file.</param>
        /// <param name="callback">The method that will be called when the save
        /// is complete.  This method takes a bool that will be true if the file
        /// was saved, otherwise it will return false.</param>
        public static void SaveFileAsync(string saveFile, Dictionary<string, object> state, Action<bool> callback) {
            var path = GetSaveFilePath(saveFile);
            var tmpPath = Path.GetTempPath();
            var future = new Future<bool>();
            future.OnError(x => {
                Debug.LogError(x.Error);
                callback(false);
            });
            future.OnSuccess(x => callback(x.Value));
            future.Process(()=>{
                lock(GetLock(path)) { //lock the file only allowing one instance to read or write at a single time.
                    try {
                        //write to a temp file then copy the file to the correct path
                        using var fileStream = File.Open(tmpPath, FileMode.Create);
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(fileStream, state);
                        fileStream.Close();
                        File.Copy(tmpPath,path,true);
                        try { File.Delete(tmpPath); }catch { }
                        return true;
                    }catch(Exception ex) {
                        Debug.LogError(ex);
                        return false;
                    }
                }
            });
        }

        /// <summary>
        /// This method is used to get the current game state.
        /// </summary>
        /// <param name="saveFile">The name of the current save file.</param>
        /// <param name="state">The dictionary you want to fill with the
        /// game state.</param>
        private static void CaptureState(string saveFile, IDictionary<string, object> state) {
            foreach(var saveable in FindObjectsOfType<AbstractSaveableEntity>()) {
                var saveData = new SaveData(saveFile);
                saveable.CaptureState(saveData);
                state[saveable.GetUniqueIdentifier()] = saveData.DataDictionary;
            }
        }

        /// <summary>
        /// This method is used to restore a game state.
        /// </summary>
        /// <param name="saveFile">The name of the current save file.</param>
        /// <param name="state">The game state that you want to restore.</param>
        private static void RestoreState(string saveFile, IDictionary<string, object> state) {
            foreach(var saveable in FindObjectsOfType<AbstractSaveableEntity>()) {
                if(state.TryGetCastValue(saveable.GetUniqueIdentifier(), out Dictionary<string, object> subState)) {
                    var saveData = new SaveData(saveFile, subState);
                    saveable.RestoreState(saveData);
                } else saveable.MissingState();
            }
        }

        /// <summary>
        /// This method is used to delete a save file.
        /// </summary>
        /// <param name="saveFile">The name of the save file you want
        /// to delete.</param>
        public static void Delete(string saveFile) {
            File.Delete(GetSaveFilePath(saveFile));
        }

        /// <summary>
        /// This property returns the names of the current save files.
        /// </summary>
        public IEnumerable<string> SaveFiles {
            get {
                var files = Directory.GetFiles(Application.persistentDataPath, "*.sav");
                foreach(var file in files) yield return Path.GetFileNameWithoutExtension(file);
            }
        }
        
    }
}