using OptionEdge.API.FlatTrade.Records;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Utf8Json;
using Websocket.Client;

namespace OptionEdge.API.FlatTrade
{
    public class Ticker
    {
        private bool _debug = false;

        private string _userId;
        private string _accessToken;
        
        private string _socketUrl = "wss://ws1.FlatTradeonline.com/NorenWS";

        private WebsocketClient _ws;

        private int _reconnectCounter = 0;

        bool _isReady;

        /// <summary>
        /// Token -> Mode Mapping
        /// </summary>
        private Dictionary<SubscriptionToken, string> _subscribedTokens;

        public delegate void OnConnectHandler();
        public delegate void OnReadyHandler();
        public delegate void OnCloseHandler();
        public delegate void OnTickHandler(Tick TickData);
        public delegate void OnReconnectHandler();       
        
        public event OnConnectHandler OnConnect;
        public event OnReadyHandler OnReady;
        public event OnCloseHandler OnClose;
        public event OnTickHandler OnTick;
        public event OnReconnectHandler OnReconnect;

        Func<int, bool> _shouldUnSubscribe = null;

        public Ticker(
            string userId, 
            string accessToken, 
            string socketUrl = null, 
            bool reconnect = false, 
            int reconnectInterval = 5, 
            int reconnectTries = 50, 
            bool debug = false)
        {
            _debug = debug;
            _userId = userId;
            _accessToken = accessToken;
            _subscribedTokens = new Dictionary<SubscriptionToken, string>();

            if (string.IsNullOrEmpty(socketUrl))
                _socketUrl = "wss://piconnect.flattrade.in/PiConnectWSTp/";
            else
                _socketUrl = socketUrl;

            var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(5),
                }
            });

            _ws = new WebsocketClient(new Uri( _socketUrl), factory);

            // By default 1 minite for ReconnectTimeout
            //_ws.ReconnectTimeout = TimeSpan.FromSeconds(60);

            _ws.ErrorReconnectTimeout = TimeSpan.FromSeconds(30);
            _ws.ReconnectionHappened.Subscribe(type => {

                if (_reconnectCounter > 0)
                {
                    if (_subscribedTokens.Count > 0)
                        ReSubscribe();
                }
                OnReconnect?.Invoke();
            });

            _ws.DisconnectionHappened.Subscribe(message =>
            {
                OnClose?.Invoke();
            });

            _ws.LostReconnectTimeout = TimeSpan.FromSeconds(15);

            _ws.MessageReceived.Subscribe(message =>
            {
                if (message.MessageType  == WebSocketMessageType.Text)
                {
                    var data = JsonSerializer.Deserialize<dynamic>(message.Text);
                    if (data["t"] == "c")
                    {

                    }
                    else if (data["t"] == "ck")
                    {
                        var connectAck = new ConnectAck(data);

                        if (connectAck.Status.ToUpper() != Constants.STATUS_OK.ToUpper())
                        {
                            if (_debug)
                            {
                                Utils.LogMessage($"Socker connection response was not successfull. Response: {connectAck.Status}");
                                return;
                            }
                        }
                        _isReady = true;

                        OnReady();

                        if (_debug)
                            Utils.LogMessage("Connection acknowledgement received. Websocket connected.");
                    }
                    else if (data["t"] == "tk" || data["t"] == "dk")
                    {
                        Tick tick = new Tick(data);
                        //AddToTickStore(tick);
                        OnTick(tick);
                    }
                    else if (data["t"] == "tf" || data["t"] == "df")
                    {
                        Tick tick = new Tick(data);
                        //FormatTick(ref tick);
                        OnTick(tick);
                    }
                    else
                    {
                        if (_debug)
                            Utils.LogMessage($"Unknown feed type: {data["t"]}");
                    }
                }
                else if (message.MessageType == WebSocketMessageType.Close)
                {
                    Close();
                }
            });
        }
        public void Close()
        {
            _subscribedTokens?.Clear();
            _ws.Stop( WebSocketCloseStatus.NormalClosure,"close");
            _ws.Dispose();
        }       

        //private void FormatTick(ref Tick tick)
        //{
        //    if (_tickStore.ContainsKey(tick.Exchange) && _tickStore[tick.Exchange].ContainsKey(tick.Token.Value))
        //    {
        //        ConcurrentDictionary<int, Tick> exchangeStore = null;
        //        Tick storedTick = null;

        //        _tickStore.TryGetValue(tick.Exchange, out exchangeStore);
        //        exchangeStore.TryGetValue(tick.Token.Value, out storedTick);

        //        tick.PreviousDayClose = storedTick.PreviousDayClose;
        //        tick.ChangeValue = storedTick.ChangeValue;

        //        if (tick.BuyPrice1 <= 0)
        //            tick.BuyPrice1 = storedTick.BuyPrice1;
        //        else
        //            storedTick.BuyPrice1 = tick.BuyPrice1;

        //        if (tick.BuyPrice2 <= 0)
        //            tick.BuyPrice2 = storedTick.BuyPrice2;
        //        else
        //            storedTick.BuyPrice2 = tick.BuyPrice2;

        //        if (tick.BuyPrice3 <= 0)
        //            tick.BuyPrice3 = storedTick.BuyPrice3;
        //        else
        //            storedTick.BuyPrice3 = tick.BuyPrice3;

        //        if (tick.BuyPrice4 <= 0)
        //            tick.BuyPrice4 = storedTick.BuyPrice4;
        //        else
        //            storedTick.BuyPrice4 = tick.BuyPrice4;

        //        if (tick.BuyPrice5 <= 0)
        //            tick.BuyPrice5 = storedTick.BuyPrice5;
        //        else
        //            storedTick.BuyPrice5 = tick.BuyPrice5;


        //        if (tick.SellPrice1 <= 0)
        //            tick.SellPrice1 = storedTick.SellPrice1;
        //        else
        //            storedTick.SellPrice1 = tick.SellPrice1;

        //        if (tick.SellPrice2 <= 0)
        //            tick.SellPrice2 = storedTick.SellPrice2;
        //        else
        //            storedTick.SellPrice2 = tick.SellPrice2;

        //        if (tick.SellPrice3 <= 0)
        //            tick.SellPrice3 = storedTick.SellPrice3;
        //        else
        //            storedTick.SellPrice3 = tick.SellPrice3;
                
        //        if (tick.SellPrice4 <= 0)
        //            tick.SellPrice4 = storedTick.SellPrice4;
        //        else
        //            storedTick.SellPrice4 = tick.SellPrice4;
                
        //        if (tick.SellPrice5 <= 0)
        //            tick.SellPrice5 = storedTick.SellPrice5;
        //        else
        //            storedTick.SellPrice5 = tick.SellPrice5;


        //        if (tick.BuyQty1 <= 0)
        //            tick.BuyQty1 = storedTick.BuyQty1;
        //        else
        //            storedTick.BuyQty1 = tick.BuyQty1;

        //        if (tick.BuyQty2 <= 0)
        //            tick.BuyQty2 = storedTick.BuyQty2;
        //        if (tick.BuyQty3 <= 0)
        //            tick.BuyQty3 = storedTick.BuyQty3;
        //        if (tick.BuyQty4 <= 0)
        //            tick.BuyQty4 = storedTick.BuyQty4;
        //        if (tick.BuyQty5 <= 0)
        //            tick.BuyQty5 = storedTick.BuyQty5;

        //        if (tick.SellQty1 <= 0)
        //            tick.SellQty1 = storedTick.SellQty1;
        //        else
        //            storedTick.SellQty1 = tick.SellQty1;

        //        if (tick.SellQty2 <= 0)
        //            tick.SellQty2 = storedTick.SellQty2;
        //        if (tick.SellQty3 <= 0)
        //            tick.SellQty3 = storedTick.SellQty3;
        //        if (tick.SellQty4 <= 0)
        //            tick.SellQty4 = storedTick.SellQty4;
        //        if (tick.SellQty5 <= 0)
        //            tick.SellQty5 = storedTick.SellQty5;


        //    }
        //}

        //private void AddToTickStore(Tick tick)
        //{
        //    if (!_tickStore.ContainsKey(tick.Exchange))
        //        _tickStore.TryAdd(tick.Exchange, new ConcurrentDictionary<int, Tick>());

        //    if (!_tickStore[tick.Exchange].ContainsKey(tick.Token.Value))
        //    {
        //        decimal close;
        //        decimal changeValue;
        //        if (tick.Close.HasValue && tick.Close.Value > 0)
        //        {
        //            close = tick.Close.Value;
        //            changeValue = tick.LastTradedPrice.Value - tick.Close.Value;
        //        }
        //        else
        //        {
        //            changeValue = tick.LastTradedPrice.Value * (tick.PercentageChange.Value / 100);
        //            if (Math.Sign(tick.PercentageChange.Value) == 1)
        //                close = tick.LastTradedPrice.Value - changeValue;
        //            else
        //                close = tick.LastTradedPrice.Value + changeValue;
        //        }

        //        tick.PreviousDayClose = close;
        //        tick.ChangeValue = changeValue;

        //        _tickStore[tick.Exchange].TryAdd(tick.Token.Value, tick);
        //    }

        //    FormatTick(ref tick);
        //}

        public bool IsConnected
        {
            get { return _ws.IsRunning; }
        }

        public bool IsReady
        {
            get { return _isReady; }
        }

        public void Connect()
        {          
            if (!IsConnected)
            {
                _ws.Start();

                Thread.Sleep(2000);
                var data = JsonSerializer.ToJsonString(new CreateWebsocketConnectionRequest
                {
                    AccessToken = _accessToken,
                    AccountId = _userId,
                    UserId = _userId,
                    RequestType = "c",
                    Source = "API"
                });

                _ws.Send(data);
            }
        }

        public void Subscribe(string exchnage, string mode, int[] tokens)
        {
            var subscriptionTokens = tokens.Select(token => new SubscriptionToken
            {
                Token = token,
                Exchange = exchnage
            }).ToArray();

            Subscribe(mode, subscriptionTokens);
        }

        public void Subscribe(string mode, SubscriptionToken[] tokens)
        {
            if (tokens.Length == 0) return;

            var subscriptionRequst = new SubscribeFeedDataRequest
            {
                SubscriptionTokens = tokens,
                RequestType = mode == Constants.TICK_MODE_QUOTE ? Constants.SUBSCRIBE_SOCKET_TICK_DATA_REQUEST_TYPE_MARKET : Constants.SUBSCRIBE_SOCKET_TICK_DATA_REQUEST_TYPE_DEPTH,
            };

            var requestJson = JsonSerializer.ToJsonString(subscriptionRequst);

            if (_debug) Utils.LogMessage(requestJson.Length.ToString());

            if (IsConnected)
                _ws.Send(requestJson);

            foreach (SubscriptionToken token in subscriptionRequst.SubscriptionTokens)
            {
                if (_subscribedTokens.ContainsKey(token))
                    _subscribedTokens[token] = mode; 
                else
                    _subscribedTokens.Add(token, mode);
            }
        }

        public void UnSubscribe(string exchnage, int[] tokens)
        {
            var subscriptionTokens = tokens.Select(token => new SubscriptionToken
            {
                Token = token,
                Exchange = exchnage
            }).ToArray();

            UnSubscribe(subscriptionTokens);
        }

        public void UnSubscribe(SubscriptionToken[] tokens)
        {
            if (tokens.Length == 0) return;

            var request = new UnsubscribeMarketDataRequest
            {
                SubscribedTokens = tokens.Where(x => _shouldUnSubscribe != null ? _shouldUnSubscribe.Invoke(x.Token) : true).ToArray(),
            };

            var requestJson = JsonSerializer.ToJsonString(request);

            if (_debug) Utils.LogMessage(requestJson.Length.ToString());

            if (IsConnected)
                _ws.Send(requestJson);

            foreach (SubscriptionToken token in request.SubscribedTokens)
                if (_subscribedTokens.ContainsKey(token))
                    _subscribedTokens.Remove(token);
        }

        private void ReSubscribe()
        {
            if (_debug) Utils.LogMessage("Resubscribing");

            SubscriptionToken[] allTokens = _subscribedTokens.Keys.ToArray();

            SubscriptionToken[] quoteTokens = allTokens.Where(key => _subscribedTokens[key] == Constants.TICK_MODE_QUOTE).ToArray();
            SubscriptionToken[] fullTokens = allTokens.Where(key => _subscribedTokens[key] == Constants.TICK_MODE_FULL).ToArray();

            UnSubscribe(quoteTokens);
            UnSubscribe(fullTokens);

            Subscribe(Constants.TICK_MODE_QUOTE, quoteTokens);
            Subscribe(Constants.TICK_MODE_FULL, fullTokens);
        }
    }
}
