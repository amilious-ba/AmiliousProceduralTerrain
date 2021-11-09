using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Amilious.ProceduralTerrain.Biomes.Blending;
using Amilious.ProceduralTerrain.Noise;
using Amilious.ProceduralTerrain.Sampling;
using Amilious.ProceduralTerrain.Textures;
using Amilious.Random;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Amilious.ProceduralTerrain.Biomes {
    
    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Biome Settings", order = 1), HideMonoScript]
    public class BiomeSettings : SerializedScriptableObject, IBiomeEvaluator {

        private const string TG = "tab group";

        private readonly ConcurrentDictionary<int, BiomeBlender> _biomeBlenderCache =
            new ConcurrentDictionary<int, BiomeBlender>();
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
        [SerializeField,TabGroup(TG, TAB_A)] private bool useComputeShader;
        [SerializeField,TabGroup(TG, TAB_A), ShowIf(nameof(useComputeShader))] 
        private ComputeShader computeShader;
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
        [OdinSerialize]
        #if UNITY_EDITOR
        [TableMatrix(VerticalTitle ="Heat", HorizontalTitle = "Moisture", 
            DrawElementMethod = nameof(GetBiomeDropdown))]
        #endif
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
        
        /// <summary>
        /// This method is used to blend the biome data for a chunk.
        /// </summary>
        /// <param name="size">The size of the chunk.</param>
        /// <param name="seed">The seed for the chunk.</param>
        /// <param name="position">The position of the chunk.</param>
        /// <param name="token">A cancellation token that can be used to
        /// cancel the blending.</param>
        /// <returns>A dictionary of the biomes and their weights.</returns>
        public Dictionary<int, float[,]> BlendChunk(int size, Seed seed, Vector2 position, CancellationToken token) {
            //try to get biomeBlender from cache
            var found = _biomeBlenderCache.TryGetValue(size, out var biomeBlender);
            //if the biome blender is not cached create it and cache it.
            biomeBlender??= new BiomeBlender(blendFrequency, blendRadiusPadding,size, useComputeShader);
            if(found) _biomeBlenderCache.TryAdd(size, biomeBlender);
            //preform the blend
            return biomeBlender.GetChunkBiomeWeights(seed, position, this, token);
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
        
        public int GetBiomeDropdown(Rect rect, int value) {
            var values = biomeInfo.Select(x => x.biomeId).ToList();
            var names = biomeInfo.Select(x => x.name).ToList();
            values.Add(0); names.Add("not set");
            return SirenixEditorFields.Dropdown(rect, "", value, values.ToArray(), names.ToArray());
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private GUIStyle _timerGUI;
        private string _generateHeightTime;
        private string _generateTextureTime;
        private Texture2D _previewTexture;
        
        private void GeneratePreviewTexture() {
            _stopwatch.Reset();
            _stopwatch.Start();
            var seed = new Seed(this.seed);
            var heatMap = heatMapSettings.Generate(size,seed,offset);
            var moistureMap = moistureMapSettings.Generate(size,seed,offset);
            //var baseMap = baseMapSettings.Generate(size, SeedGenerator.GetSeedInt(seed), offset);
            var biomeMap = new BiomeMap(seed, size, this);
            biomeMap.Generate(offset);
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
            //setup the shader
            MoistureMapSettings.SetComputeShaderValues(computeShader,'m',4564);
            HeatMapSettings.SetComputeShaderValues(computeShader,'h', 4564);
            if(UsingOceanMap) OceanMapSettings.SetComputeShaderValues(computeShader, 'o', 4564);
            computeShader.SetInt("moisture_values",biomeTable.GetUpperBound(0));
            computeShader.SetInt("heat_values",biomeTable.GetUpperBound(1));
            computeShader.SetBool("use_ocean",useOceanMap);
            computeShader.SetFloat("ocean_height",oceanHeight);
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
            //setup the shader
            MoistureMapSettings.SetComputeShaderValues(computeShader,'m',4564);
            HeatMapSettings.SetComputeShaderValues(computeShader,'h', 4564);
            if(UsingOceanMap) OceanMapSettings.SetComputeShaderValues(computeShader, 'o', 4564);
            computeShader.SetInt("moisture_values",biomeTable.GetUpperBound(0));
            computeShader.SetInt("heat_values",biomeTable.GetUpperBound(1));
            computeShader.SetBool("use_ocean",useOceanMap);
            computeShader.SetFloat("ocean_height",oceanHeight);
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
        public BiomeMap GenerateBiomeMap(int size, Seed seed, Vector2? position = null) {
            position??=Vector2.zero;
            var map = new BiomeMap(seed, size, this);
            map.Generate(position.Value);
            return map;
        }

        /// <summary>
        /// This method is used to get the biome at the given location.  This
        /// value is the base value without any biome blending.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="z">The y position.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>The id for the biome at that position.</returns>
        public int GetBiomeAt(float x, float z, Seed seed) {
            var heat = heatMapSettings.NoiseAtPoint(x, z, seed);
            var moisture = moistureMapSettings.NoiseAtPoint(x, z, seed);
            if(!useOceanMap) return GetBiomeId(heat, moisture);
            var baseVal = oceanMapSettings.NoiseAtPoint(x, z, seed);
            return GetBiomeId(heat, moisture, baseVal);
        }

        public bool UsingComputeShader { get=>useComputeShader; }
        
        public List<int> GetBiomesFromComputeShader(List<SamplePoint<int>> samplePoints, Seed seed) {
            var result = new List<int>();
            
            //create the buffer
            var bufferData = new ShaderBufferBiomeInfo[samplePoints.Count];
            for(var i = 0; i < samplePoints.Count; i++)
                bufferData[i]= new ShaderBufferBiomeInfo{position = new Vector2(samplePoints[i].X,samplePoints[i].Z)};
            var buffer = new ComputeBuffer(bufferData.Length, SHADER_BUFFER_SIZE);
            buffer.SetData(bufferData);
            //dispatch
            var kernel = computeShader.FindKernel("GetPointBiomes");
            computeShader.SetBuffer(kernel, "biome_info",buffer);
            computeShader.SetInt("num_points",bufferData.Length);
            computeShader.Dispatch(kernel,bufferData.Length/64,1,1);
            //fetch the results
            buffer.GetData(bufferData);
            for(var i = 0; i < samplePoints.Count; i++) {
                var biome = 0;
                if(bufferData[i].moisture_index != -1 && bufferData[i].heat_index != -1) {
                    biome = biomeTable[bufferData[i].moisture_index, bufferData[i].heat_index];
                }
                Debug.Log($"Position:{samplePoints[i].X}, {samplePoints[i].Z} biomeId: {biome}");
                if(!result.Contains(biome))result.Add(biome);
                samplePoints[i].PointData = biome;
            }
            //cleanup
            buffer.Dispose();
            return result;
        }

        public const int SHADER_BUFFER_SIZE = sizeof(float) * 2 + sizeof(int) * 2;

        public struct ShaderBufferBiomeInfo {
            public Vector2 position;
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnassignedField.Global
            public int moisture_index;
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnassignedField.Global
            public int heat_index;  
        }
    }

}