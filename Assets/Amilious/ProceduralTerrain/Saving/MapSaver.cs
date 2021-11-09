using System;
using System.Diagnostics;
using System.IO;
using Amilious.Saving;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Amilious.ProceduralTerrain.Saving {
    
    [HideMonoScript]
    public class MapSaver : MonoBehaviour {

        private const int SAVE_DATA_VERSION = 0;
        private const string WORLD_SETTINGS = "world";

        [SerializeField] private bool enableSaving;
        [SerializeField, ShowIf(nameof(enableSaving))]
        private bool saveTextureData;
        [SerializeField, ShowIf(nameof(enableSaving))] 
        private string saveFolder;

        public bool SavingEnabled { get => enableSaving; }

        [Button("Clear Save Data")]
        public void DeleteSave() {
            var path = Path.Combine(Application.persistentDataPath, saveFolder);
            if(Directory.Exists(path))Directory.Delete(path, true);
            Debug.Log(Directory.Exists(path) ? "Unable to clear the save data!" : "Cleared the save data!");
        }

        [Button("Open Save Directory")]
        public void OpenSaveDirectory() {
            var path = Path.Combine(Application.persistentDataPath, saveFolder);
            if(!Directory.Exists(path)) path = Application.persistentDataPath;
            Process.Start(path);
        }

        public bool SaveWorldSettings(SaveData saveData) {
            return SavingSystem.SaveFile(GetSaveFile(WORLD_SETTINGS), saveData.DataDictionary);
        }

        public bool LoadWorldSettings(out SaveData saveData) {
            var saveFile = GetSaveFile(WORLD_SETTINGS);
            var rawData = SavingSystem.LoadFile(saveFile);
            saveData = new SaveData(saveFile, rawData);
            return rawData.Keys.Count != 0;
        }

        public SaveData NewChunkSaveData(Vector2Int chunkId) {
            return new SaveData(GetSaveFile(chunkId));
        }

        public bool SaveData(Vector2Int chunkId, SaveData saveData) {
            return SavingSystem.SaveFile(GetSaveFile(chunkId), saveData.DataDictionary);
        }

        public bool LoadData(Vector2Int chunkId, out SaveData saveData) {
            var saveFile = GetSaveFile(chunkId);
            var rawData = SavingSystem.LoadFile(saveFile);
            saveData = new SaveData(saveFile, rawData);
            return rawData.Keys.Count != 0;
        }
        
        protected virtual string GetSaveFile(Vector2Int chunkId) {
            return Path.Combine(saveFolder,$"{chunkId.x}_{chunkId.y}");
        }
        protected virtual string GetSaveFile(string fileName) {
            return Path.Combine(saveFolder,fileName);
        }

    }
    
}