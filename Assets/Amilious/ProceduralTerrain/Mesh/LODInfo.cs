using UnityEngine;
using Amilious.Core.Interfaces;

namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This struct is used to store level of detail information.
    /// </summary>
    [System.Serializable]
    public struct LODInfo : IDistanceProvider<float> {

        #region constants
        public const string DISTANCE_TOOLTIP = "This is the maximum distance that this lod is visible.";
        public const string LOD_TOOLTIP = "This value indicates how many verticies will be used.";
        #endregion
        
        #region Inspector Properties

        [SerializeField, Tooltip(LOD_TOOLTIP)]
        private LevelsOfDetail levelOfDetail;
        [SerializeField, Min(1), Tooltip(DISTANCE_TOOLTIP)] 
        private float distance;
        
        #endregion

        private float? _distanceSq;
        private int? _skipStep;
        
        /// <summary>
        /// This property is used to get the level of detail.
        /// </summary>
        public LevelsOfDetail LevelsOfDetail { get => levelOfDetail; }
        
        /// <summary>
        /// This property can be used to get the max distance that this level of
        /// detail will be displayed.
        /// </summary>
        public float Distance { get { return distance; } }
        
        /// <summary>
        /// This property contains the squared max distance that this level of detail
        /// will be displayed.
        /// </summary>
        public float DistanceSq {
            get {
                _distanceSq ??= distance * distance;
                return _distanceSq.Value;
            }
        }

        /// <summary>
        /// This property contains the squared distance or the distance.
        /// </summary>
        /// <param name="squared">If true the squared distance will be returned, otherwise
        /// the distance will be returned.</param>
        public float this[bool squared] => squared ? DistanceSq : Distance;

        /// <summary>
        /// This is the number of vertices that are skipped between each
        /// no border vertex for the given lod.  This property will prevent
        /// multiple boxing.
        /// </summary>
        public int SkipStep {
            get {
                _skipStep ??= (int)LevelsOfDetail;
                return _skipStep.Value;
            }
        }
    }

}