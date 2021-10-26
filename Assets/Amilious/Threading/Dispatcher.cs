using System;
using System.Collections.Concurrent;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Amilious.Threading {
    
    /// <summary>
    /// A system for dispatching code to execute on the main thread.
    /// </summary>
    [AddComponentMenu("Amilious/Thread/Dispatcher"), HideMonoScript]
    [InfoBox("This script is responsible for executing code on the main thead.")]
    public class Dispatcher : MonoBehaviour {
        
        private static Dispatcher _instance;

        // We can't use the behaviour reference from other threads, so we use a separate bool
        // to track the instance so we can use that on the other threads.
        private static bool _instanceExists;

        private static Thread _mainThread;
        //private static object _lockObject = new object();
        //private static readonly Queue<Action> _actions = new Queue<Action>();
        private static readonly ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();

        /// <summary>
        /// Gets a value indicating whether or not the current thread is the game's main thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread == _mainThread;

        /// <summary>
        /// Queues an action to be invoked on the main game thread.
        /// </summary>
        /// <param name="action">The action to be queued.</param>
        public static void InvokeAsync(Action action) {
            if (!_instanceExists) {
                Debug.LogError("No Dispatcher exists in the scene. Actions will not be invoked!");
                return;
            }
            if (IsMainThread) action();
            else Actions.Enqueue(action);
        }

        /// <summary>
        /// Queues an action to be invoked on the main game thread and blocks the
        /// current thread until the action has been executed.
        /// </summary>
        /// <param name="action">The action to be queued.</param>
        public static void Invoke(Action action) {
            if (!_instanceExists) {
                Debug.LogError("No Dispatcher exists in the scene. Actions will not be invoked!");
                return;
            }
            var hasRun = false;
            InvokeAsync(() => {action(); hasRun = true;});
            // Lock until the action has run
            while (!hasRun) Thread.Sleep(5);
        }

        private void Awake() {
            if (_instance) DestroyImmediate(this);
            else {
                _instance = this;
                _instanceExists = true;
                _mainThread = Thread.CurrentThread;
            }
        }

        private void OnDestroy() {
            if(_instance != this) return;
            _instance = null;
            _instanceExists = false;
        }

        private void Update() {
            while(!Actions.IsEmpty) if(Actions.TryDequeue(out var action))action();
        }
    }
}