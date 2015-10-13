using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace HomeReadingManager.MyClasses
{
    public class PrintHelpers
    {
    }
    class PDFWriterEvents : IPdfPageEvent
    {
        //string watermarkText;
        //float fontSize = 80f;
        private iTextSharp.text.Image waterMark;
        float xPosition = 300f;
        float yPosition = 800f;
        //float angle = 45f;

        //public PDFWriterEvents(string watermarkText, float fontSize = 80f, float xPosition = 300f, float yPosition = 400f, float angle = 45f)
        public PDFWriterEvents(iTextSharp.text.Image image, float xPosition = 300f, float yPosition = 400f)//, float angle = 45f
        {
            //this.watermarkText = watermarkText;
            this.waterMark = image;
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            //this.angle = angle;
        }

        public void OnOpenDocument(PdfWriter writer, Document document) { }
        public void OnCloseDocument(PdfWriter writer, Document document) { }
        public void OnStartPage(PdfWriter writer, Document document)
        {
            try
            {
                //PdfContentByte content = writer.GetContent();
                //content.AddImage(waterMark);
                PdfContentByte cb = writer.DirectContentUnder;
                PdfGState graphicsState = new PdfGState();
                graphicsState.FillOpacity = 0.2F;  // (or whatever)
                //set graphics state to pdfcontentbyte    
                cb.SetGState(graphicsState);
                cb.AddImage(waterMark);
                //BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.EMBEDDED);
                //cb.BeginText();
                //cb.SetColorFill(BaseColor.LIGHT_GRAY);
                //cb.SetFontAndSize(baseFont, fontSize);
                //cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, watermarkText, xPosition, yPosition, angle);
                //cb.EndText();
            }
            catch (DocumentException docEx)
            {
                throw docEx;
            }
        }
        public void OnEndPage(PdfWriter writer, Document document) { }
        public void OnParagraph(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnParagraphEnd(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnChapter(PdfWriter writer, Document document, float paragraphPosition, Paragraph title) { }
        public void OnChapterEnd(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnSection(PdfWriter writer, Document document, float paragraphPosition, int depth, Paragraph title) { }
        public void OnSectionEnd(PdfWriter writer, Document document, float paragraphPosition) { }
        public void OnGenericTag(PdfWriter writer, Document document, Rectangle rect, String text) { }

    }

    public partial class Footer : PdfPageEventHelper
    {
        private const float postScriptPointsPerMilimeter = 2.834645669f;
        private const float xPosition = 300f;
        private const float yPosition = 800f;

        public string student { get; set; }
        public string school { get; set; }
        public string semester { get; set; }
        public bool NewStudent { get; set; }
        public bool NewStudentH { get; set; }
       // public iTextSharp.text.Image waterMark { get; set; }
        //public bool UseWatermark { get; set; }
       
        private Font times10;
        private float[] widths;
        private int pageTotal, pageTotalH;

        public override void OnOpenDocument(PdfWriter writer, Document document)
        {
            times10 = FontFactory.GetFont("Times Roman");
            times10.Size = 9;
            times10.SetStyle("Italic");
            widths = new float[] { 3f, 2f, 3f };
            pageTotal = 0;
            pageTotalH = 0;
            NewStudent = false;
            NewStudentH = false;
            //UseWatermark = false;
        }

        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            if (NewStudent)
            {
                pageTotal = writer.PageNumber - 1;
                NewStudent = false;
            }
            base.OnEndPage(writer, doc);
            Paragraph footer = new Paragraph();
            footer.Font = times10;
            footer.Alignment = Element.ALIGN_RIGHT;
            PdfPTable footerTbl = new PdfPTable(3);
            footerTbl.TotalWidth = 180f * postScriptPointsPerMilimeter; ;
            footerTbl.SetWidths(widths);
            footerTbl.HorizontalAlignment = Element.ALIGN_CENTER;
            int pageNo = pageTotal > 0 ? writer.PageNumber % pageTotal : writer.PageNumber;
            if (pageNo == 0)
                pageNo = pageTotal;
            PdfPCell cell;
            
                cell = new PdfPCell();
                cell.Border = 0;
                cell.HorizontalAlignment = 0;
                if (pageNo > 1)
                {
                cell.Phrase = new Phrase(student, times10);
            }
            //else
            //{
            //    var path = System.Web.Hosting.HostingEnvironment.MapPath("~/Images");
            //    iTextSharp.text.Image imageLogo = iTextSharp.text.Image.GetInstance(path + "/DeptEd.jpg");
            //    imageLogo.Alignment = iTextSharp.text.Image.TEXTWRAP | iTextSharp.text.Image.ALIGN_LEFT;
            //    imageLogo.ScalePercent(20f);
            //    cell = new PdfPCell(imageLogo);
            //    cell.Border = 0;
            //    cell.HorizontalAlignment = 0;
            //}
            footerTbl.AddCell(cell);

            PdfPCell cell2 = new PdfPCell();
            cell2.Border = 0;
            cell2.HorizontalAlignment = 1;
            if (pageNo > 1)
                cell2.Phrase = new Phrase(semester, times10);
            footerTbl.AddCell(cell2);
            footerTbl.WriteSelectedRows(0, -1, doc.LeftMargin, 20, writer.DirectContent);

            PdfPCell cell3 = new PdfPCell();
            cell3.Border = 0;
            cell3.HorizontalAlignment = 2;
            if (pageNo > 1)
                cell3.Phrase = new Phrase("Page " + pageNo, times10);
            footerTbl.AddCell(cell3);
            if (pageNo > 1)
                footerTbl.WriteSelectedRows(0, -1, doc.LeftMargin, 20, writer.DirectContent);
            else
                footerTbl.WriteSelectedRows(0, -1, doc.LeftMargin, 30, writer.DirectContent);

            //if (UseWatermark)
            //{
            //    PdfContentByte cb = writer.DirectContentUnder;
            //    PdfGState graphicsState = new PdfGState();
            //    graphicsState.FillOpacity = 0.2F;
            //    cb.SetGState(graphicsState);
            //    cb.AddImage(waterMark);
            //}
        }

        public override void OnStartPage(PdfWriter writer, Document doc)
        {
            if (NewStudentH)
            {
                pageTotalH = writer.PageNumber - 1;
                NewStudentH = false;
            }
            base.OnStartPage(writer, doc);
            Paragraph header = new Paragraph();
            header.Font = times10;
            header.Alignment = Element.ALIGN_RIGHT;
            PdfPTable headerTbl = new PdfPTable(1);
            headerTbl.TotalWidth = 180f * postScriptPointsPerMilimeter; ;
            //headerTbl.SetWidths(widths);
            headerTbl.HorizontalAlignment = Element.ALIGN_RIGHT;
            int pageNo = pageTotalH > 0 ? writer.PageNumber % pageTotalH : writer.PageNumber;
            if (pageNo == 0)
                pageNo = pageTotalH;       
            PdfPCell cell = new PdfPCell();
            cell.Border = 0;
            cell.HorizontalAlignment = 2;
            if (pageNo > 1)
                cell.Phrase = new Phrase(school, times10);
            
            headerTbl.AddCell(cell);
            headerTbl.WriteSelectedRows(0, -1, doc.LeftMargin, doc.PageSize.Height - 5f, writer.DirectContent);
            //doc.Add(headerTbl);

            
        }

        //// write on end of each page
        //public override void OnEndPage(PdfWriter writer, Document document)
        //{
        //    base.OnEndPage(writer, document);
        //    PdfPTable tabFot = new PdfPTable(new float[] { 1F });
        //    PdfPCell cell;
        //    tabFot.TotalWidth = 300F;
        //    cell = new PdfPCell(new Phrase("Footer"));
        //    tabFot.AddCell(cell);
        //    tabFot.WriteSelectedRows(0, -1, 150, document.Bottom, writer.DirectContent);
        //}

        //write on close of document
        //public override void OnCloseDocument(PdfWriter writer, Document document)
        //{
        //    base.OnCloseDocument(writer, document);
        //}        
    }

}