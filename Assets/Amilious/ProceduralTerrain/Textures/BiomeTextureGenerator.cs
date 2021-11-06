using System;
using System.ComponentModel;
using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Textures {
    
    public static class BiomeTextureGenerator {

        public static Texture2D GenerateTexture(this BiomeMap biomeMap, MapPaintingMode paintingMode, int borderCulling = 0) {
            var colorMap = biomeMap.GenerateTextureColors(paintingMode, borderCulling);
            return colorMap.TextureFromColorMap(biomeMap.GetBorderCulledSize(borderCulling),
                biomeMap.GetBorderCulledSize(borderCulling));
        }

        public static Texture2D GenerateTexture(this BiomeMap biomeMap, Color[] colorMap, int borderCulling = 0) {
            return colorMap.TextureFromColorMap(biomeMap.GetBorderCulledSize(borderCulling),
                biomeMap.GetBorderCulledSize(borderCulling));
        }

        public static Color[] GenerateTextureColors(this BiomeMap map, MapPaintingMode paintMode, int borderCulling=0) {
            var colorMap = new Color[map.GetBorderCulledValuesCount(borderCulling)];
            var heightMap = map.HeightMap;
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
        
        public static void GenerateTextureColors(this BiomeMap map, Color[] colorMap, MapPaintingMode paintMode, int borderCulling=0) {
            if(colorMap.Length != map.GetBorderCulledValuesCount(borderCulling)) {
                throw new InvalidOperationException(
                    "The provided color map is not the save size as the requested color map!");
            }
            foreach(var key in map.BorderCulledKeys(borderCulling)) {
                if(paintMode == MapPaintingMode.Material) break;
                var colorKey = (key.y-borderCulling)*map.GetBorderCulledSize(borderCulling)+(key.x-borderCulling);
                colorMap[colorKey] = paintMode switch {
                    MapPaintingMode.BiomeColors => map.GetBiomeColor(key),
                    MapPaintingMode.BlendedBiomeColors => map.GetBlendedBiomeColor(key),
                    MapPaintingMode.NoisePreviewColors => map.PreviewColor(key, map.HeightMap[key]),
                    MapPaintingMode.BlendedNoisePreviewColors => map.BlendedPreviewColor(key, map.HeightMap[key]),
                    _ => throw new ArgumentOutOfRangeException(nameof(paintMode), paintMode, null)
                };
            }
        }

    }
}