import com.itextpdf.text.*;
import com.itextpdf.text.pdf.PdfWriter;
import com.itextpdf.tool.xml.Pipeline;
import com.itextpdf.tool.xml.Tag;
import com.itextpdf.tool.xml.XMLWorker;
import com.itextpdf.tool.xml.XMLWorkerHelper;
import com.itextpdf.tool.xml.css.apply.ChunkCssApplier;
import com.itextpdf.tool.xml.css.apply.MarginMemory;
import com.itextpdf.tool.xml.css.apply.PageSizeContainable;
import com.itextpdf.tool.xml.html.CssAppliers;
import com.itextpdf.tool.xml.html.Tags;
import com.itextpdf.tool.xml.parser.XMLParser;
import com.itextpdf.tool.xml.pipeline.css.CSSResolver;
import com.itextpdf.tool.xml.pipeline.css.CssResolverPipeline;
import com.itextpdf.tool.xml.pipeline.end.PdfWriterPipeline;
import com.itextpdf.tool.xml.pipeline.html.*;

import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;


/**
 * Created with IntelliJ IDEA.
 * User: Jens
 * Date: 15/10/12
 * Time: 10:28
 * To change this template use File | Settings | File Templates.
 */
public class defaultConfig {

    public static void main(String[] args){
        defaultCfig();
    }
    private static void defaultCfig(){

        try {

            //FontFactory.registerDirectories();
            Document document = new Document();

        PdfWriter writer = null;
        try {
            writer = PdfWriter.getInstance(document, new FileOutputStream("thoreau1.pdf"));
        } catch (DocumentException e) {
            e.printStackTrace();
        } catch (FileNotFoundException e) {
            e.printStackTrace(); 
        }

        document.open();
        HtmlPipelineContext htmlContext = new HtmlPipelineContext(null);

        htmlContext.setTagFactory(Tags.getHtmlTagProcessorFactory());

        htmlContext.setImageProvider(new AbstractImageProvider() {

            public String getImageRootPath() {

                return "img/";

            }

        });

        htmlContext.setLinkProvider(new LinkProvider() {

            public String getLinkRoot() {

                return "http://tutorial.itextpdf.com/src/main/resources/html/";

            }

        });

        CSSResolver cssResolver = XMLWorkerHelper.getInstance().getDefaultCssResolver(true);
        Pipeline<?> pipeline = new CssResolverPipeline(cssResolver, new HtmlPipeline(htmlContext, new PdfWriterPipeline(document, writer)));
            //pipeline = new HtmlPipeline(htmlContext, new PdfWriterPipeline(document, writer));
            XMLWorker worker = new XMLWorker(pipeline, true);
        XMLParser p = new XMLParser(worker);

            FileInputStream fis = new FileInputStream("html/loremipsum.htm");

            //HTMLParsingProcess.class.getResourceAsStream();
            p.parse(fis, false);
            document.close();
        } catch (FileNotFoundException e) {
            e.printStackTrace();  //To change body of catch statement use File | Settings | File Templates.
        } catch (IOException e) {
            e.printStackTrace();  //To change body of catch statement use File | Settings | File Templates.
        }




    }

}
