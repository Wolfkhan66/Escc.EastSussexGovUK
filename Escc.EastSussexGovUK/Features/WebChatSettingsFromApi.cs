﻿using System;
using System.Collections.Generic;
using System.Configuration.Internal;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Escc.Net;
using Exceptionless;
using Newtonsoft.Json;

namespace Escc.EastSussexGovUK.Features
{
    /// <summary>
    /// Downloads web chat settings from a URL, expected (but not required) to be published as a Web API in Umbraco
    /// </summary>
    /// <seealso cref="Escc.EastSussexGovUK.Features.IWebChatSettingsService" />
    public class WebChatSettingsFromApi : IWebChatSettingsService
    {
        private readonly Uri _apiUrl;
        private readonly IProxyProvider _proxyProvider;
        private readonly ICacheStrategy<WebChatSettings> _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebChatSettingsFromApi" /> class.
        /// </summary>
        /// <param name="apiUrl">The API URL.</param>
        /// <param name="proxyProvider">The proxy provider.</param>
        /// <param name="cache">The cache.</param>
        /// <exception cref="System.ArgumentNullException">apiUrl</exception>
        public WebChatSettingsFromApi(Uri apiUrl, IProxyProvider proxyProvider, ICacheStrategy<WebChatSettings> cache)
        {
            if (apiUrl == null) throw new ArgumentNullException(nameof(apiUrl));
            _apiUrl = apiUrl;
            _proxyProvider = proxyProvider;
            _cache = cache;
        }

        /// <summary>
        /// Gets the settings for where to display the web chat feature
        /// </summary>
        /// <returns></returns>
        public WebChatSettings ReadWebChatSettings()
        {
            try
            {
                var cachedSettings =_cache?.ReadFromCache("WebChatSettingsUrl");
                if (cachedSettings != null) return cachedSettings;

                var client = new WebClient();
                if (_proxyProvider != null)
                {
                    client.Proxy = _proxyProvider.CreateProxy();
                }
                var json = Task.Run(async () => await client.DownloadStringTaskAsync(_apiUrl)).Result;
                var settings = JsonConvert.DeserializeObject<WebChatSettings>(json);

                _cache?.AddToCache("WebChatSettingsUrl", settings);    

                return settings;
            }
            catch (Exception exception)
            {
                // catch, report and suppress errors because we never want a check for web chat support to stop a page from loading
                var data = new Dictionary<string, object> {{"URL", _apiUrl.ToString()}};
                exception.ToExceptionless(true,data).Submit();
                return new WebChatSettings();
            }
        }
    }
}
