using System;
using System.Linq;
using UnityEngine;
using Amilious.Random;
using System.Threading;
using System.Diagnostics;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Amilious.ProceduralTerrain.Noise;
using Amilious.ProceduralTerrain.Sampling;
using Amilious.ProceduralTerrain.Textures;
using Amilious.ProceduralTerrain.Biomes.Blending;
using Amilious.ProceduralTerrain.Biomes.Enums;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace Amilious.ProceduralTerrain.Biomes {
    
    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Biome Settings", order = 1), HideMonoScript]
    public class BiomeSettings : SerializedScriptableObject, IBiomeEvaluator {

        private const string TG = "tab group";
        public const string PREVIEW = "Preview";
        private const string TAB_A = "Settigns";
        private const string TAB_B = "Biomes";
        private const string TAB_C = "Biome Mapping";
        public const int SHADER_BUFFER_SIZE = sizeof(float) * 2 + sizeof(int) * 2;
        private const string BIOME_LOOKUP_TEST = "{0}: {1}";

        #region Inspector Preview Variables
        #if UNITY_EDITOR
        [Tooltip("This is the seed that is used for random generation.")]
        [BoxGroup(PREVIEW), LabelText("Seed"), OnInspectorGUI("DrawPreview", append: false), SerializeField]
        [InfoBox("The biome map has not been set up correctly.", InfoMessageType.Error, nameof(InvalidBiomeMapping))]
        private string pvSeed = "seedless";
        [Tooltip("This is the size of the generated noise map used for the preview.")]
        [BoxGroup(PREVIEW), LabelText("Size"), SerializeField]
        private int pvSize = 100;
        [BoxGroup(PREVIEW), LabelText("Pixel Multiplier"), SerializeField]
        [Tooltip("This is the pixel multiplier used for the preview.")] 
        private int pvPixelMultiplier = 2;
        [BoxGroup(PREVIEW), LabelText("Offset"), SerializeField]
        [Tooltip("This is the offset value used for the preview.")]
        private Vector2 pvOffset;
        [BoxGroup(PREVIEW), SerializeField] 
        [Tooltip("This value represents the map type that you want to preview.")]
        private BiomePreviewType previewType = BiomePreviewType.BiomeMap;
        [BoxGroup(PREVIEW), LabelText("Min Heat Color"), ShowIf(nameof(previewType),BiomePreviewType.HeatMap), SerializeField]
        [Tooltip("This color will be used as the low value color for the heat map.")]
        private Color pvMinHeatColor = Color.white;
        [BoxGroup(PREVIEW), LabelText("Max Heat Color"), ShowIf(nameof(previewType),BiomePreviewType.HeatMap), SerializeField]
        [Tooltip("This color will be used as the high value color for the heat map.")]
        private Color pvMaxHeatColor = Color.red;
        [BoxGroup(PREVIEW), LabelText("Min Moisture Color"), ShowIf(nameof(previewType),BiomePreviewType.MoistureMap), SerializeField]
        [Tooltip("This color will be used as the low value color for the moisture map.")]
        private Color pvMinMoistureColor = Color.white;
        [BoxGroup(PREVIEW), LabelText("Max Moisture Color"), ShowIf(nameof(previewType),BiomePreviewType.MoistureMap), SerializeField]
        [Tooltip("This color will be used as the high value color for the moisture map.")]
        private Color pvMaxMoistureColor = Color.blue;
        #endif
        #endregion

        #region Inspector Settings Tab
        [SerializeField,TabGroup(TG, TAB_A)]
        private AbstractNoiseProvider heatMapSettings;
        [SerializeField,TabGroup(TG, TAB_A)] 
        private AbstractNoiseProvider moistureMapSettings;
        [SerializeField, TabGroup(TG, TAB_A)] 
        private bool useOceanMap;
        [SerializeField,TabGroup(TG, TAB_A), ShowIf(nameof(useOceanMap))] 
        private AbstractNoiseProvider oceanMapSettings;
        [SerializeField,TabGroup(TG, TAB_A), Range(-1,1), ShowIf(nameof(useOceanMap))] 
        private float oceanHeight;
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
        
        
        #endregion

        #region Inspector Biome Mapping Tab
        
        [TabGroup(TG, TAB_C)]
        [OdinSerialize]
        #if UNITY_EDITOR
        [TableMatrix(VerticalTitle = "<-Heat", HorizontalTitle = "Moisture->",
            DrawElementMethod = nameof(DrawTableItem))]
        #endif
        
        // ReSharper disable once InconsistentNaming
        private Biome[,] biomeTable;
        [TabGroup(TG, TAB_C), SerializeField] 
        private Biome oceanBiome;
        #if UNITY_EDITOR
        [SerializeField, TabGroup(TG, TAB_C), Range(-1,1), Title("Test Result")]
        private float testHeat;
        [SerializeField, TabGroup(TG, TAB_C), Range(-1,1)]
        private float testMoisture;
        [TabGroup(TG, TAB_C), SerializeField, DisplayAsString, HideLabel, GUIColor(0f,1f,0f)]
        // ReSharper disable once NotAccessedField.Local
        private string previewTestResult = "press test to get the biome for the given levels.";
        #endif
        #endregion
        
        
        #region Private instace variables

        private readonly ConcurrentDictionary<int, BiomeBlender> _biomeBlenderCache =
            new ConcurrentDictionary<int, BiomeBlender>();
        private readonly ConcurrentDictionary<string, Biome> _biomeLookup = 
            new ConcurrentDictionary<string, Biome>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private GUIStyle _timerGUI;
        private string _generateHeightTime;
        private string _generateTextureTime;
        private Texture2D _previewTexture;
        private bool _builtLookup;

        #endregion
        
        #region Properties
        
        public float OceanHeight { get => oceanHeight; }
        public float BlendFrequency { get => blendFrequency; }
        public float BlendRadiusPadding { get => blendRadiusPadding; }

        public bool UsingBiomeBlending { get => useBiomeBlending; }
        
        public bool UsingOceanMap { get => useOceanMap; }
        public AbstractNoiseProvider HeatMapSettings { get => heatMapSettings; }
        
        public AbstractNoiseProvider MoistureMapSettings { get => moistureMapSettings; }
        
        public AbstractNoiseProvider OceanMapSettings { get => oceanMapSettings; }

        public bool UsingComputeShader { get=>useComputeShader; }
        
        #endregion
        
        #region Inspector Methods
        
        #if UNITY_EDITOR
        
        public Biome DrawTableItem(Rect rect, Biome biome) {
            SirenixEditorFields.UnityObjectField(rect, biome,typeof(Biome),false);
            return biome;
        }
        
        /// <summary>
        /// This method is used to display the preview biome.
        /// </summary>
        public void PreviewLoopUpBiome() {
            var test = GetBiome(testHeat, testMoisture);
            previewTestResult = string.Format(BIOME_LOOKUP_TEST,test==null?"null":test.Guid, test==null?"null":test.name);
        }

        public bool InvalidBiomeMapping {
            get {
                if(biomeTable == null) return true;
                if(biomeTable.Cast<Biome>().Any(biome => biome is null)) return true;
                if(oceanBiome == null) return true;
                return false;
            }
        }
        
        /// <summary>
        /// This method is used to generate the preview texture.
        /// </summary>
        private void GeneratePreviewTexture() {
            _stopwatch.Reset();
            _stopwatch.Start();
            var seed = new Seed(this.pvSeed);
            var heatMap = heatMapSettings.Generate(pvSize,seed,pvOffset);
            var moistureMap = moistureMapSettings.Generate(pvSize,seed,pvOffset);
            //var baseMap = baseMapSettings.Generate(size, SeedGenerator.GetSeedInt(seed), offset);
            var biomeMap = new BiomeMap(seed, pvSize, this);
            biomeMap.Generate(pvOffset);
            _stopwatch.Stop();
            _generateHeightTime = $"  Biome Map: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
            _stopwatch.Reset();
            _stopwatch.Start();
            _previewTexture = previewType switch {
                BiomePreviewType.BiomeMap => biomeMap.GenerateBiomeTexture(pixelMultiplier: pvPixelMultiplier),
                BiomePreviewType.HeatMap => heatMap.GenerateGradientTexture(pvMinHeatColor,pvMaxHeatColor,pvPixelMultiplier),
                BiomePreviewType.MoistureMap => moistureMap.GenerateGradientTexture(pvMinMoistureColor,pvMaxMoistureColor,pvPixelMultiplier),
                BiomePreviewType.BlendedBiomeMap => biomeMap.GenerateBiomeBlendTexture(pixelMultiplier:pvPixelMultiplier),
                _ => throw new ArgumentOutOfRangeException()
            };
            _stopwatch.Stop();
            _generateTextureTime = $"  Texture: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
        }
        
        /// <summary>
        /// This method is used to draw a preview texture in the inspector.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private void DrawPreview() {
            if(InvalidBiomeMapping) return;
            _timerGUI ??= new GUIStyle { normal = { textColor = Color.red } };
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{pvSize}X{pvSize} Noise Sample x {pvPixelMultiplier}");
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
        
        /// <summary>
        /// This method is called when the unity editor validates this gameObject.
        /// </summary>
        private void OnValidate() {
            BuildLookup();
            PreviewLoopUpBiome();
            GeneratePreviewTexture();
            //setup the shader
            var seedStruct = new Seed(4564);
            MoistureMapSettings.SetComputeShaderValues(computeShader,'m',seedStruct);
            HeatMapSettings.SetComputeShaderValues(computeShader,'h',seedStruct);
            if(UsingOceanMap) OceanMapSettings.SetComputeShaderValues(computeShader, 'o',seedStruct);
            computeShader.SetInt("moisture_values",biomeTable.GetUpperBound(0));
            computeShader.SetInt("heat_values",biomeTable.GetUpperBound(1));
            computeShader.SetBool("use_ocean",useOceanMap);
            computeShader.SetFloat("ocean_height",oceanHeight);
        }
        
        #endif
        
        #endregion
        
        /// <summary>
        /// This is the first method called by unity.  All components may not be ready
        /// for use.  This is only called once.
        /// </summary>
        protected virtual void Awake() {
            BuildLookup();
            #if UNITY_EDITOR
            GeneratePreviewTexture();
            #endif 
            //setup the shader
            var seedStruct = new Seed(4564);
            MoistureMapSettings.SetComputeShaderValues(computeShader,'m',seedStruct);
            HeatMapSettings.SetComputeShaderValues(computeShader,'h', seedStruct);
            if(UsingOceanMap) OceanMapSettings.SetComputeShaderValues(computeShader, 'o', seedStruct);
            computeShader.SetInt("moisture_values",biomeTable.GetUpperBound(0));
            computeShader.SetInt("heat_values",biomeTable.GetUpperBound(1));
            computeShader.SetBool("use_ocean",useOceanMap);
            computeShader.SetFloat("ocean_height",oceanHeight);
        }
        
        /// <summary>
        /// This method is used to blend the biome data for a chunk.
        /// </summary>
        /// <param name="size">The size of the chunk.</param>
        /// <param name="seed">The seed for the chunk.</param>
        /// <param name="position">The position of the chunk.</param>
        /// <param name="token">A cancellation token that can be used to
        /// cancel the blending.</param>
        /// <returns>A dictionary of the biomes and their weights.</returns>
        public virtual Dictionary<string, float[,]> BlendChunk(int size, Seed seed, Vector2 position, CancellationToken token) {
            //try to get biomeBlender from cache
            var found = _biomeBlenderCache.TryGetValue(size, out var biomeBlender);
            //if the biome blender is not cached create it and cache it.
            biomeBlender??= new BiomeBlender(blendFrequency, blendRadiusPadding,size, useComputeShader);
            if(found) _biomeBlenderCache.TryAdd(size, biomeBlender);
            //preform the blend
            return biomeBlender.GetChunkBiomeWeights(seed, position, this, token);
        }
        
        /// <summary>
        /// This method is used to build a lookup table.
        /// </summary>
        protected virtual void BuildLookup() {
            //if the application is not running build lookup
            if(Application.isPlaying && _builtLookup) return;
            //need to build
            foreach(var biome in biomeTable)
                if(biome!=null)_biomeLookup.TryAdd(biome.Guid, biome);
            _builtLookup = true;
        }
        
        /// <summary>
        /// This method is used to get the biome info for the biome with
        /// the given id.
        /// </summary>
        /// <param name="biomeGuid">The id of the biome you want to get the info for.</param>
        /// <returns>The biome info for the id that was provided, otherwise returns
        /// a biome info that is marked invalid.</returns>
        public virtual Biome GetBiome(string biomeGuid) {
            //return the ocean biome
            if(UsingOceanMap && oceanBiome == null || biomeGuid == oceanBiome.Guid) return oceanBiome;
            //return the other biomes
            return _biomeLookup.TryGetValue(biomeGuid, out var value) ? value : null;
        }

        /// <summary>
        /// This method is used to get the biome info based on the passed values.
        /// </summary>
        /// <param name="heat">The heat level.</param>
        /// <param name="moisture">The moisture level.</param>
        /// <param name="baseValue">The base value.</param>
        /// <returns>The biome info for the given values.</returns>
        protected virtual Biome GetBiome(float heat, float moisture, float baseValue=-1) => 
            GetBiome(GetBiomeId(heat, moisture, baseValue));

        /// <summary>
        /// This method is used to get the biome id using the provided values.
        /// </summary>
        /// <param name="heat">The heat level.</param>
        /// <param name="moisture">The moisture level.</param>
        /// <param name="baseValue">The base value.</param>
        /// <returns>The biome id based on the passed values.</returns>
        public virtual string GetBiomeId(float heat, float moisture, float baseValue = 1) {
            //convert the -1 to 1 value into an integer value 0 to the array length minus one.
            if(useOceanMap && oceanHeight <= baseValue) return oceanBiome.Guid;
            var x = (int)((moisture+1)*(biomeTable.GetUpperBound(0)+1)*.5f-.0001f);
            var y = (int)((heat+1)*(biomeTable.GetUpperBound(1)+1)*.5f-.0001f);
            return biomeTable[x, y]?.Guid;
        }

        /// <summary>
        /// This method is used to generate a biome map.
        /// </summary>
        /// <param name="size">The size of the map that you wish to generate.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="position">The center position of the generated map.</param>
        /// <returns>The generated biome map.</returns>
        public virtual BiomeMap GenerateBiomeMap(int size, Seed seed, Vector2? position = null) {
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
        public virtual string GetBiomeAt(float x, float z, Seed seed) {
            var heat = heatMapSettings.NoiseAtPoint(x, z, seed);
            var moisture = moistureMapSettings.NoiseAtPoint(x, z, seed);
            if(!useOceanMap) return GetBiomeId(heat, moisture);
            var baseVal = oceanMapSettings.NoiseAtPoint(x, z, seed);
            return GetBiomeId(heat, moisture, baseVal);
        }
        
        /// <summary>
        /// This method is used to get the biomes from a compute shader.
        /// </summary>
        /// <param name="samplePoints">The points you want the biome for.</param>
        /// <param name="seed">The seed you want to use for the biomes.</param>
        /// <returns>A list of biomes.</returns>
        public virtual List<string> GetBiomesFromComputeShader(List<SamplePoint<string>> samplePoints, Seed seed) {
            var result = new List<string>();
            return result;
            /*//create the buffer
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
            return result;*/
        }

    }

}