﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using EsccWebTeam.Data.Web;
using EsccWebTeam.Data.Xml;
using EsccWebTeam.EastSussexGovUK.MasterPages.Remote;
using Microsoft.ApplicationBlocks.ExceptionManagement;

namespace EsccWebTeam.EastSussexGovUK.MasterPages.Controls
{
    /// <summary>
    /// Loads a section of the master page, either from a local usercontrol or remotely from the public website.
    /// </summary>
    /// <remarks>
    /// <para>Behaviour is controlled by the <c>EsccWebTeam.EastSussexGovUK/RemoteMasterPage</c> section in web.config.</para>
    /// <para>
    /// To load a control using this class, set the <see cref="Control"/> property to a string identifying the control you want to load.
    /// If <c>MasterPageControlUrl</c> is set in the <c>EsccWebTeam.EastSussexGovUK/RemoteMasterPage</c> section of web.config, this class
    /// will try to fetch HTML from that URL, passing your string instead of <c>{0}</c>. If the key is not present, the string is used as 
    /// the filename of a usercontrol in the ~/masterpages/Controls folder.
    /// </para>
    /// <para>In the following example, <c>ExampleControl</c> would be loaded from <c>http://www.eastsussex.gov.uk/masterpages/remote/control.aspx?control=ExampleControl</c>, 
    /// but if these <c>web.config</c> entries were not present it would be loaded from <c>~/masterpages/Controls/ExampleControl.ascx</c>.</para>
    /// <example><code>
    /// &lt;configuration&gt;
    ///   &lt;configSections&gt;
    ///     &lt;sectionGroup name=&quot;EsccWebTeam.EastSussexGovUK&quot;&gt;
    ///       &lt;section name=&quot;RemoteMasterPage&quot; type=&quot;System.Configuration.NameValueSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089&quot; /&gt;
    ///     &lt;/sectionGroup&gt;
    ///   &lt;/configSections&gt;
    ///
    ///   &lt;EsccWebTeam.EastSussexGovUK&gt;
    ///     &lt;RemoteMasterPage&gt;
    ///       &lt;add key=&quot;CacheMinutes&quot; value=&quot;60&quot; /&gt;
    ///       &lt;add key=&quot;MasterPageControlUrl&quot; value=&quot;http://www.eastsussex.gov.uk/masterpages/remote/control.aspx?control={0}&quot; /&gt;
    ///     &lt;/RemoteMasterPage&gt;
    ///   &lt;/EsccWebTeam.EastSussexGovUK&gt;
    /// &lt;/configuration&gt;
    /// </code></example>
    /// <para>The remote control is loaded from an ASPX page, which is just a host for the same usercontrol which would otherwise be loaded locally.
    /// The fetched HTML is saved in a local cache so that it is not requested remotely every time. The cache expires using the <c>CacheMinutes</c>
    /// setting in <c>web.config</c>.
    /// </para>
    /// <para>Requesting any page with the querystring <c>?ForceCacheRefresh=1</c> will cause the cached template to be updated even if it has not expired.</para>
    /// </remarks>
    public class MasterPageControl : PlaceHolder
    {
        /// <summary>
        /// Gets or sets an identifier for the control to load.
        /// </summary>
        /// <value>The control.</value>
        public string Control { get; set; }

        /// <summary>
        /// Gets or sets the provider for working out the current context within the site's information architecture.
        /// </summary>
        /// <value>
        /// The breadcrumb provider.
        /// </value>
        public IBreadcrumbProvider BreadcrumbProvider { get; set; }
        private static Dictionary<string, ManualResetEvent> waitFor;
        private NameValueCollection config = null;


        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        /// <exception cref="System.IO.FileNotFoundException">Thrown if cached file was not written</exception>
        protected override void CreateChildControls()
        {
            // Check that the Control property is set
            if (String.IsNullOrEmpty(this.Control)) throw new ArgumentNullException("Control", "Property 'Control' must be set for class MasterPageControl");

            // Get the configuration settings for remote master pages. Is this control in there?
            this.config = ConfigurationManager.GetSection("EsccWebTeam.EastSussexGovUK/RemoteMasterPage") as NameValueCollection;
            if (this.config != null && !String.IsNullOrEmpty(this.config["MasterPageControlUrl"]))
            {
                LoadRemoteControl();
            }
            else
            {
                LoadLocalControl();
            }
        }

        /// <summary>
        /// Loads a local usercontrol.
        /// </summary>
        /// <exception cref="System.Web.HttpException">Thrown if usercontrol does not exist</exception>
        private void LoadLocalControl()
        {
            this.Controls.Add(Page.LoadControl("~/masterpages/controls/" + this.Control + ".ascx"));
        }

        /// <summary>
        /// Fetches the master page control from a remote URL.
        /// </summary>
        private void LoadRemoteControl()
        {
            // Add the current section parsed from the breadcrumb trail
            var selectedSection = String.Empty;
            if (BreadcrumbProvider == null)
            {
                BreadcrumbProvider = BreadcrumbTrailFactory.CreateTrailProvider();
            }
            var trail = BreadcrumbProvider.BuildTrail();
            if (trail != null && trail.Count > 1)
            {
                selectedSection = new List<string>(trail.Keys)[1].ToUpperInvariant();
            }

            // Cache remote template elements using the application cache
            RemoteMasterPageCacheProviderBase cacheProvider = new RemoteMasterPageMemoryCacheProvider(Control, selectedSection);

            // Provide a way to force an immediate update of the cache
            var forceCacheRefresh = (Page.Request.QueryString["ForceCacheRefresh"] == "1");

            // Update the cached control if it's missing or too old
            if (!cacheProvider.CachedVersionExists || !cacheProvider.CachedVersionIsFresh || forceCacheRefresh)
            {
                RequestRemoteHtml(cacheProvider, selectedSection);
            }

            // Output the HTML
            this.Controls.Add(new LiteralControl(cacheProvider.ReadHtmlFromCache()));
        }

