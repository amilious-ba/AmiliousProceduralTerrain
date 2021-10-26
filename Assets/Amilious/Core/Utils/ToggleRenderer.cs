using System;
using UnityEngine;

namespace Amilious.Core.Utils {
    
    [RequireComponent(typeof(MeshRenderer))]
    public class ToggleRenderer : MonoBehaviour {

        private MeshRenderer _renderer;

        private void Awake() {
            _renderer = GetComponent<MeshRenderer>();
        }


        public void Toggle() {
            _renderer!.enabled = !_renderer.enabled;
        }
    }
}
