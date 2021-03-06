﻿using System;

namespace Escc.EastSussexGovUK.TemplateSource.MasterPages.Controls
{
    /// <summary>
    /// Standard scripts to include in the desktop template
    /// </summary>
    public partial class ScriptsDesktop : System.Web.UI.UserControl
    {
        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            var libraryContext = new LibraryCatalogueContext(Request.UserAgent);
            this.publicLibraries.Visible = libraryContext.RequestIsFromLibraryCatalogueMachine();
        }
    }
}