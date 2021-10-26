using System;
using System.ComponentModel;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Textures {
    
    public static class BiomeTextureGenerator {

        public static Texture2D GenerateTexture(this BiomeMap biomeMap, NoiseMap heightMap, 
            MapPaintingMode paintingMode, int borderCulling = 0) {
            var colorMap = biomeMap.GenerateTextureColors(heightMap, paintingMode, borderCulling);
            return colorMap.TextureFromColorMap(biomeMap.GetBorderCulledSize(borderCulling),
                biomeMap.GetBorderCulledSize(borderCulling));
        }

        public static Texture2D GenerateTexture(this BiomeMap biomeMap, Color[] colorMap, int borderCulling = 0) {
            return colorMap.TextureFromColorMap(biomeMap.GetBorderCulledSize(borderCulling),
                biomeMap.GetBorderCulledSize(borderCulling));
        }

        public static Color[] GenerateTextureColors(this BiomeMap map, NoiseMap heightMap, MapPaintingMode paintMode, int borderCulling=0) {
            var colorMap = new Color[map.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in map.BorderCulledKeys(borderCulling)) {
                if(paintMode == MapPaintingMode.Material) break;
                var colorKey = (key.y-borderCulling)*map.GetBorderCulledSize(borderCulling)+(key.x-borderCulling);
                colorMap[colorKey] = paintMode switch {
                    MapPaintingMode.BiomeColors => map.GetBiomeColor(key),
                    MapPaintingMode.BlendedBiomeColors => map.GetBlendedBiomeColor(key),
                    MapPaintingMode.NoisePreviewColors => map.PreviewColor(key, heightMap[key]),
                    MapPaintingMode.BlendedNoisePreviewColors => map.BlendedPreviewColor(key, heightMap[key]),
                    _ => throw new ArgumentOutOfRangeException(nameof(paintMode), paintMode, null)
                };
            }
            return colorMap;
        }

    }
}