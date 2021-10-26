using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Amilious.ProceduralTerrain.Biomes.Blending;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using Amilious.ProceduralTerrain.Textures;
using Amilious.Random;
using Amilious.Threading;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

namespace Amilious.ProceduralTerrain.Biomes {
    
    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Biome Settings", order = 1), HideMonoScript]
    public class BiomeSettings : SerializedScriptableObject, IBiomeEvaluator {

        private const string TG = "tab group";
        public const string PREVIEW = "Preview";
        
        #region Inspector Preview Variables
        
        [Tooltip("This is the seed that is used for random generation.")]
        [BoxGroup(PREVIEW), OnInspectorGUI("DrawPreview", append: false), SerializeField]
        private string seed = "seedless";
        [Tooltip("This is the size of the generated noise map used for the preview.")]
        [BoxGroup(PREVIEW), SerializeField]
        private int size = 100;
        [BoxGroup(PREVIEW), SerializeField]
        [Tooltip("This is the pixel multiplier used for the preview.")] 
        private int pixelMultiplier = 2;
        [BoxGroup(PREVIEW), SerializeField]
        [Tooltip("This is the offset value used for the preview.")]
        private Vector2 offset;
        [BoxGroup(PREVIEW), SerializeField] 
        [Tooltip("This value represents the map type that you want to preview.")]
        private BiomePreviewType previewType = BiomePreviewType.BiomeMap;
        [BoxGroup(PREVIEW), ShowIf(nameof(previewType),BiomePreviewType.HeatMap), SerializeField]
        [Tooltip("This color will be used as the low value color for the heat map.")]
        private Color minHeatColor = Color.white;
        [BoxGroup(PREVIEW), ShowIf(nameof(previewType),BiomePreviewType.HeatMap), SerializeField]
        [Tooltip("This color will be used as the high value color for the heat map.")]
        private Color maxHeatColor = Color.red;
        [BoxGroup(PREVIEW), ShowIf(nameof(previewType),BiomePreviewType.MoistureMap), SerializeField]
        [Tooltip("This color will be used as the low value color for the moisture map.")]
        private Color minMoistureColor = Color.white;
        [BoxGroup(PREVIEW), ShowIf(nameof(previewType),BiomePreviewType.MoistureMap), SerializeField]
        [Tooltip("This color will be used as the high value color for the moisture map.")]
        private Color maxMoistureColor = Color.blue;
        #endregion

        #region Inspector Settings Tab
        
        private const string TAB_A = "Settigns";
        [SerializeField,TabGroup(TG, TAB_A)] 
        private AbstractNoiseProvider heatMapSettings;
        [SerializeField,TabGroup(TG, TAB_A)] 
        private AbstractNoiseProvider moistureMapSettings;
        [SerializeField, TabGroup(TG, TAB_A)] 
        private bool useOceanMap;
        [SerializeField,TabGroup(TG, TAB_A), ShowIf(nameof(useOceanMap))] 
        private AbstractNoiseProvider oceanMapSettings;
        [SerializeField,TabGroup(TG, TAB_A), Range(-1,1), ShowIf(nameof(useOceanMap))] 
        private float oceanHeight = 0;
        [SerializeField, TabGroup(TG, TAB_A)] 
        private bool useBiomeBlending;
        [SerializeField,TabGroup(TG, TAB_A), ShowIf(nameof(useBiomeBlending))] 
        private float blendFrequency;
        [SerializeField,TabGroup(TG, TAB_A), ShowIf(nameof(useBiomeBlending))] 
        private float blendRadiusPadding;
        
        #endregion

        #region Inspector Biomes Tab
        
        private const string TAB_B = "Biomes";
        [TabGroup(TG, TAB_B), SerializeField] 
        private BiomeInfo oceanBiome = new BiomeInfo{name = "Ocean", biomeMapColor = Color.blue};
        [TabGroup(TG, TAB_B),SerializeField,ListDrawerSettings(CustomAddFunction = nameof(AddNewBiomeInfo), 
            Expanded = true, DraggableItems = false)] 
        private BiomeInfo[] biomeInfo;
        
