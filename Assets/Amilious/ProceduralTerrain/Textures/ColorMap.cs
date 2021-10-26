using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Textures {
    
    [Serializable, HideLabel]
    public class ColorMap {
        [SerializeField,TableColumnWidth(10)] private ColorMapValue[] colorMap;

        public void Reorder() {
            if(colorMap == null) return;
            colorMap = colorMap.OrderBy(x => x.LowestHeight).ToArray();
        }

        public Color GetColor(float value) {
            if(colorMap==null) return Color.black;
            foreach(var map in colorMap) {
                if(value > map.LowestHeight) continue;
                return map.Color;
            }
            return Color.white;
        }
    }

    [Serializable]
    public struct ColorMapValue {
        [SerializeField] float lowestHeight;
        [SerializeField] private Color color;
        public float LowestHeight { get { return lowestHeight; } }
        public Color Color { get { return color; } }
    }
}