        /// <summary>
        /// Requests the remote HTML.
        /// </summary>
        /// <param name="cacheProvider">Strategy for caching the remote HTML.</param>
        /// <param name="selectedSection">The selected section.</param>
        private void RequestRemoteHtml(RemoteMasterPageCacheProviderBase cacheProvider, string selectedSection)
        {
            try
            {
                // Get the URL to request the cached control from.
                // Include text size so that header knows which links to apply
                var siteContext = new EastSussexGovUKContext();
                Uri urlToRequest = new Uri(String.Format(CultureInfo.CurrentCulture, config["MasterPageControlUrl"], this.Control));
                var applicationPath = HttpUtility.UrlEncode(HttpRuntime.AppDomainAppVirtualPath.ToLower(CultureInfo.CurrentCulture).TrimEnd('/'));
                urlToRequest = new Uri(Iri.PrepareUrlForNewQueryStringParameter(urlToRequest) + "section=" + selectedSection + "&host=" + Page.Request.Url.Host + "&textsize=" + siteContext.TextSize + "&path=" + applicationPath);

                // Create the request. Pass current user-agent so that library catalogue PCs can be detected by the remote script.
                var webRequest = (HttpWebRequest)WebRequest.Create(urlToRequest);
                webRequest.UseDefaultCredentials = true;
                webRequest.UserAgent = Page.Request.UserAgent;
                webRequest.Proxy = XmlHttpRequest.CreateProxy();

                // Prepare the information we'll need when the response comes back
                var state = new RequestState();
                state.Request = webRequest;
                state.CacheProvider = cacheProvider;

                // Kick off the request and, only if there's nothing already cached which we can use, wait for it to come back
                if (cacheProvider.SupportsAsync)
                {
                    RequestRemoteHtmlAsynchronous(cacheProvider, webRequest, state);
                }
                else
                {
                    RequestRemoteHtmlSynchronous(cacheProvider, webRequest);
                }
            }
            catch (UriFormatException ex)
            {
                throw new ConfigurationErrorsException(String.Format(CultureInfo.CurrentCulture, config["MasterPageControlUrl"], this.Control) + " is not a valid absolute URL", ex);
            }

        }

        private static void RequestRemoteHtmlSynchronous(RemoteMasterPageCacheProviderBase cacheProvider, HttpWebRequest webRequest)
        {
            try
            {
                using (var response = webRequest.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        cacheProvider.SaveRemoteHtmlToCache(responseStream);
                    }
                }
            }
            catch (WebException ex)
            {
                // Publish exception, otherwise it just disappears as async method has no calling code to throw to.
                ex.Data.Add("URL which failed", ex.Response.ResponseUri);
                ExceptionManager.Publish(ex);
            }
        }

        private void RequestRemoteHtmlAsynchronous(RemoteMasterPageCacheProviderBase cacheProvider, HttpWebRequest webRequest, RequestState state)
        {
            // Only wait for the response if there's nothing there at all.
            // If there is a cached version use that, but set the update running in the background.
            if (!cacheProvider.CachedVersionExists)
            {
                if (waitFor == null) waitFor = new Dictionary<string, ManualResetEvent>();
                waitFor[this.Control] = new ManualResetEvent(false);
                state.WaitForResponse = waitFor[this.Control];
            }

            webRequest.BeginGetResponse(new AsyncCallback(Response_Callback), state);
            if (!cacheProvider.CachedVersionExists) waitFor[this.Control].WaitOne(new TimeSpan(0, 0, 10));
        }


        /// <summary>
        /// Saves a remote template control as a local file
        /// </summary>
        /// <param name="result">The result.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if cache file cannot be written</exception>
        private static void Response_Callback(IAsyncResult result)
        {
            try
            {
                // Get the data we need from when the request was fired off
                var state = (RequestState)result.AsyncState;

                // Get the HTML from the response and save it to a temporary file
                using (var response = state.Request.EndGetResponse(result))
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        state.CacheProvider.SaveRemoteHtmlToCache(responseStream);
                    }
                }

                // Let the calling thread continue, if it was waiting for a file to be available
                if (state.WaitForResponse != null) state.WaitForResponse.Set();
            }
            catch (WebException ex)
            {
                // Publish exception, otherwise it just disappears as async method has no calling code to throw to.
                ex.Data.Add("URL which failed", ex.Response.ResponseUri);
                ExceptionManager.Publish(ex);
            }
        }

        /// <summary>
        /// Container object for state which needs to be passed to asynchronous callback
        /// </summary>
        private class RequestState
        {
            /// <summary>
            /// Gets or sets the web request which was fired off asynchronously.
            /// </summary>
            /// <value>The request.</value>
            public WebRequest Request { get; set; }

            /// <summary>
            /// If the calling thread is waiting for the response, gets or sets the object to release it
            /// </summary>
            /// <value>The wait for response.</value>
            public ManualResetEvent WaitForResponse { get; set; }

            /// <summary>
            /// Gets or sets the cache provider.
            /// </summary>
            /// <value>The cache provider.</value>
            public RemoteMasterPageCacheProviderBase CacheProvider { get; set; }
        }
    }
}