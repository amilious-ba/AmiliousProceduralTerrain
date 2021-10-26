using System;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Noise;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Erosion {
    
    [System.Serializable]
    public class MapModifier {

        //modifier selection
        [SerializeField] private ModifierType modifierType;

        //cut off modifier
        [SerializeField, LabelText("Height Range"), ShowIf(nameof(modifierType), ModifierType.CutOff)]
        [MinMaxSlider(-1, 1), Tooltip("The height values will be clamped within this range.")]
        private Vector2 cutoffHeights;

        //rain erosion
        [SerializeField, LabelText("Droplets"), ShowIf(nameof(modifierType), ModifierType.RainErosion)]
        [Min(1), Tooltip("This is the number of droplets that will erode the map.")]
        private int rainErosionDroplets = 20;
        [SerializeField, LabelText("Droplet Diameter"), ShowIf(nameof(modifierType), ModifierType.RainErosion)]
        [Min(1), Tooltip("This is the size of the rain drop in vertices.")]
        private int rainErosionDropletDiameter = 1;
        [SerializeField, LabelText("Droplets"), ShowIf(nameof(modifierType), ModifierType.RainErosion)]
        [Range(0,2), Tooltip("The strength of each droplet that will erode.")]
        private float rainErosionStrength = .01f;

        
        public void ApplyModifier(NoiseMap map, string seed) {
            switch(modifierType) {
                case ModifierType.CutOff: 
                    ApplyCutOff(map,seed); break;
                case ModifierType.RainErosion: 
                    TerrainErosion.Rain(map, seed, rainErosionDroplets, rainErosionStrength, rainErosionDropletDiameter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ApplyCutOff(NoiseMap map, string seed) {
            foreach(var key in map) {
                map.TrySetValue(key, Mathf.Clamp(map[key], cutoffHeights.x, cutoffHeights.y));
            }
        }

    }
}