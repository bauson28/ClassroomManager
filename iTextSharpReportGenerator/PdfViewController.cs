using System.Web.Mvc;

namespace iTextSharpReportGenerator
{
    /// <summary>
    /// Extends the controller with functionality for rendering PDF views
    /// </summary>
    public class PdfViewController : Controller
    {
        private readonly HtmlViewRenderer _htmlViewRenderer;
        private readonly StandardPdfRenderer _standardPdfRenderer;

        public PdfViewController()
        {
            this._htmlViewRenderer = new HtmlViewRenderer();
            this._standardPdfRenderer = new StandardPdfRenderer();
        }

        protected ActionResult ViewPdf(string pageTitle, string viewName, object model)
        {
            // Render the view html to a string
            var htmlText = this._htmlViewRenderer.RenderViewToString(this, viewName, model);

            // Let the html be rendered into a PDF document through iTextSharp
            byte[] buffer = _standardPdfRenderer.Render(htmlText, pageTitle);

            // Return the PDF as a binary stream to the client
            return new BinaryContentResult(buffer, "application/pdf");
        }
    }
}
