using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This struct is used to store level of detail information.
    /// </summary>
    [System.Serializable]
    public struct LODInfo {
        
        public const int NUM_SUPPORTED_LODS = 4;

        [Tooltip("This is the level of detail.  The higher the level the lower the quality.")]
        [SerializeField,PropertyRange(0,nameof(NUM_SUPPORTED_LODS))]
        private int lod;
        [SerializeField, Min(1), Tooltip("This is the maximum distance that this lod is visible.")] 
        private float visibleDistance;

        private float? _sqrVisThreshold;
        
        /// <summary>
        /// This property can be used to get the level of detail.
        /// </summary>
        public int LOD { get { return lod; } }
        
        /// <summary>
        /// This property can be used to get the max distance that this level of
        /// detail will be displayed.
        /// </summary>
        public float VisibleDistanceThreshold { get { return visibleDistance; } }
        
        /// <summary>
        /// This property contains the squared max distance that this level of detail
        /// will be displayed.
        /// </summary>
        public float SqrVisibleDistanceThreshold {
            get {
                _sqrVisThreshold ??= visibleDistance * visibleDistance;
                return _sqrVisThreshold.Value;
            }
        }

        /// <summary>
        /// This is the number of vertices that are skipped between each
        /// no border vertex for the given lod.
        /// </summary>
        public int SkipStep { get => lod == 0 ? 1 : lod * 2; }
        
    }
    
    
}