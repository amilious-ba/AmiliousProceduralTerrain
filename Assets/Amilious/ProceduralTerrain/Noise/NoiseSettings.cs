using System;
using System.Diagnostics;
using Amilious.ProceduralTerrain.Textures;
using Amilious.Random;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Noise {

    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Noise Settings", order = -1), HideMonoScript]
    public class NoiseSettings : AbstractNoiseProvider {

        public const string PREVIEW = "Preview";
        public const string TAB_GROUP = "Settings";
        public const string TAB_A = "General";
        public const string TAB_B = "Fractal";
        public const string TAB_C = "Cellular";
        public const string TAB_D = "Domain Warp";
        public const string TAB_E = "Domain Warp Fractal";

        private FastNoiseLite _noise;

        /// <summary>
        /// Preview Inspector
        /// </summary>
        [Tooltip("This is the seed that is used for random generation.")]
        [BoxGroup(PREVIEW), OnInspectorGUI("DrawPreview", append: false)]
        [BoxGroup(PREVIEW), SerializeField]
        private string seed = "seedless";
        [Tooltip("This is the size of the generated noise map used for the preview.")]
        [BoxGroup(PREVIEW), SerializeField]
        private int size = 100;
        [Tooltip("This is the pixel multiplier used for the preview.")] [BoxGroup(PREVIEW), SerializeField]
        private int pixelMultiplier = 2;
        [Tooltip("This is the offset value used for the preview.")] [BoxGroup(PREVIEW), SerializeField]
        private Vector2Int offset;
        [FormerlySerializedAs("previewType")] [BoxGroup(PREVIEW), SerializeField] 
        private NoisePreviewType noisePreviewType = NoisePreviewType.NoiseMap;
        [BoxGroup(PREVIEW), SerializeField, HideIf("noisePreviewType", NoisePreviewType.ColorMap)]
        private Color lowColor = Color.white;
        [BoxGroup(PREVIEW), SerializeField, HideIf("noisePreviewType", NoisePreviewType.ColorMap)]
        private Color highColor = Color.black;
        [BoxGroup(PREVIEW), SerializeField, ShowIf("noisePreviewType", NoisePreviewType.ColorMap)]
        private ColorMap colorMap;
        [BoxGroup(PREVIEW), SerializeField, ShowIf("noisePreviewType", NoisePreviewType.CrossSection)]
        [PropertyRange(1,nameof(size))]
        private int crossSectionDepth = 0;

        [TabGroup(TAB_GROUP, TAB_A), SerializeField]
        private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        [TabGroup(TAB_GROUP,TAB_A), SerializeField]
        private float frequency = 0.07f;
        [TabGroup(TAB_GROUP, TAB_A), SerializeField]
        private bool useNoiseCurve = true;
        [TabGroup(TAB_GROUP,TAB_A),SerializeField, ShowIf(nameof(useNoiseCurve))] 
        private AnimationCurve noiseCurve = AnimationCurve.Linear(-1f, -1f, 1f, 1f);

        [TabGroup(TAB_GROUP, TAB_B), SerializeField]
        private FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.FBm;
        [TabGroup(TAB_GROUP, TAB_B), SerializeField]
        private int fractalOctaves = 5;
        [TabGroup(TAB_GROUP, TAB_B), SerializeField]
        private float fractalLacunarity = 2;
        [TabGroup(TAB_GROUP, TAB_B), SerializeField]
        private float fractalGain = .5f;
        [TabGroup(TAB_GROUP, TAB_B), SerializeField]
        private float waitedStrength = 1;
        [TabGroup(TAB_GROUP, TAB_B), SerializeField]
        private float pingPongStrength = 2;

        [TabGroup(TAB_GROUP, TAB_C), SerializeField]
        private FastNoiseLite.CellularDistanceFunction distanceFunction = 
            FastNoiseLite.CellularDistanceFunction.Euclidean;
        [TabGroup(TAB_GROUP, TAB_C), SerializeField]
        private FastNoiseLite.CellularReturnType returnType =
            FastNoiseLite.CellularReturnType.Distance;
        [TabGroup(TAB_GROUP, TAB_C), SerializeField]
        private float cellularJitter = 1f;


        public override ColorMap PreviewColors { get => colorMap; }

        private void Awake() {
            SetUpNoise();
            #if UNITY_EDITOR
            GeneratePreviewTexture();
            #endif
        }

#if UNITY_EDITOR

        protected void OnValidate() {
            _noise = null;
            crossSectionDepth = Mathf.Min(crossSectionDepth, size);
            SetUpNoise();
            GeneratePreviewTexture();
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
            var noiseMap = Generate(size, seed, offset);
            _stopwatch.Stop();
            _generateHeightTime = $"  Noise Map: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
            _stopwatch.Reset();
            _stopwatch.Start();
            _previewTexture = noisePreviewType switch {
                NoisePreviewType.NoiseMap => noiseMap.GenerateGradientTexture(pixelMultiplier: pixelMultiplier,minColor:lowColor,maxColor:highColor),
                NoisePreviewType.ColorMap => noiseMap.GenerateTexture(colorMap, pixelMultiplier),
                NoisePreviewType.CrossSection => noiseMap.GenerateCrossSectionTexture(highColor, 
                    lowColor,crossSectionDepth-1,pixelMultiplier),
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
        
        #endif

        private void SetUpNoise() {
            if(_noise != null) return;
            //create noise generator
            _noise ??= new FastNoiseLite();
            //apply settings
            _noise.SetNoiseType(noiseType);
            _noise.SetFrequency(frequency);
            _noise.SetFractalType(fractalType);
            _noise.SetFractalOctaves(fractalOctaves);
            _noise.SetFractalLacunarity(fractalLacunarity);
            _noise.SetFractalGain(fractalGain);
            _noise.SetFractalWeightedStrength(waitedStrength);
            _noise.SetFractalPingPongStrength(pingPongStrength);
            _noise.SetCellularDistanceFunction(distanceFunction);
            _noise.SetCellularReturnType(returnType);
            _noise.SetCellularJitter(cellularJitter);
        }

        public override void SetComputeShaderValues(ComputeShader computeShader, char prefix, int seed) {
            computeShader.SetInt($"{prefix}_seed",seed);
            computeShader.SetInt($"{prefix}_noise_type",(int)noiseType);
            computeShader.SetFloat($"{prefix}_frequency",frequency);
            computeShader.SetInt($"{prefix}_fractal_type",(int)fractalType);
            computeShader.SetInt($"{prefix}_octaves",fractalOctaves);
            computeShader.SetFloat($"{prefix}_lacunarity",fractalLacunarity);
            computeShader.SetFloat($"{prefix}_gain", fractalGain);
            computeShader.SetFloat($"{prefix}_weighted_strength", waitedStrength);
            computeShader.SetFloat($"{prefix}_ping_pong_strength", pingPongStrength);
            computeShader.SetInt($"{prefix}_cellular_distance_function",(int)distanceFunction);
            computeShader.SetInt($"{prefix}_cellular_return_type",(int)returnType);
            computeShader.SetFloat($"{prefix}_cellular_jitter", cellularJitter);
            computeShader.SetInt($"{prefix}_rotation_type", 0);
            computeShader.SetFloat($"{prefix}_domain_warp_amplitude", 0f);
        }

        //TODO: cache the seed value as an int
        public override NoiseMap Generate(int size, Seed seed, Vector2? position = null) {
            SetUpNoise();
            //get things set up
            position ??= Vector2Int.zero;
            var noiseMap = new NoiseMap(size, position.Value, new Vector2(-1,1));
            _noise.SetSeed(seed.Value);
            var centerX = noiseMap.Position.x - noiseMap.HalfSize;
            var centerY = -noiseMap.Position.y - noiseMap.HalfSize;
            var curve = new AnimationCurve(noiseCurve.keys);

            //generate noise
            foreach(var key in noiseMap) {
                var value = _noise.GetNoise(key.x + centerX, key.y + centerY);
                if(useNoiseCurve) value = curve.Evaluate(value);
                noiseMap.TrySetValue(key,value);
            }

            return noiseMap;

        }

        /// <summary>
        /// This method is used to get the noise value at the given position using the current settings.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public override float NoiseAtPoint(float x, float z, Seed seed) {
            SetUpNoise();
            _noise.SetSeed(seed.Value);
            var value = _noise.GetNoise(x, z);
            if(useNoiseCurve) {
                var curve = new AnimationCurve(noiseCurve.keys);
                value = curve.Evaluate(value);
            }
            return value;
        }

        
    }
}