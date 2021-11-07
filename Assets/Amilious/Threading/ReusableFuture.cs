using System;
using System.Threading;
using UnityEngine;

namespace Amilious.Threading {
    
    /// <summary>
    /// This class is used to execute a process on a background thread.
    /// </summary>
    /// <seealso cref="ReusableFuture{T}"/>
    /// <seealso cref="ReusableFuture{T,T2}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3,T4}"/>
    public class ReusableFuture {
        
        private volatile FutureState _state;
        private Action _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback = Debug.LogError;
        private bool _errorMain = true;
        private Action _cancelCallback;
        private bool _cancelMain;
        private Func<CancellationToken, bool> _processMethod;
        private bool _executeAfterCancel;
        private CancellationTokenSource _tokenSource;
        
        /// <summary>
        /// Gets the state of the ReusableFuture.
        /// </summary>
        public FutureState State { get { return _state; } }
        

        /// <summary>
        /// Initializes a new instance of the <see cref="ReusableFuture"/> class.
        /// </summary>
        public ReusableFuture() {
            _state = FutureState.Pending;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture OnSuccess(Action callback, bool useMainThread = true) {
            _successCallback = callback;
            _successMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture is canceled.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke when processing the ReusableFuture.
        /// </summary>
        /// <param name="processMethod">The method that will be invoked to process the ReusableFuture.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture OnProcess(Func<CancellationToken, bool> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value
        /// <remarks>This will cancel a previous process if it is still being executed.</remarks>
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

        /// <summary>
        /// This method is used to cancel
        /// <returns>True if the process was canceled, otherwise returns false if
        /// the process was not being executed.</returns>
        /// </summary>
        public bool Cancel() {
            if(_tokenSource == null) return false;
            _tokenSource?.Cancel();
            return true;
        }

        private void AssignImpl(bool success) {
            if(!success)FailImpl(new InvalidOperationException("The result was false."));
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke());
            else _successCallback.Invoke();
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_errorCallback == null) return;
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
    
    /// <summary>
    /// This class is used to execute a process on a background thread.
    /// </summary>
    /// <typeparam name="T">The type of value that will be generated by the process.</typeparam>
    /// <seealso cref="ReusableFuture"/>
    /// <seealso cref="ReusableFuture{T,T2}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3,T4}"/>
    public class ReusableFuture<T> {
        
        private volatile FutureState _state;
        private Action<T> _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback = Debug.LogError;
        private bool _errorMain = true;
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
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
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
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T> OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture is canceled.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T> OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke when processing the ReusableFuture.
        /// </summary>
        /// <param name="processMethod">The method that will be invoked to process the ReusableFuture.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T> OnProcess(Func<CancellationToken,T> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value
        /// <remarks>This will cancel a previous process if it is still being executed.</remarks>
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

        /// <summary>
        /// This method is used to cancel
        /// <returns>True if the process was canceled, otherwise returns false if
        /// the process was not being executed.</returns>
        /// </summary>
        public bool Cancel() {
            if(_tokenSource == null) return false;
            _tokenSource?.Cancel();
            return true;
        }

        private void AssignImpl(T value) {
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke(value));
            else _successCallback.Invoke(value);
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_errorCallback == null) return;
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
    
    /// <summary>
    /// This class is used to execute a process on a background thread.
    /// </summary>
    /// <typeparam name="T">The type of value that will be generated by the process.</typeparam>
    /// <typeparam name="T2">The first type of value that will be used to generate the value.</typeparam>
    /// <seealso cref="ReusableFuture"/>
    /// <seealso cref="ReusableFuture{T}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3,T4}"/>
    public class ReusableFuture<T,T2> {
        
        private volatile FutureState _state;
        private Action<T> _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback = Debug.LogError;
        private bool _errorMain = true;
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
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
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
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2> OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture is canceled.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2> OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke when processing the ReusableFuture.
        /// </summary>
        /// <param name="processMethod">The method that will be invoked to process the ReusableFuture.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2> OnProcess(Func<T2,CancellationToken,T> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value
        /// <param name="inputValue">The first value that will be used to process the ReusableFuture's value</param>
        /// <remarks>This will cancel a previous process if it is still being executed.</remarks>
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

        /// <summary>
        /// This method is used to cancel
        /// <returns>True if the process was canceled, otherwise returns false if
        /// the process was not being executed.</returns>
        /// </summary>
        public bool Cancel() {
            if(_tokenSource == null) return false;
            _tokenSource?.Cancel();
            return true;
        }

        private void AssignImpl(T value) {
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke(value));
            else _successCallback.Invoke(value);
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_errorCallback == null) return;
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
    
    /// <summary>
    /// This class is used to execute a process on a background thread.
    /// </summary>
    /// <typeparam name="T">The type of value that will be generated by the process.</typeparam>
    /// <typeparam name="T2">The first type of value that will be used to generate the value.</typeparam>
    /// <typeparam name="T3">The second type of value that will be used to generate the value.</typeparam>
    /// <seealso cref="ReusableFuture"/>
    /// <seealso cref="ReusableFuture{T}"/>
    /// <seealso cref="ReusableFuture{T,T2}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3,T4}"/>
    public class ReusableFuture<T,T2,T3> {
        
        private volatile FutureState _state;
        private Action<T> _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback = Debug.LogError;
        private bool _errorMain = true;
        private Action _cancelCallback;
        private bool _cancelMain;
        private Func<T2,T3,CancellationToken,T> _processMethod;
        private bool _executeAfterCancel;
        private T2 _executeInput;
        private T3 _executeInput2;
        private CancellationTokenSource _tokenSource;
        
        /// <summary>
        /// Gets the state of the ReusableFuture.
        /// </summary>
        public FutureState State { get { return _state; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReusableFuture{T,T2,T3}"/> class.
        /// </summary>
        public ReusableFuture() {
            _state = FutureState.Pending;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3> OnSuccess(Action<T> callback, bool useMainThread = true) {
            _successCallback = callback;
            _successMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3> OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture is canceled.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3> OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke when processing the ReusableFuture.
        /// </summary>
        /// <param name="processMethod">The method that will be invoked to process the ReusableFuture.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3> OnProcess(Func<T2,T3,CancellationToken,T> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value
        /// <param name="inputValue">The first value that will be used to process the ReusableFuture's value</param>
        /// <param name="inputValue2">The second value that will be used to process the ReusableFuture's value</param>
        /// <remarks>This will cancel a previous process if it is still being executed.</remarks>
        /// </summary>
        public void Process(T2 inputValue, T3 inputValue2) {
            if(_processMethod == null) {
                throw new InvalidOperationException("Cannot process a future that has no process assigned.");
            }
            if(_state == FutureState.Processing) {
                _executeAfterCancel = true;
                _executeInput = inputValue;
                _executeInput2 = inputValue2;
                Cancel();
                return;
            }
            _state = FutureState.Processing;
            _tokenSource = new CancellationTokenSource();
            _executeAfterCancel = false;
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    AssignImpl(_processMethod(inputValue,inputValue2,_tokenSource.Token));
                    _tokenSource.Token.ThrowIfCancellationRequested();
                }catch(OperationCanceledException) {CancelImpl();
                }catch(Exception e) {FailImpl(e);
                }finally {
                    _tokenSource.Dispose();
                    _tokenSource = null;
                    if(_executeAfterCancel) Process(_executeInput,_executeInput2);
                }
            });
        }

        /// <summary>
        /// This method is used to cancel
        /// <returns>True if the process was canceled, otherwise returns false if
        /// the process was not being executed.</returns>
        /// </summary>
        public bool Cancel() {
            if(_tokenSource == null) return false;
            _tokenSource?.Cancel();
            return true;
        }

        private void AssignImpl(T value) {
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke(value));
            else _successCallback.Invoke(value);
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_errorCallback == null) return;
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
    
    /// <summary>
    /// This class is used to execute a process on a background thread.
    /// </summary>
    /// <typeparam name="T">The type of value that will be generated by the process.</typeparam>
    /// <typeparam name="T2">The first type of value that will be used to generate the value.</typeparam>
    /// <typeparam name="T3">The second type of value that will be used to generate the value.</typeparam>
    /// <typeparam name="T4">The third type of value that will be used to generate the value.</typeparam>
    /// <seealso cref="ReusableFuture"/>
    /// <seealso cref="ReusableFuture{T}"/>
    /// <seealso cref="ReusableFuture{T,T2}"/>
    /// <seealso cref="ReusableFuture{T,T2,T3}"/>
    public class ReusableFuture<T,T2,T3,T4> {
        
        private volatile FutureState _state;
        private Action<T> _successCallback;
        private bool _successMain;
        private Action<Exception> _errorCallback = Debug.LogError;
        private bool _errorMain = true;
        private Action _cancelCallback;
        private bool _cancelMain;
        private Func<T2,T3,T4,CancellationToken,T> _processMethod;
        private bool _executeAfterCancel;
        private T2 _executeInput;
        private T3 _executeInput2;
        private T4 _executeInput3;
        private CancellationTokenSource _tokenSource;
        
        /// <summary>
        /// Gets the state of the ReusableFuture.
        /// </summary>
        public FutureState State { get { return _state; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReusableFuture{T,T2,T3,T4}"/> class.
        /// </summary>
        public ReusableFuture() {
            _state = FutureState.Pending;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture value is retrieved successfully.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3,T4> OnSuccess(Action<T> callback, bool useMainThread = true) {
            _successCallback = callback;
            _successMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture has an error.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3,T4> OnError(Action<Exception> callback, bool useMainThread = true) {
            _errorCallback = callback;
            _errorMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke if the ReusableFuture is canceled.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="useMainThread">If true the callback will be invoked on the main thread,
        /// otherwise the callback will be executed on the worker thread.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3,T4> OnCancel(Action callback, bool useMainThread = true) {
            _cancelCallback = callback;
            _cancelMain = useMainThread;
            return this;
        }

        /// <summary>
        /// Adds a new callback to invoke when processing the ReusableFuture.
        /// </summary>
        /// <param name="processMethod">The method that will be invoked to process the ReusableFuture.</param>
        /// <returns>The future so additional calls can be chained together.</returns>
        public ReusableFuture<T,T2,T3,T4> OnProcess(Func<T2,T3,T4,CancellationToken,T> processMethod) {
            _processMethod = processMethod;
            return this;
        }

        /// <summary>
        /// Begins running a given function on a background thread to resolve the ReusableFuture's value
        /// <param name="inputValue">The first value that will be used to process the ReusableFuture's value</param>
        /// <param name="inputValue2">The second value that will be used to process the ReusableFuture's value</param>
        /// <param name="inputValue3">The third value that will be used to process the ReusableFuture's value</param>
        /// <remarks>This will cancel a previous process if it is still being executed.</remarks>
        /// </summary>
        public void Process(T2 inputValue, T3 inputValue2, T4 inputValue3) {
            if(_processMethod == null) {
                throw new InvalidOperationException("Cannot process a future that has no process assigned.");
            }
            if(_state == FutureState.Processing) {
                _executeAfterCancel = true;
                _executeInput = inputValue;
                _executeInput2 = inputValue2;
                _executeInput3 = inputValue3;
                Cancel();
                return;
            }
            _state = FutureState.Processing;
            _tokenSource = new CancellationTokenSource();
            _executeAfterCancel = false;
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    AssignImpl(_processMethod(inputValue,inputValue2,inputValue3,_tokenSource.Token));
                    _tokenSource.Token.ThrowIfCancellationRequested();
                }catch(OperationCanceledException) {CancelImpl();
                }catch(Exception e) {FailImpl(e);
                }finally {
                    _tokenSource.Dispose();
                    _tokenSource = null;
                    if(_executeAfterCancel) Process(_executeInput,_executeInput2,_executeInput3);
                }
            });
        }

        /// <summary>
        /// This method is used to cancel
        /// <returns>True if the process was canceled, otherwise returns false if
        /// the process was not being executed.</returns>
        /// </summary>
        public bool Cancel() {
            if(_tokenSource == null) return false;
            _tokenSource?.Cancel();
            return true;
        }

        private void AssignImpl(T value) {
            _state = FutureState.Success;
            if(_successCallback == null) return;
            if(_successMain) Dispatcher.InvokeAsync(() => _successCallback.Invoke(value));
            else _successCallback.Invoke(value);
        }

        private void FailImpl(Exception error) {
            _state = FutureState.Error;
            if(_errorCallback == null) return;
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