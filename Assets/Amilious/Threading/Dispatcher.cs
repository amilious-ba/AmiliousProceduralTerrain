using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Amilious.Threading {
    
    /// <summary>
    /// A system for dispatching code to execute on the main thread.
    /// </summary>
    [AddComponentMenu("Amilious/Thread/Dispatcher"), HideMonoScript]
    [InfoBox("This script is responsible for executing code on the main thead.")]
    public class Dispatcher : MonoBehaviour {

        private const string NO_DISPATCHER = "No Dispatcher exists in the scene. Actions will not be invoked!";

        [SerializeField] private bool useAdvancedSettings;

        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        [Tooltip("The queue will be emptied if it exceeds this amount.")]
        private int maxQueueSize = 500;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private int dontInvokeIfOverMs = 2;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private int maxInvokesPerUpdate = 5;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private int skippedUpdates = 2;
        
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
            if (!_instanceExists) { Debug.LogError(NO_DISPATCHER); return; }
            if (IsMainThread) action();
            else Actions.Enqueue(action);
        }

        /// <summary>
        /// Queues an action to be invoked on the main game thread and blocks the
        /// current thread until the action has been executed.
        /// </summary>
        /// <param name="action">The action to be queued.</param>
        public static void Invoke(Action action) {
            if (!_instanceExists) { Debug.LogError(NO_DISPATCHER); return; }
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
            if(Actions.IsEmpty) return;
            if(useAdvancedSettings) AdvancedDequeue();
            else StandardDequeue();
        }

        private static void StandardDequeue() {
            while(!Actions.IsEmpty) { if(Actions.TryDequeue(out var action))action(); }
        }

        private readonly Stopwatch _actionTimer = new Stopwatch();
        private int _updatesSkipped;
        private int _invokesThisUpdate;
        
        private void AdvancedDequeue() {
            if(skippedUpdates > 0) {
                _updatesSkipped++;
                if(_updatesSkipped < skippedUpdates) return;
                _updatesSkipped = 0;
            }
            //empty the queue if it is over the threshold
            if(maxQueueSize>=0 && Actions.Count > maxQueueSize) {
                StandardDequeue();
                return;
            }
            _actionTimer.Restart();
            _invokesThisUpdate = 0;
            while(!Actions.IsEmpty&&_actionTimer.ElapsedMilliseconds<dontInvokeIfOverMs&&
                  (maxInvokesPerUpdate<0||_invokesThisUpdate<maxInvokesPerUpdate)) {
                if(Actions.TryDequeue(out var action))action();
                if(maxInvokesPerUpdate> 0) _invokesThisUpdate++;
            }
            _actionTimer.Stop();
        }
        
    }
}