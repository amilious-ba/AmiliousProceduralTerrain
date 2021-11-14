using System;
using UnityEngine;
using Amilious.Random;
using Amilious.Saving;
using System.Threading;
using System.Collections.Generic;
using Amilious.ProceduralTerrain.Noise;

namespace Amilious.ProceduralTerrain.Biomes {
    
    /// <summary>
    /// This class is used to store Biome information for
    /// a chunk or a map.
    /// </summary>
    public class BiomeMap : MapData<string> {

        protected const string PREFIX = "biomeMapData";
        protected const string BIOME_VALUES = "biomeValue";
        protected const string BIOME_WEIGHTS = "biomeWeights";
        protected const string POSITION = "position";
        
        #region Instance Variables
        
        protected Dictionary<string, float[,]> weights = new Dictionary<string, float[,]>();
        
        #endregion
        
        #region Properties
        
        protected virtual BiomeSettings BiomeSettings { get;}
        
        /// <summary>
        /// This property contains the seed that is used for this biome map.
        /// </summary>
        protected virtual Seed Seed { get;}

        /// <summary>
        /// This property is used to get the height map of this biome map.
        /// </summary>
        public virtual NoiseMap HeightMap { get; private set; }
        
        /// <summary>
        /// This property is used to get the weight of the given biome.
        /// </summary>
        /// <param name="biomeGuid">The biome id that you want to get
        /// the weight for.</param>
        /// <param name="x">The x map data position that you want to get
        /// the biome weight for.</param>
        /// <param name="z">The z map data position that you want to get
        /// the biome weight for.</param>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown
        /// if the provided map data x or z positions are invalid.</exception>
        /// ReSharper disable once MemberCanBeProtected.Global
        public virtual float this[string biomeGuid, int x, int z] {
            get {
                if(!IsValidX(x)) throw new ArgumentOutOfRangeException(nameof(x), x, 
                    "The given x map position is not valid.");
                if(!IsValidX(z)) throw new ArgumentOutOfRangeException(nameof(z), z, 
                    "The given z map position is not valid.");
                if(!weights.ContainsKey(biomeGuid)) return 0;
                return weights[biomeGuid][x, z];
            }
        }

        /// <summary>
        /// This property is used to get the weight of the given biome.
        /// </summary>
        /// <param name="biomeGuid">The biome id that you want to get
        /// the weight for.</param>
        /// <param name="key">The key that you want to use to get the
        /// biome weight.  The x value will be used as the x map data
        /// position and the y value will be used as the z map data position.</param>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown
        /// if the provided map data key is invalid.</exception>
        public virtual float this[string biomeGuid, Vector2Int key] {
            get {
                if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key,
                    "The provided map data key is invalid.");
                return this[biomeGuid, key.x, key.y];
            }
        }

        #endregion
        
        #region Constructors
        
        /// <summary>
        /// This constructor is used to create a new BiomeMap.
        /// </summary>
        /// <param name="seed">The seed that will be used to generate the map.</param>
        /// <param name="size">The size of the map.</param>
        /// <param name="settings">The biome settings that will be used when generating the map.</param>
        /// <param name="isPositionCentered">True if the position of the map is centered.</param>
        /// <remarks>The constructor does not generate the biome map.  To generate
        /// the map you need to use the <see cref="Generate(UnityEngine.Vector2,System.Threading.CancellationToken)"/>
        /// or the <see cref="Generate(UnityEngine.Vector2)"/> methods.</remarks>
        public BiomeMap(Seed seed, int size, BiomeSettings settings, bool isPositionCentered = true) : 
            base(size,Vector2.zero, isPositionCentered) {
            //set the values
            BiomeSettings = settings;
            Seed = seed;
            HeightMap = new NoiseMap(Size, Position, new Vector2(-1, 1), IsPositionCentered);
        }
        
        #endregion
        
        #region Public Methods

