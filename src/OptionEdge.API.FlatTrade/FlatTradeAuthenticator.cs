using OptionEdge.API.FlatTrade.Records;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade
{
    internal class FlatTradeAuthenticator : AuthenticatorBase
    {
        readonly string _apiKey;
        readonly string _apiSecret;
        readonly string _requestCode;

        readonly string _tokenUrl;
        readonly string _sessionIdEndpoint;
        readonly bool _enableLogging;

        Action<string> _onAccessTokenGenerated;
        Func<string> _cachedAccessTokenProvider;
        Action<string> _onAccessTokenUpdated;

        public FlatTradeAuthenticator(
            string apiKey, 
            string apiSecret, 
            string requestCode, 
            string tokenUrl,  
            bool enableLogging = false, 
            Action<string> onAccessTokenGenerated = null, 
            Func<string> cachedAccessTokenProvider = null, 
            Action<string> onAccessTokenUpdated = null) : base("")
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _requestCode = requestCode;
            _tokenUrl = tokenUrl;
            _enableLogging = enableLogging;
            _onAccessTokenGenerated = onAccessTokenGenerated;
            _cachedAccessTokenProvider = cachedAccessTokenProvider;
            _onAccessTokenUpdated = onAccessTokenUpdated;
        }

        protected override async ValueTask<Parameter> GetAuthenticationParameter(string accessToken)
        {
            var cachedToken = this.Token;

            if (string.IsNullOrEmpty(this.Token) && _cachedAccessTokenProvider != null)
            {
                cachedToken = _cachedAccessTokenProvider?.Invoke();
                this.Token = cachedToken;
            }

            if (string.IsNullOrEmpty(this.Token))
            {
                this.Token = await GetAccessToken();
                _onAccessTokenGenerated?.Invoke(this.Token);
            }

            _onAccessTokenUpdated?.Invoke(this.Token);

            //return new BodyParameter("jKey", this.Token, "application/json");

            var bearer = $"Bearer {Token}";
            return new HeaderParameter(KnownHeaders.Authorization, bearer);
        }
        async Task<string> GetAccessToken()
        {
            if (_enableLogging)
                Utils.LogMessage("Getting the Access Token (Session Id).");

            var options = new RestClientOptions(_tokenUrl);
            var restClient = new RestClient(options);

            var request = new RestRequest();

            var apiSecretSHA256 = Utils.GetSHA256($"{_apiKey}{_requestCode}{_apiSecret}");

            var apiTokenParams = new ApiTokenParams
            {
                ApiKey = _apiKey,
                RequestCode = _requestCode,
                ApiSecret = apiSecretSHA256,
            };

            request.AddStringBody(JsonConvert.SerializeObject(apiTokenParams), ContentType.Json);

            if (_enableLogging)
                Utils.LogMessage($"Calling encryption key endpoint: {apiTokenParams}");

            var apiTokenResult= await restClient.PostAsync<APITokenResult>(request);

            if (_enableLogging)
                Utils.LogMessage($"Encryption Key Result. Status: {apiTokenResult.Status}-{apiTokenResult.ErrorMessage}");

            if (apiTokenResult.Status == Constants.API_RESPONSE_STATUS_Not_OK)
                throw new Exception($"Error getting encryption key. Status: {apiTokenResult.Status}, Error Message: {apiTokenResult.ErrorMessage}");

            if (restClient != null) restClient.Dispose();

            return apiTokenResult?.Token;
        }
    }
}
