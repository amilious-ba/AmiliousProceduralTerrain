using UnityEngine;

namespace Amilious.Saving {
    
    /// <summary>
    /// This is an 
    /// </summary>
    public abstract class AbstractSaveable  : MonoBehaviour, ISaveable {

        [SerializeField] private bool enableSaveAndLoad = true;

        protected abstract void CapturingState(SaveData saveData);

        protected abstract void RestoringState(SaveData saveData);

        public void CaptureState(SaveData saveData) {
            if(!enableSaveAndLoad) return;
            CapturingState(saveData);
        }

        public void RestoreState(SaveData saveData) {
            if(!enableSaveAndLoad) return;
            RestoringState(saveData);
        }

        public virtual void MissingState(MissingStateType missingType) { }

        public bool IsSavingAndLoadingEnabled() => enableSaveAndLoad;
    }
}