using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Amilious.ProceduralTerrain.Textures {
    
    /// <summary>
    /// This class is used to map preview colors.
    /// </summary>
    [Serializable, HideLabel]
    public class ColorMap {
        
        [SerializeField,TableColumnWidth(10)] private ColorMapValue[] colorMap;

        /// <summary>
        /// This method is used to reorder the colors based on their height values.
        /// </summary>
        public void Reorder() {
            if(colorMap == null) return;
            colorMap = colorMap.OrderBy(x => x.LowestHeight).ToArray();
        }

        /// <summary>
        /// This method is used to get the color value for the given height value.
        /// </summary>
        /// <param name="value">The value you want to get the color for.</param>
        /// <returns>The color for the passed value.</returns>
        public Color GetColor(float value) {
            if(colorMap==null) return Color.black;
            foreach(var map in colorMap) {
                if(value > map.LowestHeight) continue;
                return map.Color;
            }
            return Color.white;
        }
    }

    /// <summary>
    /// This class is used by the <see cref="ColorMap"/> class.
    /// </summary>
    [Serializable]
    public struct ColorMapValue {
        [SerializeField] float lowestHeight;
        [SerializeField] private Color color;
        
        /// <summary>
        /// This property is used to get the lowest height.
        /// </summary>
        public float LowestHeight { get { return lowestHeight; } }
        
        /// <summary>
        /// This property is used to get the color.
        /// </summary>
        public Color Color { get { return color; } }
    }
}