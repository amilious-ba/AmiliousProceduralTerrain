using System;
using Amilious.ProceduralTerrain.Erosion;
using Amilious.ProceduralTerrain.Noise;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Biomes {
    
    
    /// <summary>
    /// This struct is used to store information about a
    /// specific biome.  The values here are what are used
    /// for generating the biome.
    /// </summary>
    [Serializable]
    public class BiomeInfo {
        
        /// <summary>
        /// This value will be false if the biome is invalid or does not exist.
        /// </summary>
        [HideInInspector] public bool validBiome;
        [TableColumnWidth(100, Resizable = false),HorizontalGroup("Biome")]
        [Tooltip("This value contains the display name for the biome.")]
        public string name;
        [HorizontalGroup("Biome", Width = 75),DisplayAsString, HideLabel]
        [Tooltip("This value is the biome id that will be used when generating a biome map.")]
        public int biomeId;
        [Tooltip("This color will be used on a biome map.")]
        public Color biomeMapColor;
        [Tooltip("This object will be used to generate heights for this biome.")]
        public AbstractNoiseProvider noiseSettings;
        public float minHeight;
        public float maxHeight;
        /*[Tooltip("These terrain modifiers will be applied to the biome.")]
        public MapModifier[] mapModifiers;*/

    }
}