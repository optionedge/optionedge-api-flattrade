///

// Development Test Class
// This is used to test the specific features as they are implemented

// If you are lookng for api samples, refer to FeaturesDemo.cs class

///
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Playwright;
using Newtonsoft.Json;
using OptionEdge.API.FlatTrade.Records;
using OtpNet;
using System;
using System.Security.AccessControl;
using System.Web;

namespace OptionEdge.API.FlatTrade.Samples
{
    public class DevTest
    {
        // apiKey, userId, logging setting
        static Settings _settings = new Settings();
       

        static FlatTrade _flatTrade;
        static Ticker _ticker;

        static string _cachedTokenFile = $"flattrade_cached_token_{DateTime.Now.ToString("dd_MMM_yyyy")}.txt";

        public async void Run()
        {
            try
            {
                // Read ApiKey, userId from Settings 
                // _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.dev.json"));
                _settings.ApiKey = Environment.GetEnvironmentVariable("FLATTRADE_API_KEY");
                _settings.ApiSecret= Environment.GetEnvironmentVariable("FLATTRADE_API_SECRET");
                _settings.AccountId=  Environment.GetEnvironmentVariable("FLATTRADE_ACCOUNT_ID");
                _settings.DOB = Environment.GetEnvironmentVariable("FLATTRADE_DOB");
                _settings.PAN = Environment.GetEnvironmentVariable("FLATTRADE_PAN");
                _settings.UserId = Environment.GetEnvironmentVariable("FLATTRADE_USER_ID");
                _settings.Password = Environment.GetEnvironmentVariable("FLATTRADE_PASSWORD");
                _settings.TOTPSecret = Environment.GetEnvironmentVariable("FLATTRADE_TOTP_SECRET");
                _settings.EnableLogging = true;

                // ==========================
                // Get Request Code through automation
                // ==========================


                // ==========================
                // Initialize
                // ==========================

                // Create new instance of FlatTrade client library
                _flatTrade = FlatTrade.CreateInstance(
                    _settings.UserId,
                    _settings.AccountId, 
                    _settings.ApiKey,
                    _settings.ApiSecret,
                    enableLogging: _settings.EnableLogging);

                var accessToken = ReadAccessTokenFromFile();

                if (string.IsNullOrEmpty(accessToken))
                {
                    var requestCode = await LoginAndGetRequestCode(false);

                    if (string.IsNullOrEmpty(requestCode))
                    {
                        Console.WriteLine("Unable to get request code.");
                        return;
                    }
                    accessToken = _flatTrade.RefreshAccessToken(requestCode).Result;
                    SaveAccessTokenToFile(accessToken);
                } else
                {
                    _flatTrade.SetAccessToken(accessToken);
                }


                // ==========================
                // Download Master Contracts
                // ==========================
                //_flatTrade.SaveMasterContracts(Constants.EXCHANGE_NSE, $"flattrade_master_contracts_{DateTime.Now.ToString("dd_MM_yyyy")}_{Constants.EXCHANGE_NSE}.csv");
                //_flatTrade.SaveMasterContracts(Constants.EXCHANGE_BSE, $"flattrade_master_contracts_{DateTime.Now.ToString("dd_MM_yyyy")}_{Constants.EXCHANGE_BSE}.csv");
                //_flatTrade.SaveMasterContracts(Constants.EXCHANGE_NFO, $"flattrade_master_contracts_{DateTime.Now.ToString("dd_MM_yyyy")}_{Constants.EXCHANGE_NFO}.csv");
                //_flatTrade.SaveMasterContracts(Constants.EXCHANGE_BFO, $"flattrade_master_contracts_{DateTime.Now.ToString("dd_MM_yyyy")}_{Constants.EXCHANGE_BFO}.csv");

                // or load Master Contracts into list
                //var masterContracts = _flatTrade.GetMasterContracts(Constants.EXCHANGE_NFO);
                //masterContracts = _flatTrade.GetMasterContracts(Constants.EXCHANGE_BFO);

                //var limits = _flatTrade.GetLimits();

                // ==========================
                // Place Order - Regular
                // ==========================
                //var placeRegularOrderResult = _flatTrade.PlaceOrder(new PlaceOrderParams
                //{
                //    Exchange = Constants.EXCHANGE_NFO,
                //    Remarks = "Test",
                //    PriceType = Constants.PRICE_TYPE_MARKET,
                //    Price = "0",
                //    ProductCode = Constants.PRODUCT_CODE_MIS,
                //    Quantity = "15",
                //    TransactionType = Constants.TRANSACTION_TYPE_BUY,
                //    InstrumentToken = 35458,
                //    TradingSymbol = "BANKNIFTY31JUL24C33000"
                //});


                // ==========================
                // Live Feeds Data Streaming
                // ==========================

                // Create Ticker instance
                // No need to provide the userId, apiKey, it will be automatically set
                _ticker = _flatTrade.CreateTicker();

                // Setup event handlers
                _ticker.OnTick += _ticker_OnTick;
                _ticker.OnConnect += _ticker_OnConnect;
                _ticker.OnClose += _ticker_OnClose;
                _ticker.OnReconnect += _ticker_OnReconnect;
                _ticker.OnReady += _ticker_OnReady;

                //Connect the ticker to start receiving the live feeds
                //DO NOT FORGOT TO CONNECT else you will not receive any feed

                _ticker.Connect();

                // var openInterest = _FlatTrade.GetOpenInterest(Constants.EXCHANGE_NFO, new int[] { 36303});

                // var contracts = _FlatTrade.GetMasterContracts(Constants.EXCHANGE_NFO).Result;

                // var history = _FlatTrade.GetHistoricalData(Constants.EXCHANGE_NFO, 37516, DateTime.Now.AddDays(-3), DateTime.Now, "5", false);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }

        private void SaveAccessTokenToFile(string accessToken)
        {
            File.WriteAllText(_cachedTokenFile, accessToken);
        }

        private string ReadAccessTokenFromFile()
        {
            string accessToken = null;
            if (File.Exists(_cachedTokenFile))
                accessToken = File.ReadAllText(_cachedTokenFile);

            return accessToken;
        }

        private void _ticker_OnReady()
        {
            Console.WriteLine("Socket connection authenticated. Ready to live stream feeds.");

            // subscribe for feeds when connection is authenticated, else 
            // no feeds data will be received from server.

            _ticker.Subscribe(Constants.TICK_MODE_FULL,
                new SubscriptionToken[]
                    {
                       new SubscriptionToken
                       {
                           Exchange = Constants.EXCHANGE_NSE,
                           Token = 26000
                       },
                       new SubscriptionToken
                       {
                           Exchange = Constants.EXCHANGE_NSE,
                           Token = 26009
                       },
                       new SubscriptionToken
                       {
                           Exchange = Constants.EXCHANGE_NFO,
                           Token = 40246
                       },
                       new SubscriptionToken
                       {
                           Exchange = Constants.EXCHANGE_BFO,
                           Token = 842575
                       },

                    });

            //_ticker.Subscribe(Constants.EXCHANGE_NSE, Constants.TICK_MODE_FULL, new int[] { 26000, 26009 });
        }

        private static void _ticker_OnTick(Tick TickData)
        {
            Console.WriteLine(JsonConvert.SerializeObject(TickData));
        }

        private static void _ticker_OnReconnect()
        {
            Console.WriteLine("Ticker reconnecting.");
        }

        private static void _ticker_OnNoReconnect()
        {
            Console.WriteLine("Ticker not reconnected.");
        }

        private static void _ticker_OnError(string Message)
        {
            Console.WriteLine("Ticker error." + Message);
        }

        private static void _ticker_OnClose()
        {
            Console.WriteLine("Ticker closed.");
        }

        private static void _ticker_OnConnect()
        {
            Console.WriteLine("Ticker connected.");           
        }

        private async Task<string> LoginAndGetRequestCode(bool showBrowser = true)
        {
            var requestCode = string.Empty;
            try
            {
                /// async-await crashing at this line
                /// hence using synchronous calls and thread sleep
                /// 
                var playwright = Playwright.CreateAsync().Result;
                
                var authUrl = "https://auth.flattrade.in/?app_key={{apikey}}".Replace("{{apikey}}", _settings.ApiKey);
                
                var browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = showBrowser == true ? false : true,
                }).Result;

                var page = browser.NewPageAsync().Result;
                var xx = page.GotoAsync(authUrl).Result;

                var yy = page.Locator("//*[@id=\"input-19\"]").FillAsync(_settings.UserId);
                Thread.Sleep(1000);
                var zz = page.Locator("//*[@id=\"pwd\"]").FillAsync(_settings.Password);
                Thread.Sleep(1000);

                var secret = _settings.TOTPSecret;
                var totpGenerator = new Totp(Base32Encoding.ToBytes(secret));
                var totp = totpGenerator.ComputeTotp();
                var a1 = page.Locator("//*[@id=\"pan\"]").FillAsync(totp);
                Thread.Sleep(1000);

                var a3 = page.Locator("//*[@id=\"sbmt\"]/span").ClickAsync();
                Thread.Sleep(1000);

                var a2 = page.WaitForURLAsync("https://auth.flattrade.in/localhost**");
                Thread.Sleep(2000);

                var uri = new Uri(page.Url);
                var queryStrings = QueryHelpers.ParseQuery(uri.Query);

                requestCode = queryStrings["code"];

                var x = browser.CloseAsync();

                playwright.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.GetBaseException().Message);
            }
            finally
            {
                //if (playwright != null)
                //    playwright.Dispose();
            }

            return requestCode;
        }
    }
}
