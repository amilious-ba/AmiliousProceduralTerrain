using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain {
    public class UpdateableData : ScriptableObject {

        public event Action OnValuesUpdated;
        /*[PropertySpace(SpaceBefore = 20)]
        [FoldoutGroup("Updatable Data", order:100)] public bool autoUpdate;*/
        
        public void SubscribeToUpdates(Action method) {
            OnValuesUpdated -= method;
            OnValuesUpdated += method;
        }

        public void UnsubscribeToUpdates(Action method) {
            OnValuesUpdated -= method;
        }
        
        //[FoldoutGroup("Updatable Data")][Button("Update")]
        protected virtual void OnValidate() {
           OnValuesUpdated?.Invoke();
        }
    }
}