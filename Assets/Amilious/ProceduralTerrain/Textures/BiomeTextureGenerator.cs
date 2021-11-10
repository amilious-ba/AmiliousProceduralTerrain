using System;
using UnityEngine;
using Amilious.ProceduralTerrain.Mesh;
using Amilious.ProceduralTerrain.Biomes;

namespace Amilious.ProceduralTerrain.Textures {
    
    /// <summary>
    /// This class is used to generate textures for the <see cref="BiomeMap"/>.
    /// </summary>
    public static class BiomeTextureGenerator {

        /// <summary>
        /// This method is used to generate a texture based on the passed values.
        /// </summary>
        /// <param name="biomeMap">The biome map.</param>
        /// <param name="paintMode">The paining mode.</param>
        /// <param name="borderCulling">The optional border to remove from the map.</param>
        /// <returns>A texture generated from the passed values.</returns>
        public static Texture2D GenerateTexture(this BiomeMap biomeMap, TerrainPaintingMode paintMode, int borderCulling = 0) {
            var colorMap = biomeMap.GenerateTextureColors(paintMode, borderCulling);
            return colorMap.TextureFromColorMap(biomeMap.GetBorderCulledSize(borderCulling),
                biomeMap.GetBorderCulledSize(borderCulling));
        }

        /// <summary>
        /// This method is used to generate a texture based on the passed values.
        /// </summary>
        /// <param name="biomeMap">The biome map.</param>
        /// <param name="colorMap">The texture color data.</param>
        /// <param name="borderCulling">The optional border to remove from the map.</param>
        /// <returns>A texture generated from the passed values.</returns>
        public static Texture2D GenerateTexture(this BiomeMap biomeMap, Color[] colorMap, int borderCulling = 0) {
            return colorMap.TextureFromColorMap(biomeMap.GetBorderCulledSize(borderCulling),
                biomeMap.GetBorderCulledSize(borderCulling));
        }

        /// <summary>
        /// This method is used to generate a color array based on the passed values.
        /// </summary>
        /// <param name="map">The biome map.</param>
        /// <param name="paintMode">The paining mode.</param>
        /// <param name="borderCulling">The optional border to remove from the map.</param>
        /// <returns>A color map based on the passed values</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if there is an unhandled
        /// painting mode.</exception>
        public static Color[] GenerateTextureColors(this BiomeMap map, TerrainPaintingMode paintMode, int borderCulling=0) {
            var colorMap = new Color[map.GetBorderCulledValuesCount(borderCulling)];
            foreach(var key in map.BorderCulledKeys(borderCulling)) {
                if(paintMode == TerrainPaintingMode.Material) break;
                var colorKey = (key.y-borderCulling)*map.GetBorderCulledSize(borderCulling)+(key.x-borderCulling);
                colorMap[colorKey] = paintMode switch {
                    TerrainPaintingMode.BiomeColors => map.GetBiomeColor(key),
                    TerrainPaintingMode.BlendedBiomeColors => map.GetBlendedBiomeColor(key),
                    TerrainPaintingMode.NoisePreviewColors => map.PreviewColor(key),
                    TerrainPaintingMode.BlendedNoisePreviewColors => map.BlendedPreviewColor(key),
                    _ => throw new ArgumentOutOfRangeException(nameof(paintMode), paintMode, null)
                };
            }
            return colorMap;
        }
        
        /// <summary>
        /// This method is used to generate texture colors for the passed values.
        /// </summary>
        /// <param name="map">The biome map.</param>
        /// <param name="colorMap">The texture's colors.</param>
        /// <param name="paintMode">The terrain painting mode.</param>
        /// <param name="borderCulling">The optional border to remove from the map.</param>
        /// <exception cref="InvalidOperationException">Thrown if the map and color values are not the same size.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if there is an unhandled
        /// painting mode.</exception>
        public static void GenerateTextureColors(this BiomeMap map, Color[] colorMap, TerrainPaintingMode paintMode, int borderCulling=0) {
            if(colorMap.Length != map.GetBorderCulledValuesCount(borderCulling)) {
                throw new InvalidOperationException(
                    "The provided color map is not the save size as the requested color map!");
            }
            foreach(var key in map.BorderCulledKeys(borderCulling)) {
                if(paintMode == TerrainPaintingMode.Material) break;
                var colorKey = (key.y-borderCulling)*map.GetBorderCulledSize(borderCulling)+(key.x-borderCulling);
                colorMap[colorKey] = paintMode switch {
                    TerrainPaintingMode.BiomeColors => map.GetBiomeColor(key),
                    TerrainPaintingMode.BlendedBiomeColors => map.GetBlendedBiomeColor(key),
                    TerrainPaintingMode.NoisePreviewColors => map.PreviewColor(key),
                    TerrainPaintingMode.BlendedNoisePreviewColors => map.BlendedPreviewColor(key),
                    _ => throw new ArgumentOutOfRangeException(nameof(paintMode), paintMode, null)
                };
            }
        }

    }
}