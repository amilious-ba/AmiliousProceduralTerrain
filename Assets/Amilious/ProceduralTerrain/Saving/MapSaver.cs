using System.IO;
using UnityEngine;
using Amilious.Saving;
#if UNITY_EDITOR
using System.Diagnostics;
#endif
using Sirenix.OdinInspector;
using Debug = UnityEngine.Debug;

namespace Amilious.ProceduralTerrain.Saving {
    
    /// <summary>
    /// This class is used to handle saving map data.
    /// </summary>
    [HideMonoScript]
    public class MapSaver : MonoBehaviour {

        private const int SAVE_DATA_VERSION = 0;
        private const string WORLD_SETTINGS = "world";

        #region Inspector
        
        [SerializeField] private bool enableSaving;
        [SerializeField, ShowIf(nameof(enableSaving))]
        private bool saveOnGenerate;
        [SerializeField, ShowIf(nameof(enableSaving))]
        private bool saveMeshData;
        /*[SerializeField, ShowIf(nameof(enableSaving))]
        private bool saveTextureData;*/
        [SerializeField, ShowIf(nameof(enableSaving))] 
        private string saveFolder;

        #endregion

        #region Properties

        /// <summary>
        /// This property is used to check if saving is enabled.
        /// </summary>
        public bool SavingEnabled { get => enableSaving; }
        
        /// <summary>
        /// This property is used to check if saving of mesh data is enabled.
        /// </summary>
        public bool SaveMeshData { get => enableSaving && saveMeshData; }
        
        /// <summary>
        /// This property is used to check if the chunk data should be saved
        /// when it is generated.
        /// </summary>
        public bool SaveOnGenerate { get => enableSaving && saveOnGenerate; }
        
        /*/// <summary>
        /// This property is used to check if the texture data should be saved
        /// when it is generated.
        /// </summary>
        public bool SaveTextureData { get => saveTextureData; }*/

        #endregion
        
        #region Public Methods

        /// <summary>
        /// This method is used to delete the current save directory.
        /// </summary>
        [Button("Clear Save Data")]
        public void DeleteSave() {
            var path = Path.Combine(Application.persistentDataPath, saveFolder);
            if(Directory.Exists(path))Directory.Delete(path, true);
            Debug.Log(Directory.Exists(path) ? "Unable to clear the save data!" : "Cleared the save data!");
        }

        #if UNITY_EDITOR
        /// <summary>
        /// This method is used to open the current save directory.
        /// </summary>
        [Button("Open Save Directory")]
        public void OpenSaveDirectory() {
            var path = Path.Combine(Application.persistentDataPath, saveFolder);
            if(!Directory.Exists(path)) path = Application.persistentDataPath;
            Process.Start(path);
        }
        #endif

        /// <summary>
        /// This method is used to save the world settings.
        /// </summary>
        /// <param name="saveData">The world settings <see cref="SaveData"/>.</param>
        /// <returns>True if the world settings were saved successfully,
        /// otherwise returns false.</returns>
        public virtual bool SaveWorldSettings(SaveData saveData) {
            return SavingSystem.SaveFile(GetSaveFile(WORLD_SETTINGS), saveData.DataDictionary);
        }

        /// <summary>
        /// This method is used to load the world settings.
        /// </summary>
        /// <param name="saveData">The <see cref="SaveData"/> containing the
        /// world settings.</param>
        /// <returns>True if the <see cref="SaveData"/> was loaded, otherwise
        /// returns false.</returns>
        public virtual bool LoadWorldSettings(out SaveData saveData) {
            var saveFile = GetSaveFile(WORLD_SETTINGS);
            var rawData = SavingSystem.LoadFile(saveFile);
            saveData = new SaveData(saveFile, rawData);
            return rawData.Keys.Count != 0;
        }

        /// <summary>
        /// This method is used to create a <see cref="SaveData"/> for the given chunkId.
        /// </summary>
        /// <param name="chunkId">The chunk id you want to use with the <see cref="SaveData"/>.</param>
        /// <returns>A new <see cref="SaveData"/> created for the chunk with the given chunkId.</returns>
        public virtual SaveData NewChunkSaveData(Vector2Int chunkId) {
            return new SaveData(GetSaveFile(chunkId));
        }

        /// <summary>
        /// This method is used to save the chunk with the provided values.
        /// </summary>
        /// <param name="chunkId">The id of the chunk that you want to save.</param>
        /// <param name="saveData">The <see cref="SaveData"/> for the given chunk.</param>
        /// <returns>True if the chunk <see cref="SaveData"/> was saved, otherwise
        /// returns false.</returns>
        public virtual bool SaveData(Vector2Int chunkId, SaveData saveData) {
            return SavingSystem.SaveFile(GetSaveFile(chunkId), saveData.DataDictionary);
        }

        /// <summary>
        /// This method is used to load the chunk with the provided values.
        /// </summary>
        /// <param name="chunkId">The id of the chunk that you want to save.</param>
        /// <param name="saveData">The <see cref="SaveData"/> for the given chunk.</param>
        /// <returns>True if the chunk <see cref="SaveData"/> was loaded, otherwise
        /// returns false.</returns>
        public virtual bool LoadData(Vector2Int chunkId, out SaveData saveData) {
            var saveFile = GetSaveFile(chunkId);
            var rawData = SavingSystem.LoadFile(saveFile);
            saveData = new SaveData(saveFile, rawData);
            return rawData.Keys.Count != 0;
        }
        
        #endregion

        #region Protected Methods
        
        /// <summary>
        /// This method is use to get the save file for the given chunkId.
        /// </summary>
        /// <param name="chunkId">The chunk id for the save file.</param>
        /// <returns>The save file for the given chunkId.</returns>
        protected virtual string GetSaveFile(Vector2Int chunkId) {
            return Path.Combine(saveFolder,$"{chunkId.x}_{chunkId.y}");
        }
        
        /// <summary>
        /// This method is used to get the save file for the given name.
        /// </summary>
        /// <param name="fileName">The name of the save file.</param>
        /// <returns>The save file for the given name.</returns>
        protected virtual string GetSaveFile(string fileName) {
            return Path.Combine(saveFolder,fileName);
        }
        
        #endregion

    }
    
}