using System;
using System.Globalization;
using Amilious.ProceduralTerrain.Map;
using TMPro;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Debugging {
    public class MapDebugCanvas : MonoBehaviour {

        [SerializeField] private TMP_Text chunkPoolSize;
        [SerializeField] private TMP_Text loadedChunks;
        [SerializeField] private TMP_Text availableChunks;
        [SerializeField] private TMP_Text lastChunkUpdateTime;
        [SerializeField] private TMP_Text viewerChunk;

        private MapManager _mapManager;

        private MapManager MapManager {
            get {
                _mapManager ??= FindObjectOfType<MapManager>();
                return _mapManager;
            }
        }

        private void Awake() {
            if(MapManager != null) return;
            Debug.LogWarning("The MapDebugCanvas can not function without a Map manager!");
        }

        private void Start() {
            if(MapManager == null) return;
            var vChunk = MapManager.ChunkAtPoint(MapManager.ViewerPositionXZ);
            SetText(viewerChunk,$"x:{vChunk.x}, z:{vChunk.y}");
        }

        private void OnEnable() {
            if(MapManager is null) return;
            MapManager.OnChunksUpdated += ChunksUpdated;
            MapManager.OnViewerChangedChunk += ViewerChangedChunk;
        }

        private void OnDisable() {
            if(MapManager == null) return;
            MapManager.OnChunksUpdated -= ChunksUpdated;
            MapManager.OnViewerChangedChunk -= ViewerChangedChunk;
        }

        private void ViewerChangedChunk(Transform viewer, Vector2Int oldChunkId, Vector2Int newChunkId) {
            SetText(viewerChunk,$"x:{newChunkId.x}, z:{newChunkId.y}");
        }

        private void ChunksUpdated(ChunkPool chunkPool, long ms) {
            SetText(chunkPoolSize,chunkPool.PoolSize);
            SetText(loadedChunks,chunkPool.NumberOfLoadedChunks);
            SetText(availableChunks, chunkPool.NumberOfUnusedChunks);
            SetText(lastChunkUpdateTime,ms);
        }

        private void SetText(TMP_Text field, string value) {
            if(field != null) field.text = value;
        }

        private void SetText(TMP_Text field, int value) => SetText(field, value.ToString());
        private void SetText(TMP_Text field, long value) => SetText(field, value.ToString());
    }
}