        #endregion

        #region Inspector Biome Mapping Tab
        
        private const string TAB_C = "Biome Mapping";
        [TabGroup(TG, TAB_C)]
        [OdinSerialize][TableMatrix(VerticalTitle ="Heat", HorizontalTitle = "Moisture", 
            DrawElementMethod = nameof(GetBiomeDropdown))]
        // ReSharper disable once InconsistentNaming
        private int[,] biomeTable;
        [SerializeField, TabGroup(TG, TAB_C), Range(-1,1)]
        private float testHeat;
        [SerializeField, TabGroup(TG, TAB_C), Range(-1,1)]
        private float testMoisture;
        [TabGroup(TG, TAB_C), SerializeField, DisplayAsString, HideLabel, GUIColor(0f,1f,0f)]
        private string testResult = "press test to get the biome for the given levels.";
    
        #endregion
        
        public float OceanHeight { get => oceanHeight; }
        public float BlendFrequency { get => blendFrequency; }
        public float BlendRadiusPadding { get => blendRadiusPadding; }
        
        public bool UsingBiomeBlending { get => useBiomeBlending; }
        
        public bool UsingOceanMap { get => useOceanMap; }
        public AbstractNoiseProvider HeatMapSettings { get => heatMapSettings; }
        
        public AbstractNoiseProvider MoistureMapSettings { get => moistureMapSettings; }
        
        public AbstractNoiseProvider OceanMapSettings { get => oceanMapSettings; }
        
        #region Inspector Methods
        public int GetBiomeDropdown(Rect rect, int value) {
            var values = biomeInfo.Select(x => x.biomeId).ToList();
            var names = biomeInfo.Select(x => x.name).ToList();
            values.Add(0); names.Add("not set");
            return SirenixEditorFields.Dropdown(rect, "", value, values.ToArray(), names.ToArray());
        }
        
        private BiomeInfo AddNewBiomeInfo() {
            Debug.Log("reached");
            var id = 0;
            while(true) {
                id = Guid.NewGuid().GetHashCode();
                if(biomeInfo.All(info => info.biomeId != id)&&id!=0) break;
            }
            return new BiomeInfo { biomeId = id, validBiome = true};
        }

        public void TextLookUp() {
            var test = GetBiomeInfo(testHeat, testMoisture);
            testResult = $"{test.biomeId}: {test.name}";
        }
        
        #if UNITY_EDITOR

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private GUIStyle _timerGUI;
        private string _generateHeightTime;
        private string _generateTextureTime;
        private Texture2D _previewTexture;
        
        private void GeneratePreviewTexture() {
            _stopwatch.Reset();
            _stopwatch.Start();
            var heatMap = heatMapSettings.Generate(size,SeedGenerator.GetSeedInt(seed),offset);
            var moistureMap = moistureMapSettings.Generate(size,SeedGenerator.GetSeedInt(seed),offset);
            //var baseMap = baseMapSettings.Generate(size, SeedGenerator.GetSeedInt(seed), offset);
            var biomeMap = new BiomeMap(SeedGenerator.GetSeedInt(seed), size, offset, this);
            _stopwatch.Stop();
            _generateHeightTime = $"  Biome Map: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
            _stopwatch.Reset();
            _stopwatch.Start();
            _previewTexture = previewType switch {
                BiomePreviewType.BiomeMap => biomeMap.GenerateBiomeTexture(pixelMultiplier: pixelMultiplier),
                BiomePreviewType.HeatMap => heatMap.GenerateGradientTexture(minHeatColor,maxHeatColor,pixelMultiplier),
                BiomePreviewType.MoistureMap => moistureMap.GenerateGradientTexture(minMoistureColor,maxMoistureColor,pixelMultiplier),
                BiomePreviewType.BlendedBiomeMap => biomeMap.GenerateBiomeBlendTexture(pixelMultiplier:pixelMultiplier),
                _ => throw new ArgumentOutOfRangeException()
            };
            _stopwatch.Stop();
            _generateTextureTime = $"  Texture: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
        }

        
        private void DrawPreview() {
            if(_timerGUI == null) {
                _timerGUI = new GUIStyle();
                _timerGUI.normal.textColor = Color.red;
            }
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{size}X{size} Noise Sample x {pixelMultiplier}");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(_previewTexture);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label(_generateHeightTime,_timerGUI);
            GUILayout.Label(_generateTextureTime,_timerGUI);
            GUILayout.EndVertical();
        }
        
