using System;
using UnityEngine;

namespace Amilious.ProceduralTerrain.Assets.Shaders {
    
    public class Test : MonoBehaviour {

        [SerializeField] private ComputeShader computeShader;

        public RenderTexture renderTexture;


        private void Start() {
            renderTexture = new RenderTexture(256, 256, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            
            computeShader.SetTexture(0, "Result", renderTexture);
            computeShader.Dispatch(0, renderTexture.width/8, renderTexture.height/8,1);
            
            
        }
    }
    
    
}