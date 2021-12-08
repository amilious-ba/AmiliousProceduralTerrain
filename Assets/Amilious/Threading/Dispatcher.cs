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

        #region Inspector Variables
        
        [SerializeField] private bool useAdvancedSettings;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        [Tooltip("The queue will be emptied if it exceeds this amount.")]
        private int maxQueueSize = 500;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private bool useFixedUpdate;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private int dontInvokeIfOverMs = 2;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private int maxInvokesPerUpdate = 5;
        [SerializeField, ShowIf(nameof(useAdvancedSettings))]
        private int skippedUpdates = 2;
        
        #endregion
        
        #region Instance and Static Variables
        
        private static Dispatcher _instance;
        private static bool _instanceExists;
        private static Thread _mainThread;
        private static readonly ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
        private readonly Stopwatch _actionTimer = new Stopwatch();
        private int _updatesSkipped;
        private int _invokesThisUpdate;
        
        #endregion
        
        #region Properties

        /// <summary>
        /// Gets a value indicating whether or not the current thread is the game's main thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread == _mainThread;
        
        #endregion
        
        #region Public Methods

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
        
        #endregion
        
        #region Private Methods

        /// <summary>
        /// This is the first methods that is called by UNITY
        /// </summary>
        private void Awake() {
            if (_instance) DestroyImmediate(this);
            else {
                _instance = this;
                _instanceExists = true;
                _mainThread = Thread.CurrentThread;
            }
        }

        /// <summary>
        /// This method is called when the object is being destroyed
        /// </summary>
        private void OnDestroy() {
            if(_instance != this) return;
            _instance = null;
            _instanceExists = false;
        }

        /// <summary>
        /// This method is called by UNITY on update.
        /// </summary>
        private void Update() {
            if(useFixedUpdate) return;
            if(Actions.IsEmpty) return;
            if(useAdvancedSettings) AdvancedDequeue();
            else StandardDequeue();
        }

        /// <summary>
        /// This method is called by UNITY on fixed updates.
        /// </summary>
        private void FixedUpdate() {
            if(!useFixedUpdate) return;
            if(Actions.IsEmpty) return;
            if(useAdvancedSettings) AdvancedDequeue();
            else StandardDequeue();
        }

        /// <summary>
        /// This method is used to dequeue the queued tasks in the default way.
        /// </summary>
        private static void StandardDequeue() {
            while(!Actions.IsEmpty) { if(Actions.TryDequeue(out var action))action(); }
        }

        /// <summary>
        /// This method is used to dequeue the queued tasks using the advanced settings.
        /// </summary>
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
        
        #endregion
        
    }
}