﻿using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;
using Escc.Web;

namespace EsccWebTeam.EastSussexGovUK.MasterPages.Controls
{
    /// <summary>
    /// Error message to be displayed when a page is not found.
    /// </summary>
    public partial class Error404 : ErrorUserControl
    {
        /// <summary>
        /// Return 404 code and track 404 using Google Analytics
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            // Return the correct HTTP status code
            new HttpStatus().NotFound();

            // Set the page title
            Page.Title = "Page not found";

            if (Skin != null)
            {
                css.Attributes["class"] = Skin.TextContentClass;
            }

            // ...and track the 404 with Google Analytics
            var normalisedReferrer =  String.Empty;
            try
            {
                if (Request.UrlReferrer != null)
                {
                    normalisedReferrer = Request.UrlReferrer.ToString().Replace("'", "\'");
                }
            }
            catch (UriFormatException)
            {
                // Catch this error and simply ignore the referrer if it is an invalid URI, which can happen in a hacking scenario.
                // For example, if the request contains an invalid referring URL such as http://google.com', when you access the 
                // Request.UrlReferrer property .NET creates a Uri instance which throws this exception.
            }
            var script = "<script src=\"/js/404.jsx\" id=\"track-404\" data-request=\"" + Server.HtmlEncode(Regex.Replace(NormaliseRequestedPath(), @"[^A-Za-z0-9/\-_\.\?=:#+%]", String.Empty)) + "\" data-referrer=\"" + Server.HtmlEncode(Regex.Replace(normalisedReferrer, @"[^A-Za-z0-9/\-_\.\?=:#+%]", String.Empty)) + "\"></script>";

            // Put the tracking script in the javascript placeholder
            MasterPage rootMaster = Page.Master;
            while (rootMaster.Master != null)
            {
                rootMaster = rootMaster.Master;
            }
            if (rootMaster != null)
            {
                var placeholder = rootMaster.FindControl("javascript");
                if (placeholder != null)
                {
                    placeholder.Controls.Add(new LiteralControl(script));
                }
            }
        }

        /// <summary>
        /// Normalise the path which was originally requested - different depending on whether it came direct from IIS or via ASP.NET
        /// </summary>
        /// <returns></returns>
        private string NormaliseRequestedPath()
        {
            var requestedPath = String.Empty;
            if (!String.IsNullOrEmpty(Request.QueryString["aspxerrorpath"]))
            {
                requestedPath = Request.QueryString["aspxerrorpath"];
            }
            else
            {
                try
                {
                    string urlNotFound = Server.UrlDecode(Request.QueryString.ToString());
                    int? requestedUrlPos = null;
                    if (urlNotFound != null)
                    {
                        requestedUrlPos = urlNotFound.LastIndexOf("404;", StringComparison.OrdinalIgnoreCase);
                    }
                    if (requestedUrlPos.HasValue && requestedUrlPos.Value > -1)
                    {
                        requestedPath = new Uri(urlNotFound.Substring(requestedUrlPos.Value+4)).PathAndQuery;
                    }

                }
                catch (UriFormatException)
                {
                    // If someone's trying to feed in something other than an unrecognised URL, just show them the standard 404
                    return String.Empty;
                }
            }
            if (!String.IsNullOrEmpty(requestedPath))
            {
                requestedPath = requestedPath.Replace(Environment.NewLine, String.Empty).TrimEnd('/').ToLower(CultureInfo.CurrentCulture);
            }
            return requestedPath;
        }
    }
}