using Amilious.ProceduralTerrain.Biomes;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Textures {
    
    public static class TextureGenerator {
        
        public static Texture2D TextureFromColorMap(this Color[] colorMap, int width, int height, int multiplier = 1) {
            if(multiplier <= 0) multiplier = 1;
            if(multiplier != 1) colorMap = colorMap.MultiplyPixels(width, height, multiplier);
            var texture = new Texture2D(width*multiplier, height*multiplier) {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, 
            };
            texture.SetPixels(colorMap);
            texture.Apply();
            return texture;
        }

        public static Texture2D GenerateGradientTexture(this NoiseMap noiseMap, Color? minColor = null, Color? maxColor = null,
            int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[noiseMap.GetBorderCulledValuesCount(borderCulling)];
            minColor??=Color.white;
            maxColor??=Color.black;
            foreach(var key in noiseMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * noiseMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                var lerpValue = noiseMap[key];
                if(noiseMap.GeneratedMinMax.x != 0 || noiseMap.GeneratedMinMax.y != 1) 
                    lerpValue = Mathf.InverseLerp(noiseMap.GeneratedMinMax.x, noiseMap.GeneratedMinMax.y, lerpValue);
                colorMap[colorKey] = Color.Lerp(minColor.Value,maxColor.Value, lerpValue);
            }
            return TextureFromColorMap(colorMap, noiseMap.GetBorderCulledSize(borderCulling), 
                noiseMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }

        public static Texture2D GenerateTexture(this NoiseMap noiseMap, ColorMap colors, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[noiseMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in noiseMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * noiseMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = colors.GetColor(noiseMap[key]);
            }
            return TextureFromColorMap(colorMap, noiseMap.GetBorderCulledSize(borderCulling), 
                noiseMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }

        public static Texture2D GenerateSplitTexture(this NoiseMap noiseMap, float splitValue, Color lessOrEqual,
            Color greater, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[noiseMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in noiseMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * noiseMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = noiseMap[key] <= splitValue ? lessOrEqual : greater;
                
            }
            return TextureFromColorMap(colorMap, noiseMap.GetBorderCulledSize(borderCulling), 
                noiseMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }
        
        public static Texture2D GenerateBiomeTexture(this BiomeMap biomeMap, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[biomeMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in biomeMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * biomeMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = biomeMap.GetBiomeColor(key);
            }
            return TextureFromColorMap(colorMap, biomeMap.GetBorderCulledSize(borderCulling), 
                biomeMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }
        
        public static Texture2D GenerateBiomeBlendTexture(this BiomeMap biomeMap, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[biomeMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in biomeMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * biomeMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = biomeMap.GetBlendedBiomeColor(key);
            }
            return TextureFromColorMap(colorMap, biomeMap.GetBorderCulledSize(borderCulling), 
                biomeMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }

        public static Texture2D GenerateCrossSectionTexture(this NoiseMap noiseMap, Color above, Color below, 
            int depth = 0, int pixelMultiplier=1) {
            var colorMap = new Color[noiseMap.NumberOfValues];
            depth = Mathf.Clamp(depth, 0, noiseMap.Size);
            for(int x = 0; x < noiseMap.Size; x++) {
                var height = (int)Mathf.Lerp(0, noiseMap.Size, Mathf.InverseLerp(-1,1,noiseMap[x, depth]));
                for(int y = 0; y < noiseMap.Size; y++) {
                    var colorKey = y * noiseMap.Size + x;
                    if(y < height) colorMap[colorKey] = below;
                    else colorMap[colorKey] = above;
                }
            }
            return TextureFromColorMap(colorMap, noiseMap.Size, noiseMap.Size, pixelMultiplier);
        }
        
        public static Color[] MultiplyPixels(this Color[] colorMap, int width, int height, int multiplier) {
            if(multiplier <= 0) multiplier = 1;
            if(multiplier == 1) return colorMap;
            var newWidth = width * multiplier;
            var newHeight = height * multiplier;
            var newMap = new Color[newWidth * newHeight];
            for(int x=0;x<width;x++) for(int y = 0; y < height; y++) {
                var color = colorMap[y * width + x];
                for(var yy=0;yy<multiplier;yy++) for(int xx = 0; xx < multiplier; xx++) {
                    newMap[(y * multiplier + yy) * newWidth + x*multiplier + xx] = color;
                } 
            }
            return newMap;
        }
        
    }
    
}