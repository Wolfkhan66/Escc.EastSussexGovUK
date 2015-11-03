﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI.HtmlControls;
using EsccWebTeam.Data.Web;

namespace EsccWebTeam.EastSussexGovUK.MasterPages.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public partial class HeaderDesktop : System.Web.UI.UserControl
    {
        readonly EastSussexGovUKContext _siteContext = new EastSussexGovUKContext();
        private string _textSizeUrl;

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            BaseUrl();

            AtoZ();

            Menu();

            TextSize();

            Season();
        }

        private void Season()
        {
            var month = DateTime.UtcNow.Month;
            if (month == 12 || month == 1 || month == 2)
            {
                this.season.Attributes["class"] += " winter";
            }
            if (month == 3 || month == 4 || month == 5)
            {
                this.season.Attributes["class"] += " spring";
            }
            if (month == 6 || month == 7 || month == 8)
            {
                this.season.Attributes["class"] += " summer";
            }
            if (month == 9 || month == 10 || month == 11)
            {
                this.season.Attributes["class"] += " autumn";
            }
        }


        private void BaseUrl()
        {
            // Text size should have a default URL, but also support changing that URL in config
            _textSizeUrl = "/masterpages/textsize.aspx";
            var generalSettings = ConfigurationManager.GetSection("EsccWebTeam.EastSussexGovUK/GeneralSettings") as NameValueCollection;
            if (generalSettings != null && !String.IsNullOrEmpty(generalSettings["TextSizeUrl"]))
            {
                _textSizeUrl = ResolveUrl(generalSettings["TextSizeUrl"]);
            }

            // Preprend the base URL if specified (which it should be if this is a subdomain of eastsussex.gov.uk)
            if (_siteContext.BaseUrl != null)
            {
                var urlPrefix = _siteContext.BaseUrl.ToString().TrimEnd('/');
                this.logoSmallLink.Text = urlPrefix + this.logoSmallLink.Text;
                this.logoLargeLink.Text = urlPrefix + this.logoLargeLink.Text;
                this.contact.HRef = urlPrefix + this.contact.HRef;
                this.az.TargetFile = urlPrefix + this.az.TargetFile;
                this.searchUrl.Text = urlPrefix + this.searchUrl.Text;
                this.mobileHome.HRef = urlPrefix + this.mobileHome.HRef;
                this.mobileMenu.HRef = urlPrefix + this.mobileMenu.HRef;
                this.jobs.HRef = urlPrefix + this.jobs.HRef;
                this.socialcare.HRef = urlPrefix + this.socialcare.HRef;
                this.business.HRef = urlPrefix + this.business.HRef;
                this.community.HRef = urlPrefix + this.community.HRef;
                this.education.HRef = urlPrefix + this.education.HRef;
                this.environment.HRef = urlPrefix + this.environment.HRef;
                this.families.HRef = urlPrefix + this.families.HRef;
                this.leisure.HRef = urlPrefix + this.leisure.HRef;
                this.libraries.HRef = urlPrefix + this.libraries.HRef;
                this.transport.HRef = urlPrefix + this.transport.HRef;
                this.council.HRef = urlPrefix + this.council.HRef;
                this._textSizeUrl = urlPrefix + this._textSizeUrl;

                // Because these are resources loaded by the page, rather than linking off to another page, 
                // ensure the URL is protocol relative
                var colon = urlPrefix.IndexOf(":", StringComparison.OrdinalIgnoreCase);
                if (colon > -1)
                {
                    urlPrefix = urlPrefix.Substring(colon + 1);
                }
                this.logoSmall.Text = urlPrefix + this.logoSmall.Text;
                this.logoLarge.Text = urlPrefix + this.logoLarge.Text;
            }
            else
            {
                // If no base URL we're on the main server. Are we on the home page, and therefore need to de-link the logo?
                var isHomePage = (_siteContext.RequestUrl.AbsolutePath.StartsWith("/DEFAULT.", StringComparison.OrdinalIgnoreCase));
                this.logoSmallLinkOpen.Visible = !isHomePage;
                this.logoSmallLinkClose.Visible = !isHomePage;
                this.logoSmallLink.Visible = !isHomePage;
                this.logoSmallLinkEnd.Visible = !isHomePage;
                this.logoLargeLinkOpen.Visible = !isHomePage;
                this.logoLargeLinkClose.Visible = !isHomePage;
                this.logoLargeLink.Visible = !isHomePage;
                this.logoLargeLinkEnd.Visible = !isHomePage;
                this.logoSmallDivOpen.Visible = isHomePage;
                this.logoSmallDivEnd.Visible = isHomePage;

                // Ensure large logo is an absolute URL as it's part of schema.org metadata
                // Ensure small logo is an absolute URL because WorldPay loads and redisplays a confirmation page
                var urlPrefix = Request.Url.Scheme + "://" + Request.Url.Host;
                if (!Request.Url.IsDefaultPort) urlPrefix += ":" + Request.Url.Port;

                this.logoSmall.Text = urlPrefix + this.logoSmall.Text;
                this.logoLarge.Text = urlPrefix + this.logoLarge.Text;

                if (this.logoSmallLink.Visible) this.logoSmallLink.Text = urlPrefix + this.logoSmallLink.Text;
                if (this.logoLargeLink.Visible) this.logoLargeLink.Text = urlPrefix + this.logoLargeLink.Text;
            }

            // Ensure search is always an HTTPS URL to avoid a redirect
            this.searchUrl.Text = Iri.MakeAbsolute(new Uri(this.searchUrl.Text, UriKind.RelativeOrAbsolute)).ToString();
            if (this.searchUrl.Text.StartsWith("http://"))
            {
                this.searchUrl.Text = "https://" + this.searchUrl.Text.Substring(7);
            }

        }

        private void Menu()
        {
            // Highlight the current section using the breadcrumb trail. Compare just A-Z characters to avoid any special characters (eg &) breaking the logic.
            string selectedSection;
            if (!String.IsNullOrEmpty(Request.QueryString["section"]))
            {
                selectedSection = Request.QueryString["section"];
            }
            else
            {
                selectedSection = String.Empty;
                var provider = BreadcrumbTrailFactory.CreateTrailProvider();
                var trail = provider.BuildTrail();
                if (trail != null && trail.Count > 1)
                {
                    selectedSection = new List<string>(trail.Keys)[1].ToUpperInvariant();
                }
            }

            if (selectedSection.Length == 0) return;
            var normalise = new Regex("[^A-Z]");
            selectedSection = normalise.Replace(selectedSection, String.Empty);

            HtmlContainerControl[] sections = { this.jobs, this.socialcare, this.business, this.community, this.education, this.environment, this.families, this.leisure, this.libraries, this.transport, this.council };
            foreach (var section in sections)
            {
                if (selectedSection == normalise.Replace(section.InnerText.Replace("<br />", " ").ToUpperInvariant(), String.Empty))
                {
                    section.Attributes["class"] = "selected";
                }
            }
        }

        private void AtoZ()
        {
            // Support filters for the A-Z
            if (this._siteContext.RequestUrl.AbsolutePath.StartsWith("/atoz/default"))
            {
                if (!String.IsNullOrEmpty(Request.QueryString["autoPostback"])) this.az.AddQueryStringParameter("autoPostback", Request.QueryString["autoPostback"]);
                else if (String.IsNullOrEmpty(Request.QueryString["azq"])) this.az.AddQueryStringParameter("autoPostback", "on"); // default to on if form not submitted (ie: not a postback) 
                if (!String.IsNullOrEmpty(Request.QueryString["acc"])) this.az.AddQueryStringParameter("acc", Request.QueryString["acc"]);
                if (!String.IsNullOrEmpty(Request.QueryString["ae"])) this.az.AddQueryStringParameter("ae", Request.QueryString["ae"]);
                if (!String.IsNullOrEmpty(Request.QueryString["ah"])) this.az.AddQueryStringParameter("ah", Request.QueryString["ah"]);
                if (!String.IsNullOrEmpty(Request.QueryString["al"])) this.az.AddQueryStringParameter("al", Request.QueryString["al"]);
                if (!String.IsNullOrEmpty(Request.QueryString["ar"])) this.az.AddQueryStringParameter("ar", Request.QueryString["ar"]);
                if (!String.IsNullOrEmpty(Request.QueryString["aw"])) this.az.AddQueryStringParameter("aw", Request.QueryString["aw"]);

                // If no search term specified we must be viewing headings by letter, so highlight the letter
                if (String.IsNullOrEmpty(Request.QueryString["azq"]))
                {
                    if (!String.IsNullOrEmpty(Request.QueryString["index"]))
                    {
                        this.az.SelectedChar = Request.QueryString["index"];
                    }
                    else if (String.IsNullOrEmpty(Request.QueryString.ToString()))
                    {
                        this.az.SelectedChar = "a";
                    }
                }
            }
        }

        private void TextSize()
        {
            // If the current top-level domain is not *.eastsussex.gov.uk, either the page to change text size won't be there or it
            // will change the text size for the wrong domain, so just hide the links. However we want the links available on internal
            // copies of the main site, so check for hostnames without a ., which must be internal servers.
            var host = Request.Url.Host;
            if (!String.IsNullOrEmpty(Request.QueryString["host"])) host = Request.QueryString["host"];
            if (host.Contains(".") && !host.Contains(".eastsussex.gov.uk"))
            {
                this.textSize.Visible = false;
                return;
            }

            int baseTextSize = _siteContext.TextSize;

            if (baseTextSize > 1)
            {
                // Add the link to make it smaller again
                var smallerText = new HtmlAnchor();
                smallerText.HRef = this._textSizeUrl + "?textsize=" + (baseTextSize - 1).ToString(CultureInfo.InvariantCulture);
                smallerText.Attributes["class"] = "zoom-out screen";
                smallerText.Attributes["rel"] = "nofollow";
                smallerText.InnerText = "Make text smaller";
                this.textSize.Controls.Add(smallerText);
            }

            // Add the link to make text bigger
            if (baseTextSize < 3)
            {
                var biggerText = new HtmlAnchor();
                biggerText.HRef = this._textSizeUrl + "?textsize=" + (baseTextSize + 1).ToString(CultureInfo.InvariantCulture);
                biggerText.Attributes["class"] = "zoom-in screen";
                biggerText.Attributes["rel"] = "nofollow";
                biggerText.InnerText = "Make text bigger";
                this.textSize.Controls.Add(biggerText);
            }
        }
    }
}