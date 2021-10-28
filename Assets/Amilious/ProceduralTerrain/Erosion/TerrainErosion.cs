using System.Collections.Generic;
using Amilious.ProceduralTerrain.Noise;
using Amilious.Random;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Erosion {
    
    public static class TerrainErosion {

        public static void Rain(NoiseMap heightMap, string seed, int droplets, float erosionStrength, int dropletDiameter) {
            var random = new SeededRandom(seed);
            var dropletPositions = new List<Vector2Int>();
            var sqrDropletDistance = (int)((dropletDiameter / 2f) * (dropletDiameter / 2f));
            //generate droplets
            for(var i = 0; i < droplets; i++) {
                var droplet = new Vector2Int(random.IntRange(0, heightMap.Size),
                    random.IntRange(0, heightMap.Size));
                if(dropletDiameter == 1) heightMap.ClampReduce(droplet, erosionStrength);
                else dropletPositions.Add(droplet);
            }
            if(dropletDiameter == 1) return;
            //apply droplets that are greater than 1
            foreach(var key in heightMap) {
                foreach(var droplet in dropletPositions) {
                    var sqrDistance = (key - droplet).sqrMagnitude;
                    if(sqrDistance > sqrDropletDistance) continue;
                    var erosion = Mathf.InverseLerp(0, sqrDropletDistance, sqrDistance) * erosionStrength;
                    heightMap.ClampReduce(key, erosion);
                }
            }
            
        }

        public static void ThermalLandslide(NoiseMap heightMap, string seed, int droplets) {
            
        }

        private static void Subtract(NoiseMap heightMap, Vector2Int key, float amount) {
            
        }
        
    }
    
}