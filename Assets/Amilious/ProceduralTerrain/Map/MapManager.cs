using System;
using UnityEngine;
using Amilious.Random;
using System.Diagnostics;
using Amilious.Threading;
using Sirenix.OdinInspector;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Saving;

namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This class wid used for generating terrain.  This is the brain
    /// that loads and unloads chunks from the world.
    /// </summary>
    [RequireComponent(typeof(Dispatcher), typeof(MapSaver)), HideMonoScript]
    public class MapManager : MonoBehaviour {

        #region Inspector Values
        
        [SerializeField] private MapType mapType = MapType.PreGenerated;
        [SerializeField] private bool enableSavingAndLoading = false;
        [SerializeField] private bool generateChunksAtStart;
        [SerializeField, ShowIf(nameof(generateChunksAtStart))] private int chunkPoolSize = 100;
        [SerializeField, Tooltip("This is the distance the player needs to move before the chunk will update.")]
        private float chunkUpdateThreshold = 25f;
        [SerializeField] private float colliderGenerationThreshold = 5;
        [SerializeField, Required] private string seed;
        [SerializeField] private bool applyHeight = true;
        [SerializeField, Required] private MeshSettings meshSettings;
        [SerializeField, Required] private BiomeSettings biomeSettings;
        [SerializeField] private Transform viewer;

        #endregion

        #region Events
        
        /// <summary>
        /// This event is triggered when the update cycle starts.
        /// </summary>
        public event Action OnStartUpdate;
        
        /// <summary>
        /// This event is triggered to update visible chunks.
        /// </summary>
        public event Action<int,int,int,int> OnUpdateVisible;
        
        /// <summary>
        /// This even is triggered at the end of the update cycle.
        /// </summary>
        public event Action OnEndUpdate;
        
        /// <summary>
        /// This event is triggered when collision mesh should be udated.
        /// </summary>
        public event Action OnUpdateCollisionMesh;
        
        /// <summary>
        /// This event is triggered when an update cycle is complete.
        /// </summary>
        public event Delegates.OnChunksUpdatedDelegate OnChunksUpdated;
        
        /// <summary>
        /// This event is called when a viewer enters a new chunk.
        /// </summary>
        public event Delegates.OnViewerChangedChunkDelegate OnViewerChangedChunk;

        #endregion

        #region Private Instance Variables

        private float _sqrChunkUpdateThreshold;
        private Vector2 _viewerPosition;
        private Vector2Int _viewerChunk;
        private Vector2 _oldViewerPosition;
        private float? _sqrColliderGenerationThreshold;
        private Seed? _seedStruct;
        private ChunkPool _chunkPool;
        private readonly Stopwatch _updateSW = new Stopwatch();

        #endregion
        
        #region Properties
        
        /// <summary>
        /// This property is used to get the squared distance from the viewer
        /// where colliders should be generated.
        /// </summary>
        public float SqrColliderGenerationThreshold {
            get {
                _sqrColliderGenerationThreshold??= colliderGenerationThreshold * colliderGenerationThreshold;
                return _sqrColliderGenerationThreshold.Value;
            }
        }

        /// <summary>
        /// This property is used to get the seed struct.
        /// </summary>
        public Seed Seed {
            get {
                _seedStruct ??= new Seed(seed);
                return _seedStruct.Value;
            }
        }
        
        /// <summary>
        /// This property is used to check if height values should be applied to the
        /// generated map.
        /// </summary>
        public bool ApplyHeight { get => applyHeight; }
        
        /// <summary>
        /// This property is used to get the map's <see cref="MeshSettings"/>.
        /// </summary>
        public MeshSettings MeshSettings { get => meshSettings; }
        
        /// <summary>
        /// This property is used to get the map's <see cref="BiomeSettings"/>.
        /// </summary>
        public BiomeSettings BiomeSettings { get => biomeSettings; }
        
        /// <summary>
        /// This property is used to get the map's <see cref="MapSaver"/>.
        /// </summary>
        public MapSaver MapSaver { get; private set; }

        /// <summary>
        /// This property is used to get the viewer's x and z positions as a <see cref="Vector2"/>.
        /// </summary>
        public Vector2 ViewerPositionXZ => new Vector2(viewer.position.x, viewer.position.z);
        
        /// <summary>
        /// This property is used to get the map's <see cref="MapType"/>.
        /// </summary>
        public MapType MapType { get => mapType; }
        
        /// <summary>
        /// This property is used to check if saving is enabled for the map.
        /// </summary>
        public bool SaveEnabled { get => enableSavingAndLoading; }
        
        /// <summary>
        /// This property is used to get the map's viewer.
        /// </summary>
        public Transform Viewer { get => viewer; }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to get the chunk id for the chunk
        /// at the give <see cref="Vector3"/> position.
        /// </summary>
        /// <param name="point">The position you want to get the chunk of.</param>
        /// <returns>The chunk id or coordinate at the given position.</returns>
        public Vector2Int ChunkAtPoint(Vector3 point) {
            return new Vector2Int(
                Mathf.RoundToInt(point.x / meshSettings.MeshWorldSize),
                Mathf.RoundToInt(point.z / meshSettings.MeshWorldSize)
            );
        }

        /// <summary>
        /// This method is used to get the chunk id for the chunk
        /// at the given <see cref="Vector2"/> position.
        /// </summary>
        /// <param name="point">The position you want to get the chunk of.</param>
        /// <returns>The chunk id or coordinate at the given position.</returns>
        public Vector2Int ChunkAtPoint(Vector2 point) {
            return new Vector2Int(
                Mathf.RoundToInt(point.x / meshSettings.MeshWorldSize),
                Mathf.RoundToInt(point.y / meshSettings.MeshWorldSize)
            );
        }
        
        #endregion

        #region Protected Methods
        
        /// <summary>
        /// This method is used update visible chunks.
        /// </summary>
        protected virtual void UpdateVisibleChunks() {
            _updateSW.Restart();
            OnStartUpdate?.Invoke();
            var chunks = meshSettings.ChunksVisibleInViewDistance;
            OnUpdateVisible?.Invoke(_viewerChunk.x-chunks,_viewerChunk.x+chunks,
                _viewerChunk.y-chunks,_viewerChunk.y+chunks);
            for(var xOff = - chunks; xOff <= chunks; xOff++)
            for(var yOff = -chunks; yOff <= chunks; yOff++) {
                var chunkCoord = new Vector2Int(_viewerChunk.x + xOff, _viewerChunk.y + yOff);
                _chunkPool.LoadChunk(chunkCoord);
            }
            OnEndUpdate?.Invoke();
            OnChunksUpdated?.Invoke(_chunkPool,_updateSW.ElapsedMilliseconds);
        }
        
        /// <summary>
        /// This method is the first method that is called by unity.  It
        /// may be called before all components have been created.  This
        /// will only be called once.
        /// </summary>
        protected virtual void Awake() {
            MapSaver = GetComponent<MapSaver>();
        }

        /// <summary>
        /// This method is called by unity after the awake method has been
        /// called by all the loaded components and will only be called once.
        /// </summary>
        protected virtual void Start() {
            //we use a squared threshold because it is cheaper to calculate a squared
            //distance than a normal distance.
            _chunkPool = generateChunksAtStart?
                new ChunkPool(this, chunkPoolSize):
                new ChunkPool(this);
            _sqrChunkUpdateThreshold = chunkUpdateThreshold * chunkUpdateThreshold;
            UpdateVisibleChunks();
        }

        /// <summary>
        /// This method is called on every game update.
        /// </summary>
        protected virtual void Update() {
            
            //get the player position
            _viewerPosition = ViewerPositionXZ;
            var vChunk = ChunkAtPoint(_viewerPosition);
            if(vChunk != _viewerChunk) {
                OnViewerChangedChunk?.Invoke(viewer,_viewerChunk,vChunk);
                _viewerChunk = vChunk;
            }
            
            //check for collision mesh update
            if(_viewerPosition != _oldViewerPosition) {
                OnUpdateCollisionMesh?.Invoke();
            }

            //check if visible chunks need to be updated.
            if(!((_oldViewerPosition - _viewerPosition).sqrMagnitude > _sqrChunkUpdateThreshold)) return;
            _oldViewerPosition = _viewerPosition;
            UpdateVisibleChunks();
        }

        #endregion
        
    }
}