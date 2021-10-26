using System.Collections.Generic;
using UnityEngine;

namespace Amilious.Saving {
    public abstract class AbstractSaveableEntity : MonoBehaviour {
        public abstract string GetUniqueIdentifier();
        public abstract void CaptureState(SaveData saveData);
        public abstract void MissingState();

        public abstract void RestoreState(SaveData saveData);
    }
}