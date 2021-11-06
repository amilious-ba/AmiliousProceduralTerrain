using System;
using System.Threading;

namespace Amilious.Threading {
    
    public class ReusableFuture<T> {
        
        private volatile FutureState _state;
        private Action<T> _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback;
        private bool _errorMain;
        private Action _cancelCallback;
        private bool _cancelMain;
        private Func<CancellationToken,T> _processMethod;
        private bool _executeAfterCancel;
        private CancellationTokenSource _tokenSource;
        
        /// <summary>
        /// Gets the state of the ReusableFuture.
        /// </summary>
        public FutureState State { get { return _state; } }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="ReusableFuture{T}"/> class.
        /// </summary>
        public ReusableFuture() {
            _state = FutureState.Pending;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T> OnSuccess(Action<T> callback, bool useMainThread = true) {
            _successCallback = callback;
            _successMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T> OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        public ReusableFuture<T> OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        public ReusableFuture<T> OnProcess(Func<CancellationToken,T> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value, as long
        /// as it is still in the Pending state.
        /// </summary>
        public void Process() {
            if(_processMethod == null) {
                throw new InvalidOperationException("Cannot process a future that has no process assigned.");
            }
            if(_state == FutureState.Processing) {
                _executeAfterCancel = true;
                Cancel();
                return;
            }
            _state = FutureState.Processing;
            _tokenSource = new CancellationTokenSource();
            _executeAfterCancel = false;
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    // Directly call the Impl version to avoid the state validation of the public method
                    AssignImpl(_processMethod(_tokenSource.Token));
                    _tokenSource.Token.ThrowIfCancellationRequested();
                }
                catch(OperationCanceledException) {
                    CancelImpl();
                }
                catch(Exception e) {
                    // Directly call the Impl version to avoid the state validation of the public method
                    FailImpl(e);
                }finally {
                    _tokenSource.Dispose();
                    _tokenSource = null;
                    if(_executeAfterCancel) Process();
                }
            });
        }

        public void Cancel() {
            _tokenSource?.Cancel();
        }

        private void AssignImpl(T value) {
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke(value));
            else _successCallback.Invoke(value);
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_cancelCallback == null) return;
            if(_errorMain) Dispatcher.InvokeAsync(()=>_errorCallback.Invoke(error));
            else _errorCallback.Invoke(error);
        }

        private void CancelImpl() {
            _state = FutureState.Canceled;
            if(_cancelCallback == null) return;
            if(_cancelMain) Dispatcher.InvokeAsync(_cancelCallback);
            else _cancelCallback.Invoke();
        }
    }
    
    public class ReusableFuture<T,T2> {
        
        private volatile FutureState _state;
        private Action<T> _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback;
        private bool _errorMain;
        private Action _cancelCallback;
        private bool _cancelMain;
        private Func<T2,CancellationToken,T> _processMethod;
        private bool _executeAfterCancel;
        private T2 _executeInput;
        private CancellationTokenSource _tokenSource;
        
        /// <summary>
        /// Gets the state of the ReusableFuture.
        /// </summary>
        public FutureState State { get { return _state; } }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="ReusableFuture{T,T2}"/> class.
        /// </summary>
        public ReusableFuture() {
            _state = FutureState.Pending;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2> OnSuccess(Action<T> callback, bool useMainThread = true) {
            _successCallback = callback;
            _successMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2> OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        public ReusableFuture<T,T2> OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        public ReusableFuture<T,T2> OnProcess(Func<T2,CancellationToken,T> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value, as long
        /// as it is still in the Pending state.
        /// </summary>
        public void Process(T2 inputValue) {
            if(_processMethod == null) {
                throw new InvalidOperationException("Cannot process a future that has no process assigned.");
            }
            if(_state == FutureState.Processing) {
                _executeAfterCancel = true;
                _executeInput = inputValue;
                Cancel();
                return;
            }
            _state = FutureState.Processing;
            _tokenSource = new CancellationTokenSource();
            _executeAfterCancel = false;
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    AssignImpl(_processMethod(inputValue,_tokenSource.Token));
                    _tokenSource.Token.ThrowIfCancellationRequested();
                }catch(OperationCanceledException) {CancelImpl();
                }catch(Exception e) {FailImpl(e);
                }finally {
                    _tokenSource.Dispose();
                    _tokenSource = null;
                    if(_executeAfterCancel) Process(_executeInput);
                }
            });
        }

        public void Cancel() { _tokenSource?.Cancel(); }

        private void AssignImpl(T value) {
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke(value));
            else _successCallback.Invoke(value);
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_cancelCallback == null) return;
            if(_errorMain) Dispatcher.InvokeAsync(()=>_errorCallback.Invoke(error));
            else _errorCallback.Invoke(error);
        }

        private void CancelImpl() {
            _state = FutureState.Canceled;
            if(_cancelCallback == null) return;
            if(_cancelMain) Dispatcher.InvokeAsync(_cancelCallback);
            else _cancelCallback.Invoke();
        }
    }
    
}