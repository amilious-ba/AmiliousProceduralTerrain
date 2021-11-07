using System.Collections.Generic;
using System.Linq;
using Amilious.ProceduralTerrain.Map;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Mesh {
    
    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Mesh Settings", order = 0), HideMonoScript]
    public class MeshSettings : UpdateableData {

        
        [SerializeField]
        private float meshScale = 1f;
        [SerializeField,Tooltip("If ture the mesh will use more vertices so that it can be flat shaded.")]
        private bool useFlatShading = false;
        [SerializeField]
        private ChunkBaseSize chunkBaseSize = ChunkBaseSize.Base64X64;
        [SerializeField] private RegionSize regionSize = RegionSize.Chunks8X8;
        [SerializeField] private TerrainPaintingMode paintingMode = TerrainPaintingMode.Material;
        [SerializeField] private Material material;
        [SerializeField, Required, ValidateInput(nameof(UniqueLod),
             "Each Lod must have a unique level of detail and visible distance.")]
        [ValidateInput(nameof(ContainsValues),"You must have at least one level of detail.")]
        private LODInfo[] chunkLevelsOfDetail;
        [SerializeField, ValidateInput(nameof(ValidateColliderLOD),
             "The collider level of detail must be one of your chunk's lods.")]
        private LevelsOfDetail colliderLOD = Mesh.LevelsOfDetail.Max;

        [SerializeField, Tooltip("This distance should be greater than the max lod distance.")]
        private float chunkUnloadDistance = 600;

        private float? _meshWorldSize = null;
        private float? _maxViewDistance = null;
        private float? _maxViewDistanceSq = null;
        private float? _chunkUnloadDistanceSq = null;
        private int? _chunksVisibleInViewDistance = null;
        private int? _colliderLODIndex = null;
        
        
        public float MeshScale { get => meshScale; }
        //public int ChunkSize => useFlatShading ? flatShadedChunkSize : standardChunkSize;
        public ChunkBaseSize ChunkBaseSize { get => chunkBaseSize; }
        public RegionSize RegionSize { get => regionSize; }
        public bool UseFlatShading { get => useFlatShading; }
        public IEnumerable<LODInfo> LevelsOfDetail { get => chunkLevelsOfDetail; }
        //public int VertsPerLine => ChunkSize + 5;
        public int VertsPerLine => (int)ChunkBaseSize + 5;
        public float MeshWorldSize {
            get {
                _meshWorldSize ??= (VertsPerLine - 3) * meshScale;
                return _meshWorldSize.Value;
            }
        }
        public float MaxViewDistance {
            get {
                _maxViewDistance ??= chunkLevelsOfDetail.Max(x => x.VisibleDistanceThreshold);
                return _maxViewDistance.Value;
            }
        }
        
        public TerrainPaintingMode PaintingMode { get => paintingMode; }
        
        public float MaxViewDistanceSq {
            get {
                _maxViewDistanceSq ??= MaxViewDistance * MaxViewDistance;
                return _maxViewDistanceSq.Value;
            }
        }
        
        public float ChunkUnloadDistance { get => chunkUnloadDistance; }

        public float ChunkUnloadDistanceSq {
            get {
                _chunkUnloadDistanceSq ??= chunkUnloadDistance * chunkUnloadDistance;
                return _chunkUnloadDistanceSq.Value;
            }
        }
        
        public int ChunksVisibleInViewDistance {
            get {
                _chunksVisibleInViewDistance ??= (int)Mathf.Round(MaxViewDistance / MeshWorldSize);
                return _chunksVisibleInViewDistance.Value;
            }
        }

        public int ColliderLODIndex {
            get {
                if(_colliderLODIndex.HasValue) return _colliderLODIndex ?? -1;
                for(var i = 0; i < chunkLevelsOfDetail.Length; i++) {
                    if(chunkLevelsOfDetail[i].LevelsOfDetail != colliderLOD) continue;
                    _colliderLODIndex = i; break;
                }
                return _colliderLODIndex ?? -1;
            }
        }
        
        public Material Material { get => material; }
        
        
        #region Validation Methods

        protected override void OnValidate() {
            base.OnValidate();
            //make sure that the lod's are in order.
            if(chunkLevelsOfDetail == null) return;
            chunkLevelsOfDetail = chunkLevelsOfDetail.OrderBy(x => x.LevelsOfDetail).ToArray();
        }

        /// <summary>
        /// This method is used by the inspector to validate the lod values.
        /// </summary>
        /// <param name="lods">The levels of detail.</param>
        /// <returns>True if the lods are unique.</returns>
        private bool UniqueLod(LODInfo[] lods) {
            if(lods == null) return true;
            var uniqueLods = lods.Select(x => x.LevelsOfDetail).Distinct().Count() == lods.Length;
            var uniqueDistances = lods.Select(x => x.VisibleDistanceThreshold).Distinct().Count() == lods.Length;
            return uniqueLods && uniqueDistances;
        }

        /// <summary>
        /// This method is used by the inspector to check if the array contains a value.
        /// </summary>
        /// <param name="lods">The levels of detail you want to check.</param>
        /// <returns>True if there is at least one level of detail.</returns>
        private bool ContainsValues(LODInfo[] lods) => lods!=null && lods.Length > 0;

        /// <summary>
        /// This method is used by the inspector to validate the collider lod.
        /// </summary>
        /// <param name="lod">The lod that you want to use for the collider.</param>
        /// <returns>True if the lods contain the collider lod, otherwise false.</returns>
        private bool ValidateColliderLOD(LevelsOfDetail lod) {
            if(chunkLevelsOfDetail == null) return false;
            return chunkLevelsOfDetail.Select(x => x.LevelsOfDetail).Contains(lod);
        }
        
        #endregion


    }
    
}