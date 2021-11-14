using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Amilious.Core.ScriptableObjects {
    
    /// <summary>
    /// This class is used to create a scriptable object that will remember it's
    /// guid and local id.
    /// </summary>
    public class GuidScriptableObject : ScriptableObject, ISerializationCallbackReceiver {

        [FoldoutGroup("Scriptable Object Info", Mathf.Infinity)]
        [SerializeField, DisplayAsString, LabelText("Guid"), LabelWidth(50)]
        private string guid;
        [FoldoutGroup("Scriptable Object Info")]
        [SerializeField, DisplayAsString, LabelText("Local ID"), LabelWidth(50)]
        private long localId;
        
        /// <summary>
        /// This property is used to get the objects Guid.
        /// </summary>
        public string Guid { get => guid; }
        
        /// <summary>
        /// This property is used to get the objects localId.
        /// </summary>
        public long LocalId { get => localId; }

        /// <summary>
        /// This method is called before the scriptable object is serialized.  We can set the guid
        /// and the localId here to make sure that it is accurate.
        /// </summary>
        public virtual void OnBeforeSerialize() {
            #if UNITY_EDITOR
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out var guid, out long localId);
            if(guid != null && guid != this.guid) this.guid = guid;
            this.localId = localId;
            #endif
        }

        /// <summary>
        /// This method is not used but it is required by it <see cref="ISerializationCallbackReceiver"/>.
        /// </summary>
        public virtual void OnAfterDeserialize() {}
        
    }
    
}