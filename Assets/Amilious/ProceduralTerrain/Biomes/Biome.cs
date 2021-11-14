using Amilious.Core.ScriptableObjects;
using Amilious.ProceduralTerrain.Erosion;
using Amilious.ProceduralTerrain.Noise;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Biomes {
    
    [CreateAssetMenu(menuName = "Amilious/Procedural Terrain/Biome", order = 3), HideMonoScript]
    public class Biome : GuidScriptableObject {
        
        [Space(30)]
        [Tooltip("This value contains the display name for the biome.")]
        public string biomeName;
        [Tooltip("This color will be used on a biome map.")]
        public Color biomeMapColor;
        [Tooltip("This object will be used to generate heights for this biome.")]
        public AbstractNoiseProvider noiseSettings;
        [Tooltip("This value is the min height for this biome.")]
        public float minHeight;
        [Tooltip("This value is the max height for this biome.")]
        public float maxHeight;
        [InfoBox("This is not yet implemented!")]
        [Tooltip("These terrain modifiers will be applied to the biome.")]
        public MapModifier[] mapModifiers;
        
    }
}