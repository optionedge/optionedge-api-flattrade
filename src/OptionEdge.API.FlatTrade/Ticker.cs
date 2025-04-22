using Microsoft.Extensions.Logging;
using OptionEdge.API.FlatTrade.Records;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace OptionEdge.API.FlatTrade
{
    public class Ticker : IDisposable
    {
        private bool _debug = false;

        private string _userId;
        private string _accessToken;

        private string _socketUrl = "wss://ws1.FlatTradeonline.com/NorenWS";
        private bool _isReconnect = false;
        private int _interval = 5;
        private int _retries = 50;
        private int _retryCount = 0;
        private readonly Random _random = new Random();
        private DateTime _lastReconnectAttempt = DateTime.MinValue;
        private readonly TimeSpan _minReconnectInterval = TimeSpan.FromSeconds(1);

        System.Timers.Timer _timer;
        int _timerTick = 5;

        private System.Timers.Timer _timerHeartbeat;
        private int _timerHeartbeatInterval = 40000;
        
        private System.Timers.Timer _connectionHealthCheck;
        private int _connectionHealthCheckInterval = 30000; // Reduced to 30 seconds for faster detection
        private int _connectionTimeout = 10000;
        private DateTime _lastHeartbeatResponse = DateTime.MinValue;
        private DateTime _lastHeartbeatSent = DateTime.MinValue;
        private int _consecutiveHealthCheckFailures = 0;
        private int _maxHealthCheckFailures = 2; // Reduced to 2 for faster reconnection
        private bool _networkWasDown = false;
        private DateTime _lastForceReconnectAttempt = DateTime.MinValue;
        private readonly TimeSpan _minForceReconnectInterval = TimeSpan.FromSeconds(10);

        private IWebSocket _ws;

        bool _isReady;

        /// <summary>
        /// Token -> Mode Mapping
        /// </summary>
        private ConcurrentDictionary<SubscriptionToken, string> _subscribedTokens;

        public delegate void OnConnectHandler();
        public delegate void OnReadyHandler();
        public delegate void OnCloseHandler();
        public delegate void OnTickHandler(Tick TickData);
        public delegate void OnErrorHandler(string Message);
        public delegate void OnReconnectHandler();
        public delegate void OnNoReconnectHandler();
        
        public event OnConnectHandler OnConnect;
        public event OnReadyHandler OnReady;
        public event OnCloseHandler OnClose;
        public event OnTickHandler OnTick;
        public event OnErrorHandler OnError;
        public event OnReconnectHandler OnReconnect;
        public event OnNoReconnectHandler OnNoReconnect;

        Func<int, bool> _shouldUnSubscribe = null;
        ILogger _logger = null;

        public Ticker(
            string userId, 
            string accessToken, 
            string socketUrl = null, 
            bool reconnect = false, 
            int reconnectInterval = 5, 
            int reconnectTries = 50, 
            bool debug = false,
            ILogger logger = null)
        {
            _debug = debug;
            _userId = userId;
            _accessToken = accessToken;
            _subscribedTokens = new ConcurrentDictionary<SubscriptionToken, string>();
            _interval = reconnectInterval;
            _timerTick = reconnectInterval;
            _retries = reconnectTries;
            _isReconnect = reconnect;
            _logger = logger;

            if (string.IsNullOrEmpty(socketUrl))
                _socketUrl = "wss://ws1.FlatTradeonline.com/NorenWS";
            else
                _socketUrl = socketUrl;

            _ws = new WebSocket();

            _ws.OnConnect += _onConnect;
            _ws.OnData += _onData;
            _ws.OnClose += _onClose;
            _ws.OnError += _onError;

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _onTimerTick;
            _timer.Interval = 1000;

            _timerHeartbeat = new System.Timers.Timer();
            _timerHeartbeat.Elapsed += _timerHeartbeat_Elapsed;
            _timerHeartbeat.Interval = _timerHeartbeatInterval;
            
            _connectionHealthCheck = new System.Timers.Timer();
            _connectionHealthCheck.Elapsed += _connectionHealthCheck_Elapsed;
            _connectionHealthCheck.Interval = _connectionHealthCheckInterval;
        }

        private void _timerHeartbeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsConnected)
            {
                SendHeartBeat();
                _lastHeartbeatSent = DateTime.Now;
            }
        }
        
        private void _connectionHealthCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // Always log the current connection state for debugging
                LogInformation($"Connection health check - IsConnected: {IsConnected}, IsReady: {_isReady}, FailureCount: {_consecutiveHealthCheckFailures}");
                
                // Check if network is available
                bool networkAvailable = IsNetworkAvailable();
                
                if (!networkAvailable)
                {
                    LogWarning("Network appears to be down. Will attempt reconnection when network is available.");
                    _networkWasDown = true;
                    _consecutiveHealthCheckFailures++;
                    
                    // Even if network is down, try to reconnect after max failures
                    if (_consecutiveHealthCheckFailures >= _maxHealthCheckFailures)
                    {
                        LogWarning("Maximum failures reached even with network down. Attempting reconnection anyway.");
                        ForceReconnect();
                    }
                    return;
                }
                
                // If network was down but is now up, force reconnection
                if (_networkWasDown && networkAvailable)
                {
                    LogWarning("Network is back online. Forcing reconnection.");
                    _networkWasDown = false;
                    ForceReconnect();
                    return;
                }
                
                if (!IsConnected || !_isReady)
                {
                    _consecutiveHealthCheckFailures++;
                    LogWarning($"Connection health check: Not connected or not ready. Failure count: {_consecutiveHealthCheckFailures}");
                    
                    if (_isReconnect)
                    {
                        if (_consecutiveHealthCheckFailures >= _maxHealthCheckFailures)
                        {
                            LogWarning($"Maximum consecutive health check failures reached ({_maxHealthCheckFailures}). Forcing reconnection.");
                            ForceReconnect();
                        }
                        else
                        {
                            LogWarning("Attempting normal reconnect before reaching max failures.");
                            Reconnect();
                        }
                    }
                    else
                    {
                        LogWarning("Reconnect is disabled, but connection is not ready. Enabling reconnect.");
                        EnableReconnect();
                        Reconnect();
                    }
                    return;
                }
                
                // Reset failure counter if connection is healthy
                if (_consecutiveHealthCheckFailures > 0)
                {
                    LogInformation($"Connection is healthy. Resetting failure count from {_consecutiveHealthCheckFailures} to 0.");
                    _consecutiveHealthCheckFailures = 0;
                }
                
                // Check if we've received a response since the last heartbeat
                if (_lastHeartbeatSent != DateTime.MinValue &&
                    _lastHeartbeatResponse < _lastHeartbeatSent &&
                    (DateTime.Now - _lastHeartbeatSent).TotalMilliseconds > _connectionTimeout)
                {
                    LogWarning($"Connection health check: No heartbeat response received in {_connectionTimeout}ms");
                    
                    // Force reconnection
                    ForceReconnect();
                }
            }
            catch (Exception ex)
            {
                LogError("Error in connection health check", ex);
                
                // Even if there's an error, increment failure count and try to reconnect
                _consecutiveHealthCheckFailures++;
                if (_consecutiveHealthCheckFailures >= _maxHealthCheckFailures)
                {
                    try
                    {
                        LogWarning("Error in health check but still attempting reconnection.");
                        ForceReconnect();
                    }
                    catch (Exception reconnectEx)
                    {
                        LogError("Failed to force reconnect after health check error", reconnectEx);
                    }
                }
            }
        }
        
        private bool IsNetworkAvailable()
        {
            try
            {
                // Simple check - if we can resolve a DNS name, network is likely available
                // This is a basic check and might not work in all environments
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                LogError("Error checking network availability", ex);
                return true; // Assume network is available if we can't check
            }
        }
        
        private void ForceReconnect()
        {
            try
            {
                // Prevent too frequent force reconnection attempts
                if (DateTime.Now - _lastForceReconnectAttempt < _minForceReconnectInterval)
                {
                    LogWarning($"Force reconnect attempt too soon after previous attempt, delaying. Will try again in {(_minForceReconnectInterval - (DateTime.Now - _lastForceReconnectAttempt)).TotalSeconds:0.0} seconds.");
                    return;
                }
                
                _lastForceReconnectAttempt = DateTime.Now;
                
                LogWarning("FORCING RECONNECTION - Aggressive reconnect strategy");
                
                // Make sure reconnect is enabled
                if (!_isReconnect)
                {
                    LogWarning("Reconnect was disabled. Enabling it now.");
                    EnableReconnect();
                }
                
                // Close the current connection
                try
                {
                    if (IsConnected)
                    {
                        LogWarning("Closing existing connection before force reconnect");
                        _ws.Close(true);
                    }
                }
                catch (Exception closeEx)
                {
                    LogError("Error closing connection during force reconnect", closeEx);
                    // Continue anyway
                }
                
                // Reset state
                _isReady = false;
                _retryCount = 0;
                _lastReconnectAttempt = DateTime.MinValue; // Reset this to allow immediate reconnect
                
                // Stop and restart timers
                try
                {
                    _timer.Stop();
                    _timerHeartbeat.Stop();
                    _connectionHealthCheck.Stop();
                    
                    _timerTick = _interval;
                    _timer.Start();
                    _connectionHealthCheck.Start();
                }
                catch (Exception timerEx)
                {
                    LogError("Error managing timers during force reconnect", timerEx);
                    // Continue anyway
                }
                
                // Create a new WebSocket instance if the current one is in a bad state
                try
                {
                    if (_ws == null || (_ws.IsConnected() && !_isReady))
                    {
                        LogWarning("Creating new WebSocket instance");
                        
                        // Unsubscribe from old events
                        if (_ws != null)
                        {
                            _ws.OnConnect -= _onConnect;
                            _ws.OnData -= _onData;
                            _ws.OnClose -= _onClose;
                            _ws.OnError -= _onError;
                        }
                        
                        // Create new instance
                        _ws = new WebSocket();
                        
                        // Subscribe to events
                        _ws.OnConnect += _onConnect;
                        _ws.OnData += _onData;
                        _ws.OnClose += _onClose;
                        _ws.OnError += _onError;
                    }
                }
                catch (Exception wsEx)
                {
                    LogError("Error recreating WebSocket during force reconnect", wsEx);
                    // Continue anyway
                }
                
                // Directly call Connect to bypass the reconnection delay
                LogWarning("Calling Connect() directly from ForceReconnect");
                Connect();
                
                // Log the attempt
                LogWarning("Force reconnect attempt completed");
            }
            catch (Exception ex)
            {
                LogError("Error in ForceReconnect", ex);
                
                // Last resort - try to reconnect anyway
                try
                {
                    _ws = new WebSocket();
                    _ws.OnConnect += _onConnect;
                    _ws.OnData += _onData;
                    _ws.OnClose += _onClose;
                    _ws.OnError += _onError;
                    
                    _ws.Connect(_socketUrl);
                }
                catch (Exception lastEx)
                {
                    LogError("Last resort reconnection also failed", lastEx);
                }
            }
        }

        private void LogInformation(string message)
        {
            _logger?.LogInformation($"[Ticker] {message}");
        }
        
        private void LogWarning(string message)
        {
            _logger?.LogWarning($"[Ticker] {message}");
        }
        
        private void LogError(string message, Exception ex = null)
        {
            if (ex != null)
                _logger?.LogError(ex, $"[Ticker] {message}");
            else
                _logger?.LogError($"[Ticker] {message}");
        }
    
        private void SendHeartBeat()
        {
            try
            {
                LogInformation("Heartbeat triggered");

                if (!_ws.IsConnected())
                {
                    LogWarning("Unable to send heartbeat - socket not connected");
                    return;
                }
                string msg = @"{\""k\"": \""\"",\""t\"": \""h\""}";
                _ws.Send(msg);
            }
            catch (Exception ex)
            {
                LogError("Send Heartbeat error", ex);
            }
        }

        private void _onError(string Message)
        {
            try
            {
                _isReady = false;
                LogError($"WebSocket error: {Message}");
                _timerTick = _interval;
                _timer.Start();
                OnError?.Invoke(Message);
                
                if (_isReconnect && !IsConnected)
                {
                    Reconnect();
                }
            }
            catch (Exception ex)
            {
                LogError("Error in _onError handler", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                _timer?.Stop();
                _timerHeartbeat?.Stop();
                _connectionHealthCheck?.Stop();
                
                _timer?.Dispose();
                _timerHeartbeat?.Dispose();
                _connectionHealthCheck?.Dispose();
                
                if (_ws != null)
                {
                    _ws.OnConnect -= _onConnect;
                    _ws.OnData -= _onData;
                    _ws.OnClose -= _onClose;
                    _ws.OnError -= _onError;
                    
                    _ws.Close();
                }
                
                _subscribedTokens?.Clear();
            }
        }
        
        ~Ticker()
        {
            Dispose(false);
        }

        private void _onClose()
        {
            try
            {
                _isReady = false;
                LogInformation("WebSocket connection closed");
                _timer.Stop();
                _timerHeartbeat.Stop();
                _connectionHealthCheck.Stop();
                OnClose?.Invoke();
                
                if (_isReconnect)
                {
                    _timerTick = _interval;
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                LogError("Error in _onClose handler", ex);
            }
        }

        public void Close()
        {
            try
            {
                _isReady = false;
                LogInformation("Closing WebSocket connection");
                _subscribedTokens?.Clear();
                _ws?.Close();
                _timer.Stop();
                _timerHeartbeat.Stop();
                _connectionHealthCheck.Stop();
            }
            catch (Exception ex)
            {
                LogError("Error in Close method", ex);
            }
        }

        private readonly object _tickLock = new object();
        private readonly object _connectionLock = new object();
        private void _onData(byte[] Data, int Count, string MessageType)
        {
            try
            {
                _timerTick = _interval;

                if (MessageType == "Text")
                {
                    if (Count <= 0 || Data == null)
                    {
                        _logger?.LogWarning("Received empty data in _onData");
                        return;
                    }

                    var tick = JsonSerializer.Deserialize<Tick>(Data.Take(Count).ToArray(), 0);

                    if (_debug) LogInformation($"Data: {JsonSerializer.Serialize(tick)}");

                    if (tick == null)
                    {
                        _logger?.LogWarning("Failed to deserialize tick data");
                        return;
                    }

                    // Update heartbeat response time for any message received
                    _lastHeartbeatResponse = DateTime.Now;
                    
                    // Reset failure counter on successful message
                    _consecutiveHealthCheckFailures = 0;
                    
                    // 'ck' represents connect acknowledgement
                    if (tick.ResponseType == "ck")
                    {
                        lock (_tickLock)
                        {
                            if (!_isReady)
                            {
                                _isReady = true;
                                OnReady?.Invoke();
                            }
                        }
                        
                        if (_subscribedTokens.Count > 0)
                            ReSubscribe();
                            
                        if (_debug)
                            LogInformation("Connection acknowledgement received. Websocket connected.");
                    }
                    else if (tick.ResponseType == "tk" || tick.ResponseType == "dk")
                    {
                        OnTick?.Invoke(tick);
                    }
                    else if (tick.ResponseType == "tf" || tick.ResponseType == "df")
                    {
                        OnTick?.Invoke(tick);
                    }
                    else
                    {
                        if (_debug)
                            LogWarning($"Unknown feed type: {tick.ResponseType}");
                        
                        _logger?.LogInformation($"Unknown feed type: {tick.ResponseType}");
                    }
                }
                else if (MessageType == "Close")
                {
                    this.LogInformation("On Message 'Close'. Connection closed by server.");
                    _isReady = false;
                    
                    if (_isReconnect)
                    {
                        _timerTick = _interval;
                        _timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error in _onData: {ex.Message}\nStackTrace: {ex.StackTrace}");
                
                // Don't let exceptions break the connection
                if (_isReconnect && !IsConnected)
                {
                    _timerTick = _interval;
                    _timer.Start();
                }
            }
        }

        private void _onTimerTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timerTick--;
            if (_timerTick < 0)
            {
                _timer.Stop();
                if (_isReconnect)
                    Reconnect();
            }
            if (_debug) LogInformation($"Timer tick: {_timerTick}");
        }

        private void _onConnect()
        {
            LogInformation("WebSocket connected, sending authentication request");
            var data = JsonSerializer.ToJsonString(new CreateWebsocketConnectionRequest
            {
                AccessToken = _accessToken,
                AccountId = _userId,
                UserId = _userId,
                RequestType = "c",
                Source = "API"
            });

            _ws.Send(data);

            _retryCount = 0;
            _timerTick = _interval;
            _timer.Start();
            _timerHeartbeat.Start();
            _connectionHealthCheck.Start();

            OnConnect?.Invoke();
        }

        public bool IsConnected
        {
            get { return _ws.IsConnected(); }
        }

        public bool IsReady
        {
            get { return _isReady; }
        }

        public void Connect()
        {
            try
            {
                lock (_connectionLock)
                {
                    _timerTick = _interval;
                    _timer.Start();
                    
                    if (!IsConnected)
                    {
                        LogWarning($"Connecting to {_socketUrl}");
                        
                        // Try to connect with timeout
                        var connectTask = Task.Run(() => {
                            try {
                                _ws.Connect(_socketUrl);
                            }
                            catch (Exception connectEx) {
                                LogError("Error in WebSocket.Connect", connectEx);
                            }
                        });
                        
                        // Wait for connection with timeout
                        bool connectCompleted = Task.WaitAll(new[] { connectTask }, 5000);
                        
                        if (!connectCompleted)
                        {
                            LogWarning("Connect operation timed out after 5 seconds");
                        }
                        
                        // Check if connection was successful
                        if (IsConnected)
                        {
                            LogInformation("Connection successful");
                        }
                        else
                        {
                            LogWarning("Connection attempt failed");
                            
                            // Schedule another reconnect attempt
                            _timerTick = _interval;
                            _timer.Start();
                        }
                    }
                    else
                    {
                        LogInformation("Already connected, not reconnecting");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error in Connect", ex);
                
                // Still try to reconnect despite the error
                if (_isReconnect)
                {
                    _timerTick = _interval;
                    _timer.Start();
                }
            }
        }

        private void Reconnect()
        {
            try
            {
                lock (_connectionLock)
                {
                    // Check if network is available before attempting reconnection
                    if (!IsNetworkAvailable())
                    {
                        LogWarning("Network appears to be down. Will attempt reconnection when network is available.");
                        _networkWasDown = true;
                        return;
                    }
                    
                    // Prevent too frequent reconnection attempts
                    if (DateTime.Now - _lastReconnectAttempt < _minReconnectInterval)
                    {
                        LogInformation("Reconnect attempt too soon after previous attempt, delaying");
                        return;
                    }
                    
                    _lastReconnectAttempt = DateTime.Now;
                    
                    LogInformation($"Attempting to reconnect");
                    if (IsConnected && _isReady)
                    {
                        LogInformation($"Already connected and ready, no need to reconnect");
                        return;
                    }

                    if (_retryCount > _retries)
                    {
                        LogWarning($"Maximum retry count reached: {_retryCount} > {_retries}");
                        _ws.Close(true);
                        
                        // Reset retry count instead of disabling reconnect
                        // This allows the system to try again after a break
                        _retryCount = 0;
                        
                        // Only disable reconnect if explicitly requested
                        // DisableReconnect();
                        OnNoReconnect?.Invoke();
                    }
                    else
                    {
                        OnReconnect?.Invoke();
                        _retryCount += 1;
                        
                        // Close the connection if it's in a bad state
                        if (IsConnected && !_isReady)
                        {
                            LogWarning("Connection exists but is not ready. Closing before reconnect.");
                            _ws.Close(true);
                        }
                        
                        // Add jitter to prevent thundering herd problem
                        int jitter = _random.Next(0, 1000);
                        Thread.Sleep(jitter);
                        
                        // Attempt to connect
                        LogInformation($"Reconnect attempt #{_retryCount} of {_retries}");
                        Connect();
                        
                        // Use shorter backoff for initial retries, then exponential
                        if (_retryCount <= 3)
                        {
                            _timerTick = _interval;
                        }
                        else
                        {
                            // Exponential backoff with max of 60 seconds
                            _timerTick = (int)Math.Min(Math.Pow(1.5, _retryCount) * _interval, 60);
                        }
                        
                        LogInformation($"Next reconnect attempt in {_timerTick} seconds if needed");
                        _timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error in Reconnect", ex);
                
                // Still try to reconnect despite the error
                _timerTick = _interval;
                _timer.Start();
            }
        }

        public void Subscribe(string exchange, string mode, int[] tokens)
        {
            try
            {
                if (tokens == null || tokens.Length == 0)
                {
                    LogWarning("Attempted to subscribe with empty tokens array");
                    return;
                }
                
                LogInformation($"Subscribing {exchange}, {mode}, {JsonSerializer.ToJsonString(tokens)}");
                var subscriptionTokens = tokens.Select(token => new SubscriptionToken
                {
                    Token = token,
                    Exchange = exchange
                }).ToArray();

                Subscribe(mode, subscriptionTokens);
            }
            catch (Exception ex)
            {
                LogError("Error in Subscribe", ex);
            }
        }

        public void Subscribe(string mode, SubscriptionToken[] tokens)
        {
            try
            {
                if (tokens == null || tokens.Length == 0)
                {
                    LogWarning("Attempted to subscribe with empty tokens array");
                    return;
                }

                var subscriptionRequest = new SubscribeFeedDataRequest
                {
                    SubscriptionTokens = tokens,
                    RequestType = Constants.SUBSCRIBE_SOCKET_TICK_DATA_REQUEST_TYPE_DEPTH,
                };

                var requestJson = JsonSerializer.ToJsonString(subscriptionRequest);

                LogInformation($"Subscribe request: {requestJson}");

                if (IsConnected)
                {
                    LogInformation("Socket is connected, sending subscribe request.");
                    _ws.Send(requestJson);
                }
                else
                {
                    LogWarning("Cannot subscribe - socket not connected. Will subscribe on reconnect.");
                }

                foreach (SubscriptionToken token in subscriptionRequest.SubscriptionTokens)
                {
                    _subscribedTokens.AddOrUpdate(token, mode, (k, v) => mode);
                }
            }
            catch (Exception ex)
            {
                LogError("Error in Subscribe", ex);
            }
        }

        public void UnSubscribe(string exchange, int[] tokens)
        {
            try
            {
                if (tokens == null || tokens.Length == 0)
                {
                    LogWarning("Attempted to unsubscribe with empty tokens array");
                    return;
                }
                
                LogInformation($"Unsubscribing from {exchange}, tokens: {JsonSerializer.ToJsonString(tokens)}");
                var subscriptionTokens = tokens.Select(token => new SubscriptionToken
                {
                    Token = token,
                    Exchange = exchange
                }).ToArray();

                UnSubscribe(subscriptionTokens);
            }
            catch (Exception ex)
            {
                LogError("Error in UnSubscribe", ex);
            }
        }

        public void UnSubscribe(SubscriptionToken[] tokens)
        {
            try
            {
                if (tokens == null || tokens.Length == 0)
                {
                    LogWarning("Attempted to unsubscribe with empty tokens array");
                    return;
                }

                var request = new UnsubscribeMarketDataRequest
                {
                    SubscribedTokens = tokens.Where(x => _shouldUnSubscribe != null ? _shouldUnSubscribe.Invoke(x.Token) : true).ToArray(),
                };

                if (request.SubscribedTokens.Length == 0)
                {
                    LogWarning("No tokens to unsubscribe after applying filter");
                    return;
                }

                var requestJson = JsonSerializer.ToJsonString(request);

                if (_debug) LogInformation($"Unsubscribe request: {requestJson}");

                if (IsConnected)
                {
                    LogInformation($"Socket is connected. Unsubscribing {requestJson}");
                    _ws.Send(requestJson);
                }
                else
                {
                    LogWarning("Cannot unsubscribe - socket not connected");
                }

                foreach (SubscriptionToken token in request.SubscribedTokens)
                {
                    _subscribedTokens.TryRemove(token, out _);
                }
            }
            catch (Exception ex)
            {
                LogError("Error in UnSubscribe", ex);
            }
        }

        private void ReSubscribe()
        {
            try
            {
                LogInformation("Resubscribing to all tokens");
                
                if (_subscribedTokens.Count == 0)
                {
                    LogInformation("No tokens to resubscribe");
                    return;
                }

                SubscriptionToken[] allTokens = _subscribedTokens.Keys.ToArray();
                
                if (allTokens.Length == 0)
                {
                    LogInformation("No tokens found in dictionary");
                    return;
                }

                var tokensByMode = allTokens.GroupBy(token => _subscribedTokens[token]);
                
                foreach (var group in tokensByMode)
                {
                    string mode = group.Key;
                    SubscriptionToken[] tokens = group.ToArray();
                    
                    if (tokens.Length > 0)
                    {
                        LogInformation($"Resubscribing {tokens.Length} tokens with mode {mode}");
                        Subscribe(mode, tokens);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error in ReSubscribe", ex);
            }
        }

        public void EnableReconnect(int interval = 5, int retries = 50)
        {
            _logger?.LogInformation($"EnableReconnect: {interval}"); 
            _isReconnect = true;
            _interval = Math.Max(interval, 5);
            _retries = retries;

            _timerTick = _interval;
            if (IsConnected)
                _timer.Start();
        }

        public void DisableReconnect()
        {
            _logger?.LogInformation("Disable Reconnect");
            _isReconnect = false;
            if (IsConnected)
                _timer.Stop();
            _timerTick = _interval;
        }
    }
}