        /// <summary>
        /// This method is used to generate the biome map.
        /// </summary>
        /// <param name="position">The position the generated map is for.</param>
        /// <param name="token">A cancellation token that can be used to cancel the generation.</param>
        /// <seealso cref="Generate(UnityEngine.Vector2)"/>
        public virtual void Generate(Vector2 position, CancellationToken token) {
            Position = position;
            HeightMap.ResetPosition(position);
            //generate the map
            if(BiomeSettings.UsingBiomeBlending) GenerateBlendedMap(token);
            else GenerateNonBlendedMap(token);
            GenerateHeightMap();
            HasBeenUpdated = true;
        }
        
        /// <summary>
        /// This method is used to generate the <see cref="BiomeMap"/>..
        /// </summary>
        /// <param name="position">The position the generated map is for.</param>
        /// <seealso cref="Generate(UnityEngine.Vector2,System.Threading.CancellationToken)"/>
        public virtual void Generate(Vector2 position) {
            Position = position;
            HeightMap.ResetPosition(position);
            //generate the map
            var tokenSource = new CancellationTokenSource();
            if(BiomeSettings.UsingBiomeBlending) GenerateBlendedMap(tokenSource.Token);
            else GenerateNonBlendedMap(tokenSource.Token);
            GenerateHeightMap();
            HasBeenUpdated = true;
            tokenSource.Dispose();
        }

        /// <summary>
        /// This method is used to save the <see cref="BiomeMap"/>.
        /// </summary>
        /// <param name="saveData">The <see cref="SaveData"/> that is being used to save the data.</param>
        public virtual bool Save(SaveData saveData) {
            if(!HasBeenUpdated && !HeightMap.HasBeenUpdated) return false;
            saveData.SetPrefix(PREFIX);
            if(HasBeenUpdated) {
                saveData.StoreData(BIOME_VALUES, values);
                saveData.StoreData(BIOME_WEIGHTS, weights);
                saveData.StoreData(POSITION, Position);
            }
            HeightMap.Save(saveData);
            saveData.ClearPrefix();
            HasBeenUpdated = false;
            return true;
        }

        /// <summary>
        /// This method is used to load the <see cref="BiomeMap"/>.
        /// </summary>
        /// <param name="saveData">The <see cref="SaveData"/> that is being used to load the data.</param>
        public virtual void Load(SaveData saveData) {
            saveData.SetPrefix(PREFIX);
            Position = saveData.FetchData<Vector2>(POSITION);
            values = saveData.FetchData<string[,]>(BIOME_VALUES);
            weights = saveData.FetchData<Dictionary<string, float[,]>>(BIOME_WEIGHTS);
            saveData.ClearPrefix();
            HeightMap.Load(saveData);
            HasBeenUpdated = false;
        }

