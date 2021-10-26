using System.Collections.Generic;
using Amilious.Core.Extensions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Amilious.Saving {
    
    [ExecuteAlways]
    public class SaveableEntity  : AbstractSaveableEntity {

        [SerializeField] private string uniqueIdentifier = string.Empty;

        private static readonly Dictionary<string, SaveableEntity> GlobalLookup = new Dictionary<string, SaveableEntity>();

        public override string GetUniqueIdentifier() {
            return uniqueIdentifier;
        }

        public override void CaptureState(SaveData saveData) {
            var state = new Dictionary<string, object>();
            foreach(var saveable in GetComponents<ISaveable>()) {
                if(!saveable.IsSavingAndLoadingEnabled()) continue;
                var subSaveData = new SaveData(saveData.SaveFile);
                state[saveable.GetType().ToString()] = subSaveData;
                saveable.CaptureState(subSaveData);
            }
        }

        public override void RestoreState(SaveData saveData) {
            //var state = (Dictionary<string, object>) stateRaw;
            foreach(var saveable in GetComponents<ISaveable>()) {
                if(!saveable.IsSavingAndLoadingEnabled()) continue;
                var key = saveable.GetType().ToString();
                if(saveData.DataDictionary.TryGetCastValue(key, out SaveData subSaveData)){
                    saveable.RestoreState(subSaveData);
                } else saveable.MissingState(MissingStateType.SaveableComponent);
            }
        }

        public override void MissingState() {
            foreach(var saveable in GetComponents<ISaveable>()) {
                if(!saveable.IsSavingAndLoadingEnabled()) continue;
                saveable.MissingState(MissingStateType.SaveableEntity);
            }
        }

        #if UNITY_EDITOR
        public void Update() {
            if(Application.IsPlaying(gameObject)) return;
            if(string.IsNullOrEmpty(gameObject.scene.path)) return;
            var serializedObject = new SerializedObject(this);
            var property = serializedObject.FindProperty(nameof(uniqueIdentifier));
            if(string.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue)) {
                property.stringValue = System.Guid.NewGuid().ToString();
                serializedObject.ApplyModifiedProperties();
            }
            GlobalLookup[property.stringValue] = this;
        }
        #endif

        private bool IsUnique(string candidate) {
            if(!GlobalLookup.TryGetValue(candidate, out var value)) return true;
            return (value == this || value == null || value.GetUniqueIdentifier()!=candidate);
        }
    }
    
}