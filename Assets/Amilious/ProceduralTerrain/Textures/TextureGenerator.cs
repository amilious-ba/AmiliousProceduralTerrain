using UnityEngine;
using Amilious.ProceduralTerrain.Noise;
using Amilious.ProceduralTerrain.Biomes;

namespace Amilious.ProceduralTerrain.Textures {
    
    /// <summary>
    /// This class is used to generate textures.
    /// </summary>
    public static class TextureGenerator {
        
        /// <summary>
        /// This method is used to generate a texture from the passed values.
        /// </summary>
        /// <param name="colorMap">The colors of the texture.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="multiplier">The pixel multiplier.</param>
        /// <returns>The texture generated from the passed values.</returns>
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

        /// <summary>
        /// This method is used to generate a gradient texture based on the passed values.
        /// </summary>
        /// <param name="noiseMap">The noise map that you want to generate a texture from.</param>
        /// <param name="minColor">The minimum value color.</param>
        /// <param name="maxColor">The maximum value color.</param>
        /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <param name="borderCulling">The border that should be removed from the noise when generating
        /// the texture.</param>
        /// <returns>The generated texture.</returns>
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

        /// <summary>
        /// This method is used to generate a texture based on the passed values.
        /// </summary>
        /// <param name="noiseMap">The noise map that you want to generate a texture from.</param>
        /// <param name="colors">The color map colors for the texture.</param>
        /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <param name="borderCulling">The border that should be removed from the noise when generating
        /// the texture.</param>
        /// <returns>The generated texture.</returns>
        public static Texture2D GenerateTexture(this NoiseMap noiseMap, ColorMap colors, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[noiseMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in noiseMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * noiseMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = colors.GetColor(noiseMap[key]);
            }
            return TextureFromColorMap(colorMap, noiseMap.GetBorderCulledSize(borderCulling), 
                noiseMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }

        /// <summary>
        /// This method is used to generate a texture based on the passed values.
        /// </summary>
        /// <param name="noiseMap">The noise map that you want to generate a texture from.</param>
        /// <param name="splitValue">This value will decide where the colors are split.</param>
        /// <param name="lessOrEqual">The color for values that are less than or equal to the split value.</param>
        /// <param name="greater">The color for the values that are greater than the split value.</param>
        /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <param name="borderCulling">The border that should be removed from the noise when generating
        /// the texture.</param>
        /// <returns>The generated texture.</returns>
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
        
        /// <summary>
        /// This method is used to generate a texture based on the passed values.
        /// </summary>
        /// <param name="biomeMap">The biome map.</param>
        /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <param name="borderCulling">The border that should be removed from the noise when generating
        /// the texture.</param>
        /// <returns>The generated texture.</returns>
        public static Texture2D GenerateBiomeTexture(this BiomeMap biomeMap, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[biomeMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in biomeMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * biomeMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = biomeMap.GetBiomeColor(key);
            }
            return TextureFromColorMap(colorMap, biomeMap.GetBorderCulledSize(borderCulling), 
                biomeMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }
        
        /// <summary>
        /// This method is used to generate a texture based on the passed values.
        /// </summary>
        /// <param name="biomeMap">The biome map.</param>
        /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <param name="borderCulling">The border that should be removed from the noise when generating
        /// the texture.</param>
        /// <returns>The generated texture.</returns>
        public static Texture2D GenerateBiomeBlendTexture(this BiomeMap biomeMap, int pixelMultiplier = 1, int borderCulling = 0) {
            var colorMap = new Color[biomeMap.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in biomeMap.BorderCulledKeys(borderCulling)) {
                var colorKey = (key.y-borderCulling) * biomeMap.GetBorderCulledSize(borderCulling) + (key.x-borderCulling);
                colorMap[colorKey] = biomeMap.GetBlendedBiomeColor(key);
            }
            return TextureFromColorMap(colorMap, biomeMap.GetBorderCulledSize(borderCulling), 
                biomeMap.GetBorderCulledSize(borderCulling), pixelMultiplier);
        }

        /// <summary>
        /// This method is used to generate a cross section texture from the passed noise map.
        /// </summary>
        /// <param name="noiseMap">The noise map you want to use to generate the texture.</param>
        /// <param name="above">The color for values above the noise map value.</param>
        /// <param name="below">The color for vales below the noise map value.</param>
        /// <param name="depth">The position within the noise map.</param>
        /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <returns>The generated texture.</returns>
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
        
        /// <summary>
        /// This method is used to apply the pixel multiplier.
        /// </summary>
        /// <param name="colorMap">The color map that you want to apply the pixel multiplier to.</param>
        /// <param name="width">The width of the color map.</param>
        /// <param name="height">The height of the color map.</param>
        /// /// <param name="pixelMultiplier">The pixel multiplier.</param>
        /// <returns>The pixel multiplied color map.</returns>
        public static Color[] MultiplyPixels(this Color[] colorMap, int width, int height, int pixelMultiplier) {
            if(pixelMultiplier <= 0) pixelMultiplier = 1;
            if(pixelMultiplier == 1) return colorMap;
            var newWidth = width * pixelMultiplier;
            var newHeight = height * pixelMultiplier;
            var newMap = new Color[newWidth * newHeight];
            for(int x=0;x<width;x++) for(int y = 0; y < height; y++) {
                var color = colorMap[y * width + x];
                for(var yy=0;yy<pixelMultiplier;yy++) for(int xx = 0; xx < pixelMultiplier; xx++) {
                    newMap[(y * pixelMultiplier + yy) * newWidth + x*pixelMultiplier + xx] = color;
                } 
            }
            return newMap;
        }
        
    }
    
}