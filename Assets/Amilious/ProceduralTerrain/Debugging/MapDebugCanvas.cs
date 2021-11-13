using Amilious.Core.Structs;
using TMPro;
using UnityEngine;
using Amilious.ProceduralTerrain.Map;

namespace Amilious.ProceduralTerrain.Debugging {
    
    /// <summary>
    /// This class is used to display map debug information.
    /// </summary>
    public class MapDebugCanvas : MonoBehaviour {

        private const int GOOD_MS_TIME = 0;
        private const int BAD_MS_TIME = 30;
        private const string MS_STRING = "{0}ms";

        [SerializeField] protected TMP_Text chunkPoolSize;
        [SerializeField] protected TMP_Text loadedChunks;
        [SerializeField] protected TMP_Text availableChunks;
        [SerializeField] protected TMP_Text lastChunkUpdateTime;
        [SerializeField] protected TMP_Text viewerChunk;

        private MapManager _mapManager;
        private long _lastMs = -1;
        private int _lastPoolSize = -1;
        private int _lastLoadChunks = -1;
        private int _lastPoolChunks = -1;

        #region Properties
        
        /// <summary>
        /// This property contains the <see cref="MapManager"/>.
        /// </summary>
        protected virtual MapManager MapManager {
            get {
                _mapManager ??= FindObjectOfType<MapManager>();
                return _mapManager;
            }
        }
        
        #endregion

        #region Protected Methods

        /// <summary>
        /// This method is called by unity before all components have been fully setup.
        /// </summary>
        protected virtual void Awake() {
            if(MapManager != null) return;
            Debug.LogWarning("The MapDebugCanvas can not function without a Map manager!");
        }

        /// <summary>
        /// This method is called by unity after all the loaded components have completed awake.
        /// </summary>
        protected virtual void Start() {
            if(MapManager == null) return;
            var vChunk = MapManager.ChunkAtPoint(MapManager.ViewerPositionXZ);
            SetText(viewerChunk,$"x:{vChunk.x}, z:{vChunk.y}");
        }

        /// <summary>
        /// This method is called when the <see cref="GameObject"/> is enabled.
        /// </summary>
        protected virtual void OnEnable() {
            if(MapManager is null) return;
            MapManager.OnChunksUpdated += ChunksUpdated;
            MapManager.OnViewerChangedChunk += ViewerChangedChunk;
        }

        /// <summary>
        /// This method is called when the <see cref="GameObject"/> is disabled.
        /// </summary>
        protected virtual void OnDisable() {
            if(MapManager == null) return;
            MapManager.OnChunksUpdated -= ChunksUpdated;
            MapManager.OnViewerChangedChunk -= ViewerChangedChunk;
        }

        /// <summary>
        /// This method is called when the viewer changes chunks.
        /// </summary>
        /// <param name="viewer">The viewer that changed chunks.</param>
        /// <param name="oldChunkId">The old chunk id.</param>
        /// <param name="newChunkId">The new chunk id.</param>
        protected virtual void ViewerChangedChunk(Transform viewer, Vector2Int oldChunkId, Vector2Int newChunkId) {
            SetText(viewerChunk,$"x:{newChunkId.x}, z:{newChunkId.y}");
        }

        /// <summary>
        /// This method is called when the chunk update cycle has completed.
        /// </summary>
        /// <param name="chunkPoolInfo">The chunk pool.</param>
        /// <param name="ms">The update time in milliseconds.</param>
        protected virtual void ChunksUpdated(PoolInfo chunkPoolInfo, long ms) {
            if(_lastPoolSize < 0 || _lastPoolSize != chunkPoolInfo.Size) {
                _lastPoolSize = chunkPoolInfo.Size;
                SetText(chunkPoolSize, _lastPoolSize);
            }
            if(_lastLoadChunks < 0 || _lastLoadChunks != chunkPoolInfo.CheckedOut) {
                _lastLoadChunks = chunkPoolInfo.CheckedOut;
                SetText(loadedChunks, _lastLoadChunks);
            }

            if(_lastPoolChunks < 0 || _lastPoolChunks != chunkPoolInfo.Available) {
                _lastLoadChunks = chunkPoolInfo.Available;
                SetText(availableChunks, _lastLoadChunks);
            }

            if(_lastMs>-1 && ms ==_lastMs) return;
            _lastMs = ms;
            SetText(lastChunkUpdateTime,string.Format(MS_STRING,_lastMs));
            //change the color based on the time
            var lerp = Mathf.InverseLerp(GOOD_MS_TIME, BAD_MS_TIME, _lastMs);
            lastChunkUpdateTime.color = Color.Lerp(Color.green, Color.red, lerp);
        }

        /// <summary>
        /// This method is used to set a fields text.
        /// </summary>
        /// <param name="field">The field you want to update the text for.</param>
        /// <param name="value">The value you want to set the field text to.</param>
        protected static void SetText(TMP_Text field, string value) {
            if(field != null) field.text = value;
        }

        /// <summary>
        /// This method is used to set a fields text.
        /// </summary>
        /// <param name="field">The field you want to update the text for.</param>
        /// <param name="value">The value you want to set the field text to.</param>
        protected static void SetText(TMP_Text field, int value) => SetText(field, value.ToString());
        
        /// <summary>
        /// This method is used to set a fields text.
        /// </summary>
        /// <param name="field">The field you want to update the text for.</param>
        /// <param name="value">The value you want to set the field text to.</param>
        protected static void SetText(TMP_Text field, long value) => SetText(field, value.ToString());
        
        #endregion
        
    }
}