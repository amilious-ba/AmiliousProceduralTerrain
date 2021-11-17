using System;
using UnityEngine;
using Amilious.Random;
using System.Diagnostics;
using Amilious.ProceduralTerrain.Noise.Enum;
using Amilious.ProceduralTerrain.Noise.Libraries;
using Sirenix.OdinInspector;
using Amilious.ProceduralTerrain.Textures;

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

        #region Inspector Preview
        
        #if UNITY_EDITOR
        [Tooltip("This is the seed that is used for random generation.")]
        [BoxGroup(PREVIEW), OnInspectorGUI(nameof(DrawPreview), append: false)]
        [BoxGroup(PREVIEW), SerializeField, LabelText("Seed")]
        private string pvSeed = "seedless";
        [Tooltip("This is the size of the generated noise map used for the preview.")]
        [BoxGroup(PREVIEW), SerializeField, LabelText("Size")]
        private int pvSize = 100;
        [Tooltip("This is the pixel multiplier used for the preview.")] 
        [BoxGroup(PREVIEW), SerializeField, LabelText("Pixel Multiplier")]
        private int pvPixelMultiplier = 2;
        [Tooltip("This is the offset value used for the preview.")] 
        [BoxGroup(PREVIEW), SerializeField, LabelText("Offset")]
        private Vector2Int pvOffset;
        [BoxGroup(PREVIEW), SerializeField] 
        private NoisePreviewType noisePreviewType = NoisePreviewType.NoiseMap;
        [BoxGroup(PREVIEW), LabelText("Low Color"), SerializeField, HideIf("noisePreviewType", NoisePreviewType.ColorMap)]
        private Color pvLowColor = Color.white;
        [BoxGroup(PREVIEW), LabelText("High Color"), SerializeField, HideIf("noisePreviewType", NoisePreviewType.ColorMap)]
        private Color pvHighColor = Color.black;
        [BoxGroup(PREVIEW), LabelText("Color Nap"), SerializeField, ShowIf("noisePreviewType", NoisePreviewType.ColorMap)]
        private ColorMap pvColorMap;
        [BoxGroup(PREVIEW), SerializeField, ShowIf("noisePreviewType", NoisePreviewType.CrossSection)]
        [PropertyRange(1,nameof(pvSize))]
        private int crossSectionDepth;
        #endif
        #endregion

        #region Inspector General Settigns
        
        [TabGroup(TAB_GROUP, TAB_A), SerializeField]
        private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        [TabGroup(TAB_GROUP,TAB_A), SerializeField]
        private float frequency = 0.07f;
        [TabGroup(TAB_GROUP, TAB_A), SerializeField]
        private bool useNoiseCurve = true;
        [TabGroup(TAB_GROUP,TAB_A),SerializeField, ShowIf(nameof(useNoiseCurve))] 
        private AnimationCurve noiseCurve = AnimationCurve.Linear(-1f, -1f, 1f, 1f);

        #endregion

        #region Inspector Fractal Settings
        
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
        
        #endregion

        #region Inspector Cellular Settings
        
        [TabGroup(TAB_GROUP, TAB_C), SerializeField]
        private FastNoiseLite.CellularDistanceFunction distanceFunction = 
            FastNoiseLite.CellularDistanceFunction.Euclidean;
        [TabGroup(TAB_GROUP, TAB_C), SerializeField]
        private FastNoiseLite.CellularReturnType returnType =
            FastNoiseLite.CellularReturnType.Distance;
        [TabGroup(TAB_GROUP, TAB_C), SerializeField]
        private float cellularJitter = 1f;

        #endregion

        #region Instance Variables

        private FastNoiseLite _noise;
        
        #endregion
        
        #region Properties
        
        #endregion

        #region Editor Only
        
        #if UNITY_EDITOR

        /// <summary>
        /// This method is called when a <see cref="GameObject"/> is
        /// changed in the unity editor
        /// </summary>
        protected void OnValidate() {
            _noise = null;
            crossSectionDepth = Mathf.Min(crossSectionDepth, pvSize);
            SetUpNoise();
            GeneratePreviewTexture();
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private GUIStyle _timerGUI;
        private string _generateHeightTime;
        private string _generateTextureTime;
        private Texture2D _previewTexture;
        
        /// <summary>
        /// This method is used to generate the preview texture.
        /// </summary>
        private void GeneratePreviewTexture() {
            _stopwatch.Reset();
            _stopwatch.Start();
            var seedStruct = new Seed(this.pvSeed);
            var noiseMap = Generate(pvSize, seedStruct, pvOffset);
            _stopwatch.Stop();
            _generateHeightTime = $"  Noise Map: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
            _stopwatch.Reset();
            _stopwatch.Start();
            _previewTexture = noisePreviewType switch {
                NoisePreviewType.NoiseMap => noiseMap.GenerateGradientTexture(pixelMultiplier: pvPixelMultiplier,minColor:pvLowColor,maxColor:pvHighColor),
                NoisePreviewType.ColorMap => noiseMap.GenerateTexture(pvColorMap, pvPixelMultiplier),
                NoisePreviewType.CrossSection => noiseMap.GenerateCrossSectionTexture(pvHighColor, 
                    pvLowColor,crossSectionDepth-1,pvPixelMultiplier),
                _ => throw new ArgumentOutOfRangeException()
            };
            _stopwatch.Stop();
            _generateTextureTime = $"  Texture: min {_stopwatch.Elapsed.Minutes} sec {_stopwatch.Elapsed.Seconds} ms {_stopwatch.Elapsed.Milliseconds}";
        }

        /// <summary>
        /// This method is used to draw the preview
        /// </summary>
        /// ReSharper disable once UnusedMember.Local
        private void DrawPreview() {
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
        
        #endif
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// This is the first method that is called on this object by unity
        /// </summary>
        protected virtual void Awake() {
            SetUpNoise();
            #if UNITY_EDITOR
            GeneratePreviewTexture();
            #endif
        }

        /// <summary>
        /// This method is used to setup the noise.
        /// </summary>
        protected virtual void SetUpNoise() {
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

        /// <summary>
        /// This method is used to set the values in the compute shader.
        /// </summary>
        /// <param name="computeShader">The compute shader.</param>
        /// <param name="prefix">The prefix for this noise in the compute shader.</param>
        /// <param name="seed">The seed that will be used with this noise.</param>
        public override void SetComputeShaderValues(ComputeShader computeShader, char prefix, Seed seed) {
            computeShader.SetInt($"{prefix}_seed",seed.Value);
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

        /// <summary>
        /// This method is used to generate a noise map using this <see cref="NoiseSettings"/>.
        /// </summary>
        /// <param name="size">The size of the map that should be generated.</param>
        /// <param name="seed">The seed that should be used to generate the map.</param>
        /// <param name="position">The position of the map.</param>
        /// <returns>A noise map generated from the given values.</returns>
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
        /// <param name="x">The x position.</param>
        /// <param name="z">The z position.</param>
        /// <param name="seed">The seed.</param>
        /// <returns>The noise value at the given position with the given seed.</returns>
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

        #endregion
        
    }
}