        /// <summary>
        /// This method is used to get the biome color using the given
        /// map data key.
        /// </summary>
        /// <param name="key">The key of the map data value you want to get
        /// the color of.  The x value will be used as the x map data position
        /// and the y value will be used as the z map data position.</param>
        /// <returns>The color for the biome at the given map data position.</returns>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown if
        /// the provided map data key is invalid.</exception>
        public virtual Color GetBiomeColor(Vector2Int key) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, 
        "The provided map data key is invalid.");
            return BiomeSettings.GetBiome(this[key]).biomeMapColor;
        }

        /// <summary>
        /// This method is used to get the blended biome color based on the biome
        /// weights at the given key.
        /// </summary>
        /// <param name="key">The key of the map data value that you want to get
        /// the blended biome color of.</param>
        /// <returns>The blended biome color at the given map data position.</returns>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown if
        /// the provided map data key is invalid.</exception>
        public virtual Color GetBlendedBiomeColor(Vector2Int key) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, 
        "The provided map data key is invalid.");
            float red = 0f, blue = 0f, green=0f;
            foreach(var biome in weights.Keys) {
                var color = BiomeSettings.GetBiome(biome).biomeMapColor;
                red += color.r * weights[biome][key.x, key.y];
                green += color.g * weights[biome][key.x, key.y];
                blue += color.b * weights[biome][key.x, key.y];
            }
            return new Color(red, green, blue, 1f);
        }
        
        /// <summary>
        /// This method is used to get the biome info for the main biome at
        /// the given key position.
        /// </summary>
        /// <param name="key">The position you want to get the biome info for.</param>
        /// <returns>The biome info at the given key position.</returns>
        public virtual Biome GetBiomeInfo(Vector2Int key) {
            return BiomeSettings.GetBiome(this[key]);
        }

        #endregion
        
        #region Protected Methods
        
        /// <summary>
        /// This method is used to generate a height map using this biome map.
        /// </summary>
        protected virtual void GenerateHeightMap() {
            var centerX = HeightMap.Position.x;
            var centerY = -HeightMap.Position.y;
            if(IsPositionCentered) {
                centerX -= HeightMap.HalfSize;
                centerY -= HeightMap.HalfSize;
            }
            //generate noise
            foreach(var key in HeightMap) {
                var heightValue = 0f;
                foreach(var biome in weights.Keys) {
                    if(weights[biome][key.x, key.y] == 0) continue;
                    var info = BiomeSettings.GetBiome(biome);
                    var rawHeight = info.noiseSettings.NoiseAtPoint(key.x + centerX, key.y + centerY, Seed);
                    var lerp = Mathf.InverseLerp(-1, 1, rawHeight);
                    heightValue += Mathf.Lerp(info.minHeight,info.maxHeight,lerp) * weights[biome][key.x,key.y];
                }
                HeightMap.TrySetValue(key,heightValue);
            }
        }
        
        /// <summary>
        /// This method is used to generate a <see cref="BiomeMap"/> without blending the biomes.
        /// </summary>
        protected virtual void GenerateNonBlendedMap(CancellationToken token) {
            //create maps
            var heatMap = BiomeSettings.HeatMapSettings.Generate(Size, Seed, Position);
            var moistureMap = BiomeSettings.MoistureMapSettings.Generate(Size, Seed, Position);
            if(BiomeSettings.UsingOceanMap) {
                var oceanMap = BiomeSettings.OceanMapSettings.Generate(Size, Seed, Position);
                foreach(var key in heatMap) {
                    token.ThrowIfCancellationRequested();
                    this[key] = BiomeSettings.GetBiomeId(
                        heatMap[key], moistureMap[key], oceanMap[key]);
                    //add biome type
                    if(!weights.ContainsKey(this[key])) {
                        weights.Add(this[key],new float[Size,Size]);
                    }
                    //set the weight
                    weights[this[key]][key.x, key.y] = 1f;
                }
            }else {
                foreach(var key in heatMap) {
                    token.ThrowIfCancellationRequested();
                    this[key] = BiomeSettings.GetBiomeId(heatMap[key], moistureMap[key]);
                    //add biome type
                    if(!weights.ContainsKey(this[key])) {
                        weights.Add(this[key],new float[Size,Size]);
                    }
                    //set the weight
                    weights[this[key]][key.x, key.y] = 1f;
                }
            }
        }

        /// <summary>
        /// This method is used to generate a <see cref="BiomeMap"/> with biome blending.
        /// </summary>
        protected virtual void GenerateBlendedMap(CancellationToken token) {
            //generate the blend weights
            weights = BiomeSettings.BlendChunk(Size,Seed,Position, token);
            //set the biome to the greatest weight
            foreach(var key in this) {
                string currentBiome = null;
                var currentWeight = 0f;
                foreach(var biome in weights.Keys) {
                    token.ThrowIfCancellationRequested();
                    var value = weights[biome][key.x,key.y];
                    if(!(value > currentWeight)) continue;
                    currentWeight = value;
                    currentBiome = biome;
                }
                this[key] = currentBiome;
            }
        }
        
        #endregion
        
    }
}