using System.Collections.Generic;
using UnityEngine;

namespace Amilious.Saving {
    
    /// <summary>
    /// This is a special SaveableMonoBehavior that can be used on an entity
    /// that is persistant across scenes, but needs different data in each scene.
    /// </summary>
    public abstract class SceneSaveableMonoBehavior : MonoBehaviour, ISaveable {

        private const string KEY = "KEY";
        
        //inspector variables
        [SerializeField] private bool enableSaveAndLoad = true;

        //private variables
        private Dictionary<object, SaveData> _saveData = new Dictionary<object, SaveData>();

        /// <summary>
        /// The method is called when the game is saving.
        /// </summary>
        /// <param name="saveData">The data container that values you
        ///  want to save can be added to.</param>
        protected abstract void CapturingState(SaveData saveData);
        
        /// <summary>
        /// This method is called when the game is loading.
        /// </summary>
        /// <param name="saveData">This object contains all of the object's saved data.</param>
        protected abstract void RestoringState(SaveData saveData);
        
        /// <summary>
        /// This method can be overwritten if you need to handle missing data.
        /// </summary>
        /// <param name="missingType">The type of data that was missing.</param>
        public virtual void MissingState(MissingStateType missingType) { }
        
        /// <summary>
        /// This method is called by the saving system when the game is saving.
        /// </summary>
        /// <param name="saveData">The data container that values you
        ///  want to save can be added to.</param>
        public void CaptureState(SaveData saveData) {
            var subSaveData = new SaveData(saveData.SaveFile);
            CapturingState(subSaveData);
            _saveData[GetSceneKey(saveData.SaveFile)] = subSaveData;
            saveData.TryStoreData(KEY, _saveData);
        }

        /// <summary>
        /// This method is called by the saving system when the game is loading.
        /// </summary>
        /// <param name="saveData">The data container that contains the values
        ///  that have been saved for this object.</param>
        public void RestoreState(SaveData saveData) {
            if(saveData.TryFetchData(KEY, out Dictionary<object, SaveData> state)) {
                _saveData = state;
                var sceneKey = GetSceneKey(saveData.SaveFile);
                if(_saveData.TryGetValue(sceneKey, out SaveData subSaveData)) {
                    RestoringState(subSaveData);
                }else MissingState(MissingStateType.SceneData);
            } else MissingState(MissingStateType.SaveableEntity);
        }
        
        /// <summary>
        /// This method is used to get the current scene key.
        /// </summary>
        /// <param name="saveFile">The name of the save file.</param>
        /// <returns>The current scene's identifier object.</returns>
        private static object GetSceneKey(string saveFile) {
            return SavingSystem.GetCurrentSceneIdentifier(saveFile);
        }

        /// <summary>
        /// This property is used to check if saving and loading is enabled.
        /// </summary>
        /// <returns>True if saving and loading is enabled, otherwise
        /// returns false.</returns>
        public bool IsSavingAndLoadingEnabled() => enableSaveAndLoad;
    }
}