        private void OnValidate() {
            BuildLookup();
            TextLookUp();
            GeneratePreviewTexture();
        }
        
        #endif
        
        #endregion
        
        
        private readonly ConcurrentDictionary<int, BiomeInfo> _biomeLookup = 
            new ConcurrentDictionary<int, BiomeInfo>();
        private bool _builtLookup;
        private Thread _mainThread;
        private bool IsMainThread => _mainThread == Thread.CurrentThread;
        
        private void Awake() {
            _mainThread = Thread.CurrentThread;
            BuildLookup();
            #if UNITY_EDITOR
            GeneratePreviewTexture();
            #endif
        }

        private void BuildLookup() {
            //if the application is not running build lookup
            if(Application.isPlaying && _builtLookup) return;
            //need to build
            foreach(var info in biomeInfo)
                _biomeLookup.TryAdd(info.biomeId, info);
            _builtLookup = true;
        }
        
        public BiomeInfo GetBiomeInfo(int biomeId) {
            //return the ocean biome
            if(UsingOceanMap && biomeId == 0) return oceanBiome;
            //return the other biomes
            return _biomeLookup.TryGetValue(biomeId, out var value) ? value : new 
                BiomeInfo { validBiome = false, biomeId = 0, name = "Invalid", biomeMapColor = Color.magenta};
        }

        public BiomeInfo GetBiomeInfo(float heat, float moisture, float baseValue=1) => GetBiomeInfo(GetBiomeId(heat, moisture, baseValue));

        public int GetBiomeId(float heat, float moisture, float baseValue = 1) {
            //convert the -1 to 1 value into an integer value 0 to the array length minus one.
            if(useOceanMap && oceanHeight <= baseValue) return oceanBiome.biomeId;
            var x = (int)((moisture+1)*(biomeTable.GetUpperBound(0)+1)*.5f-.0001f);
            var y = (int)((heat+1)*(biomeTable.GetUpperBound(1)+1)*.5f-.0001f);
            return biomeTable[x, y];
        }

        /// <summary>
        /// This method is used to generate a biome map.
        /// </summary>
        /// <param name="size">The size of the map that you wish to generate.</param>
        /// <param name="hashedSeed">The hashed or int generation seed.</param>
        /// <param name="position">The center position of the generated map.</param>
        /// <returns>The generated biome map.</returns>
        public BiomeMap GenerateBiomeMap(int size, int hashedSeed, Vector2? position = null) {
            position??=Vector2.zero;
            return new BiomeMap(hashedSeed, size, position.Value, this);
        }

        /// <summary>
        /// This method is used to get the biome at the given location.  This
        /// value is the base value without any biome blending.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="z">The y position.</param>
        /// <param name="hashedSeed">The hashed seed.</param>
        /// <returns>The id for the biome at that position.</returns>
        public int GetBiomeAt(float x, float z, int hashedSeed) {
            var heat = heatMapSettings.NoiseAtPoint(x, z, hashedSeed);
            var moisture = moistureMapSettings.NoiseAtPoint(x, z, hashedSeed);
            if(!useOceanMap) return GetBiomeId(heat, moisture);
            var baseVal = oceanMapSettings.NoiseAtPoint(x, z, hashedSeed);
            return GetBiomeId(heat, moisture, baseVal);
        }
    }

}