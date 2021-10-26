using System;
using System.Collections.Generic;
using Amilious.ProceduralTerrain.Biomes.Blending;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Biomes {
    
    /// <summary>
    /// This class is used to store Biome information for
    /// a chunk or a map.
    /// </summary>
    public class BiomeMap : MapData<int> {

        private Dictionary<int, float[,]> _weights = new Dictionary<int, float[,]>();
        public BiomeSettings BiomeSettings { get;}
        public int HashedSeed { get;}

        public BiomeMap(int hashedSeed, int size, Vector2 position, BiomeSettings settings, 
            bool isPositionCentered = true): base(size,position, isPositionCentered) {
            //set the values
            BiomeSettings = settings;
            HashedSeed = hashedSeed;
            //generate the map
            if(BiomeSettings.UsingBiomeBlending) GenerateBlendedMap();
            else GenerateNonBlendedMap();
            
        }

        /// <summary>
        /// This method is used to generate a <see cref="BiomeMap"/> without blending the biomes.
        /// </summary>
        private void GenerateNonBlendedMap() {
            //create maps
            var heatMap = BiomeSettings.HeatMapSettings.Generate(Size, HashedSeed, Position);
            var moistureMap = BiomeSettings.MoistureMapSettings.Generate(Size, HashedSeed, Position);
            if(BiomeSettings.UsingOceanMap) {
                var oceanMap = BiomeSettings.OceanMapSettings.Generate(Size, HashedSeed, Position);
                foreach(var key in heatMap) {
                    this[key] = BiomeSettings.GetBiomeId(
                        heatMap[key], moistureMap[key], oceanMap[key]);
                    //add biome type
                    if(!_weights.ContainsKey(this[key])) {
                        _weights.Add(this[key],new float[Size,Size]);
                    }
                    //set the weight
                    _weights[this[key]][key.x, key.y] = 1f;
                }
            }else {
                foreach(var key in heatMap) {
                    this[key] = BiomeSettings.GetBiomeId(heatMap[key], moistureMap[key]);
                    //add biome type
                    if(!_weights.ContainsKey(this[key])) {
                        _weights.Add(this[key],new float[Size,Size]);
                    }
                    //set the weight
                    _weights[this[key]][key.x, key.y] = 1f;
                }
            }
        }

        /// <summary>
        /// This method is used to generate a <see cref="BiomeMap"/> with biome blending.
        /// </summary>
        private void GenerateBlendedMap() {
            var b = new BiomeBlender(BiomeSettings.BlendFrequency,BiomeSettings.BlendRadiusPadding,Size);
            //generate the blend weights
            _weights = b.GetChunkBiomeWeights(HashedSeed, Position, BiomeSettings);
            //set the biome to the greatest weight
            foreach(var key in this) {
                var currentBiome = 0;
                var currentWeight = 0f;
                foreach(var biome in _weights.Keys) {
                    var value = _weights[biome][key.x,key.y];
                    if(!(value > currentWeight)) continue;
                    currentWeight = value;
                    currentBiome = biome;
                }
                this[key] = currentBiome;
            }
        }

        /// <summary>
        /// This property is used to get the weight of the given biome.
        /// </summary>
        /// <param name="biomeId">The biome id that you want to get
        /// the weight for.</param>
        /// <param name="x">The x map data position that you want to get
        /// the biome weight for.</param>
        /// <param name="z">The z map data position that you want to get
        /// the biome weight for.</param>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown
        /// if the provided map data x or z positions are invalid.</exception>
        public float this[int biomeId, int x, int z] {
            get {
                if(!IsValidX(x)) throw new ArgumentOutOfRangeException(nameof(x), x, 
                "The given x map position is not valid.");
                if(!IsValidX(z)) throw new ArgumentOutOfRangeException(nameof(z), z, 
                    "The given z map position is not valid.");
                if(!_weights.ContainsKey(biomeId)) return 0;
                return _weights[biomeId][x, z];
            }
        }

        /// <summary>
        /// This property is used to get the weight of the given biome.
        /// </summary>
        /// <param name="biomeId">The biome id that you want to get
        /// the weight for.</param>
        /// <param name="key">The key that you want to use to get the
        /// biome weight.  The x value will be used as the x map data
        /// position and the y value will be used as the z map data position.</param>
        /// <exception cref="ArgumentOutOfRangeException">This is thrown
        /// if the provided map data key is invalid.</exception>
        public float this[int biomeId, Vector2Int key] {
            get {
                if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key,
                "The provided map data key is invalid.");
                return this[biomeId, key.x, key.y];
            }
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
        public Color GetBiomeColor(Vector2Int key) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, 
        "The provided map data key is invalid.");
            return BiomeSettings.GetBiomeInfo(this[key]).biomeMapColor;
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
        public Color GetBlendedBiomeColor(Vector2Int key) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, 
        "The provided map data key is invalid.");
            float red = 0f, blue = 0f, green=0f;
            foreach(var biome in _weights.Keys) {
                var color = BiomeSettings.GetBiomeInfo(biome).biomeMapColor;
                red += color.r * _weights[biome][key.x, key.y];
                green += color.g * _weights[biome][key.x, key.y];
                blue += color.b * _weights[biome][key.x, key.y];
            }
            return new Color(red, green, blue, 1f);
        }

        private Color BiomePreviewColor(int biomeId, float height) {
            var info = BiomeSettings.GetBiomeInfo(biomeId);
            height = Mathf.InverseLerp(-info.maxHeight, info.maxHeight, height) * 2 - 1;
            return info.noiseSettings.PreviewColors.GetColor(height);

        }
        
        public Color PreviewColor(Vector2Int key, float height) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, 
                "The provided map data key is invalid.");
            return BiomePreviewColor(this[key], height);
        }

        public Color BlendedPreviewColor(Vector2Int key, float height) {
            if(!ContainsKey(key)) throw new ArgumentOutOfRangeException(nameof(key), key, 
                "The provided map data key is invalid.");
            float red = 0f, blue = 0f, green=0f;
            foreach(var biome in _weights.Keys) {
                var color = BiomePreviewColor(biome,height);
                red += color.r * _weights[biome][key.x, key.y];
                green += color.g * _weights[biome][key.x, key.y];
                blue += color.b * _weights[biome][key.x, key.y];
            }
            return new Color(red, green, blue, 1f);
        }
        
        public BiomeInfo GetBiomeInfo(Vector2Int key) {
            return BiomeSettings.GetBiomeInfo(this[key]);
        }

        public NoiseMap GenerateHeightMap() {
            var heightMap = new NoiseMap(Size, Position, new Vector2(-1, 1), IsPositionCentered);
            var centerX = heightMap.Position.x;
            var centerY = -heightMap.Position.y;
            if(IsPositionCentered) {
                centerX -= heightMap.HalfSize;
                centerY -= heightMap.HalfSize;
            }
            //generate noise
            foreach(var key in heightMap) {
                var heightValue = 0f;
                foreach(var biome in _weights.Keys) {
                    if(_weights[biome][key.x, key.y] == 0) continue;
                    var info = BiomeSettings.GetBiomeInfo(biome);
                    var rawHeight = info.noiseSettings.NoiseAtPoint(key.x + centerX, key.y + centerY, HashedSeed);
                    var lerp = Mathf.InverseLerp(-1, 1, rawHeight);
                    heightValue += Mathf.Lerp(info.minHeight,info.maxHeight,lerp) * _weights[biome][key.x,key.y];
                }
                heightMap.TrySetValue(key,heightValue);
            }
            return heightMap;
        }
        
    }
}