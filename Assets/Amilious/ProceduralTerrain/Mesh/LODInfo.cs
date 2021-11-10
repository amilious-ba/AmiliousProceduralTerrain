using UnityEngine;

namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This struct is used to store level of detail information.
    /// </summary>
    [System.Serializable]
    public struct LODInfo {

        #region Inspector Properties

        [SerializeField]
        private LevelsOfDetail levelOfDetail;
        [SerializeField, Min(1), Tooltip("This is the maximum distance that this lod is visible.")] 
        private float visibleDistance;
        
        #endregion

        private float? _sqrVisThreshold;
        
        /// <summary>
        /// This property is used to get the level of detail.
        /// </summary>
        public LevelsOfDetail LevelsOfDetail { get => levelOfDetail; }
        
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
        public int SkipStep { get => (int)LevelsOfDetail; }
        
    }

}