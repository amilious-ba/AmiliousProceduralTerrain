using System;
using System.Collections;
using UnityEngine;
using Amilious.Random;
using System.Diagnostics;
using Amilious.Core.Structs;
using Amilious.Threading;
using Sirenix.OdinInspector;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Map.Components;
using Amilious.ProceduralTerrain.Map.Enums;
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
        [SerializeField] private bool generateChunksAtStart;
        [SerializeField, ShowIf(nameof(generateChunksAtStart))] private int chunkPoolSize = 100;
        [SerializeField, Tooltip("This is the distance the player needs to move before the chunk will update.")]
        private float chunkUpdateThreshold = 25f;
        [SerializeField, SuffixLabel("chunks")] private int colliderGenerationThreshold = 5;
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
        public event Action<ChunkRange> OnUpdateVisible;
        
        /// <summary>
        /// This even is triggered at the end of the update cycle.
        /// </summary>
        public event Action OnEndUpdate;
        
        /// <summary>
        /// This event is triggered when collision mesh should be updated.
        /// </summary>
        public event Action OnUpdateCollisionMesh;
        
        /// <summary>
        /// This event is triggered when an update cycle is complete.
        /// </summary>
        public event OnChunksUpdatedDelegate OnChunksUpdated;
        
        /// <summary>
        /// This event is called when a viewer enters a new chunk.
        /// </summary>
        public event OnViewerChangedChunkDelegate OnViewerChangedChunk;

        #endregion

        #region Private Instance Variables

        private float _sqrChunkUpdateThreshold;
        private Vector2 _viewerPositionXZ;
        private Vector3 _viewerPosition;
        private Vector2Int _viewerChunk;
        private Vector2 _oldViewerPosition;
        private DistanceValueInt? _colliderGenerationThreshold;
        private Seed? _seedStruct;
        private MapPool<Chunk> _mapPool;
        private readonly Stopwatch _updateSW = new Stopwatch();
        private int _updateChunksRadius;

        #endregion
        
        #region Properties
        
        /// <summary>
        /// This property is used to get the squared distance from the viewer
        /// where colliders should be generated.
        /// </summary>
        public DistanceValue ColliderGenerationThreshold {
            get {
                _colliderGenerationThreshold??= new DistanceValue(colliderGenerationThreshold, true);
                return _colliderGenerationThreshold.Value;
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
        /// This property is used to get the viewer's x and z positions on the last update as a <see cref="Vector2"/>.
        /// </summary>
        public Vector2 ViewerPositionXZ { get => _viewerPositionXZ; }
        
        /// <summary>
        /// This property is used to get the viewer's position on the last update.
        /// </summary>
        public Vector3 ViewerPosition { get => _viewerPosition; }
        
        public Vector2Int ViewerChunk { get => _viewerChunk; }
        
        /// <summary>
        /// This property is used to get the map's <see cref="MapType"/>.
        /// </summary>
        public MapType MapType { get => mapType; }
        
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

        /// <summary>
        /// This method is used for setting the viewer.
        /// </summary>
        /// <param name="newViewer">The viewer that you want </param>
        /// <param name="resetNewViewerPosition">If true the new viewer's position
        /// will be set to the current viewer's position.</param>
        public void SetViewer(Transform newViewer, bool resetNewViewerPosition = true) {
            if(resetNewViewerPosition && viewer!=null) {
                if(Dispatcher.IsMainThread) newViewer.position = ViewerPosition;
                else Dispatcher.Invoke(()=>newViewer.position = ViewerPosition);
            }
            viewer = newViewer;
        }
        
        #endregion

        #region Protected Methods
        
        /// <summary>
        /// This method is used update visible chunks.
        /// </summary>
        protected void UpdateVisibleChunks() {
            _updateSW.Restart();
            OnStartUpdate?.Invoke();
            OnUpdateVisible?.Invoke(new ChunkRange(_viewerChunk,_updateChunksRadius));
            StartCoroutine(SpawnChunks(_viewerChunk));
            OnEndUpdate?.Invoke();
            _updateSW.Stop();
            OnChunksUpdated?.Invoke(_mapPool.PoolInfo,_updateSW.ElapsedMilliseconds);
        }

        /// <summary>
        /// This spawns the chunks that are not spawned in the radius around the given
        /// viewer chunk.
        /// </summary>
        /// <param name="viewersChunk">The chunk the viewer is on.</param>
        protected IEnumerator SpawnChunks(Vector2Int viewersChunk) {
            for(var xOff = - _updateChunksRadius; xOff <= _updateChunksRadius; xOff++)
            for(var yOff = -_updateChunksRadius; yOff <= _updateChunksRadius; yOff++) {
                _mapPool.BarrowFromPool(new Vector2Int(viewersChunk.x + xOff, viewersChunk.y + yOff));
                yield return null;
            }
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
            _mapPool = generateChunksAtStart?
                new MapPool<Chunk>(this, chunkPoolSize):
                new MapPool<Chunk>(this);
            _sqrChunkUpdateThreshold = chunkUpdateThreshold * chunkUpdateThreshold;
            _updateChunksRadius = (int)meshSettings.MaxViewDistance[false];
            UpdateVisibleChunks();
        }

        /// <summary>
        /// This method is called on every game update.
        /// </summary>
        protected virtual void Update() {
            if(viewer == null) return;
            //get the player position
            _viewerPosition = viewer.position;
            _viewerPositionXZ = new Vector2(_viewerPosition.x, _viewerPosition.z);
            var vChunk = ChunkAtPoint(_viewerPositionXZ);
            if(vChunk != _viewerChunk) {
                OnViewerChangedChunk?.Invoke(viewer,_viewerChunk,vChunk);
                _viewerChunk = vChunk;
            }
            
            //check for collision mesh update
            if(_viewerPositionXZ != _oldViewerPosition) {
                OnUpdateCollisionMesh?.Invoke();
            }

            //check if visible chunks need to be updated.
            if((_oldViewerPosition - _viewerPositionXZ).sqrMagnitude <= _sqrChunkUpdateThreshold) return;
            _oldViewerPosition = _viewerPositionXZ;
            UpdateVisibleChunks();
        }

        #endregion

    }
}