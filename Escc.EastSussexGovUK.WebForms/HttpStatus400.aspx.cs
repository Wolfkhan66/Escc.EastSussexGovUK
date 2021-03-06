using System.Web.UI;
using Escc.EastSussexGovUK.Skins;
using Escc.EastSussexGovUK.Views;

namespace Escc.EastSussexGovUK.WebForms
{
    /// <summary>
    /// Error page for an unhandled exception
    /// </summary>
    public partial class HttpStatus400 : Page
    {
        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, System.EventArgs e)
        {
            var skinnable = Master as BaseMasterPage;
            if (skinnable != null)
            {
                skinnable.Skin = new CustomerFocusSkin(ViewSelector.CurrentViewIs(MasterPageFile));
                css.Attributes["class"] = skinnable.Skin.TextContentClass;
            }

            // Return the correct HTTP status code
            new Web.HttpStatus().BadRequest();

            // Set the page title
            Page.Title = "Bad request";
        }

    }
}
