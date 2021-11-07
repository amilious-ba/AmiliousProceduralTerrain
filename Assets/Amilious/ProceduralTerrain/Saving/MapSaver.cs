using System.IO;
using Amilious.Saving;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Saving {
    
    [HideMonoScript]
    public class MapSaver : MonoBehaviour {

        private const int SAVE_DATA_VERSION = 0;
        private const string WORLD_SETTINGS = "world";

        [SerializeField] private string saveFolder;

        [Button("ClearSaveData")]
        public void DeleteSave() {
            Directory.Delete(SavingSystem.GetSaveDirectory(saveFolder), true);
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
        
        private string GetSaveFile(Vector2Int chunkId) {
            return Path.Combine(saveFolder,$"{chunkId.x}_{chunkId.y}");
        }
        private string GetSaveFile(string fileName) {
            return Path.Combine(saveFolder,fileName);
        }

    }
    
}