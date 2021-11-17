using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Amilious.Core.Structs;
using Amilious.ProceduralTerrain.Map;
using UnityEngine.Serialization;

namespace Amilious.ProceduralTerrain.Mesh {
    
    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Mesh Settings", order = 0), HideMonoScript]
    public class MeshSettings : ScriptableObject {

        #region Constants

        private const string INVALID_COLLIDER_LOD =
            "The collider's level of detail must be one of your chunk level of details";
        
        #endregion
        
        #region Inspector Values
        
        [SerializeField]
        private ChunkBaseSize chunkBaseSize = ChunkBaseSize.Base64X64;
        [SerializeField] private RegionSize regionSize = RegionSize.Chunks8X8;
        [SerializeField] private TerrainPaintingMode paintingMode = TerrainPaintingMode.Material;
        [SerializeField] private Material material;[SerializeField]
        private float meshScale = 1f;
        
        [SerializeField,Tooltip("If ture the mesh will use more vertices so that it can be flat shaded.")]
        private bool useFlatShading;
        [SerializeField] 
        private bool bakeCollisionMeshes;
        [SerializeField]
        private bool calculateMeshDataOnLoad;
        [Space(20), SerializeField, Required, TableList(AlwaysExpanded = true)]
        [ValidateInput(nameof(UniqueLod), "Each Lod must have a unique level of detail and visible distance.")]
        [ValidateInput(nameof(ContainsValues),"You must have at least one level of detail.")]
        private LODInfo[] chunkLevelsOfDetail;
        [SerializeField, Tooltip("This distance should be greater than the max lod distance.")]
        private float unloadDistance = 600;
        [SerializeField, ValidateInput(nameof(ValidateColliderLOD),INVALID_COLLIDER_LOD)]
        private LevelsOfDetail colliderLOD = Mesh.LevelsOfDetail.Max;
        #endregion
        
        #region Instance Variables
        
        private float? _meshWorldSize;
        private DistanceValue? _maxViewDistance;
        private DistanceValue? _unloadDistance;
        private int? _chunksVisibleInViewDistance;
        private int? _colliderLODIndex;

        #endregion
        
        #region Properties
        
        /// <summary>
        /// If this property is true, the mesh data should be generated on the
        /// same thread that the biome map is generated on.
        /// </summary>
        public bool CalculateMeshDataOnLoad { get => calculateMeshDataOnLoad; }
        
        /// <summary>
        /// This property is used to get the scale of the mesh.
        /// </summary>
        public float MeshScale { get => meshScale; }
        
        /// <summary>
        /// This property is used to get the chunk's base size.
        /// </summary>
        public ChunkBaseSize ChunkBaseSize { get => chunkBaseSize; }
        
        /// <summary>
        /// This property is used to get the chunk region size.
        /// </summary>
        public RegionSize RegionSize { get => regionSize; }
        
        /// <summary>
        /// This property is used to check if the mesh should be using flat shading.
        /// </summary>
        public bool UseFlatShading { get => useFlatShading; }
        
        /// <summary>
        /// This property contains true if collision meshes should be baked.
        /// </summary>
        public bool BakeCollisionMeshes { get => bakeCollisionMeshes; }
        
        /// <summary>
        /// This property is used to get the levels of detail.
        /// </summary>
        public IEnumerable<LODInfo> LevelsOfDetail { get => chunkLevelsOfDetail; }
        
        /// <summary>
        /// This property is used to get the number of vertices per line.
        /// </summary>
        public int VertsPerLine => (int)ChunkBaseSize + 5;
        
        /// <summary>
        /// This property is used to get the meshes world size.
        /// </summary>
        public float MeshWorldSize {
            get {
                _meshWorldSize ??= (VertsPerLine - 3) * meshScale;
                return _meshWorldSize.Value;
            }
        }
        
        /// <summary>
        /// This property is used to get the max view distance.
        /// </summary>
        public DistanceValue MaxViewDistance {
            get {
                _maxViewDistance ??= new DistanceValue(chunkLevelsOfDetail.Max(x => x.Distance),true);
                return _maxViewDistance.Value;
            }
        }
        
        /// <summary>
        /// This property is used to know how the terrains should be painted.
        /// </summary>
        public TerrainPaintingMode PaintingMode { get => paintingMode; }
        
        /// <summary>
        /// This property contains the distance that chunks should be unloaded.
        /// </summary>
        public DistanceValue UnloadDistance {
            get {
                _unloadDistance ??= new DistanceValue(unloadDistance, true);
                return _unloadDistance.Value;
            }
        }
        
        /// <summary>
        /// This property is used to get the number of chunks visible in the view distance.
        /// </summary>
        public int ChunksVisibleInViewDistance {
            get {
                _chunksVisibleInViewDistance ??= (int)Mathf.Round(MaxViewDistance[false] / MeshWorldSize);
                return _chunksVisibleInViewDistance.Value;
            }
        }

        /// <summary>
        /// This property is used to get the level of detail that should be used for
        /// the colliders.
        /// </summary>
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
        
        /// <summary>
        /// This property contains the material that should be applied to all terrain meshes.
        /// </summary>
        public Material Material { get => material; }
        
        #endregion
        
        #region Validation Methods

        /// <summary>
        /// This method is called when a <see cref="GameObject"/> is modified
        /// in the unity editor.
        /// </summary>
        protected void OnValidate() {
            //make sure that the level of details are in order.
            chunkLevelsOfDetail = chunkLevelsOfDetail?.OrderBy(x => x.LevelsOfDetail).ToArray();
        }

        /// <summary>
        /// This method is used by the inspector to validate the lod values.
        /// </summary>
        /// <param name="lods">The levels of detail.</param>
        /// <returns>True if the lods are unique.</returns>
        protected virtual bool UniqueLod(LODInfo[] lods) {
            if(lods == null) return true;
            var uniqueLods = lods.Select(x => x.LevelsOfDetail).Distinct().Count() == lods.Length;
            var uniqueDistances = lods.Select(x => x.Distance).Distinct().Count() == lods.Length;
            return uniqueLods && uniqueDistances;
        }

        /// <summary>
        /// This method is used by the inspector to check if the array contains a value.
        /// </summary>
        /// <param name="lods">The levels of detail you want to check.</param>
        /// <returns>True if there is at least one level of detail.</returns>
        protected virtual bool ContainsValues(LODInfo[] lods) => lods!=null && lods.Length > 0;

        /// <summary>
        /// This method is used by the inspector to validate the collider lod.
        /// </summary>
        /// <param name="lod">The lod that you want to use for the collider.</param>
        /// <returns>True if the lods contain the collider lod, otherwise false.</returns>
        protected virtual bool ValidateColliderLOD(LevelsOfDetail lod) {
            return chunkLevelsOfDetail != null && chunkLevelsOfDetail.Select(x => x.LevelsOfDetail).Contains(lod);
        }
        
        #endregion


    }
    
}