
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;

import com.itextpdf.text.Document;
import com.itextpdf.text.DocumentException;
import com.itextpdf.text.pdf.PdfWriter;
import com.itextpdf.tool.xml.Pipeline;
import com.itextpdf.tool.xml.XMLWorker;
import com.itextpdf.tool.xml.XMLWorkerHelper;
import com.itextpdf.tool.xml.exceptions.CssResolverException;
import com.itextpdf.tool.xml.html.Tags;
import com.itextpdf.tool.xml.parser.XMLParser;
import com.itextpdf.tool.xml.pipeline.css.CSSResolver;
import com.itextpdf.tool.xml.pipeline.css.CssResolverPipeline;
import com.itextpdf.tool.xml.pipeline.end.PdfWriterPipeline;
import com.itextpdf.tool.xml.pipeline.html.HtmlPipeline;
import com.itextpdf.tool.xml.pipeline.html.HtmlPipelineContext;

public class XMLWorkerDemo {

	private static String dirpath = "";

	public static void main(String[] args) throws FileNotFoundException, IOException, DocumentException, CssResolverException {
		if (args.length == 0) {
			System.out.println("Please specify 1 or more html files to convert:\njava XMLWorkerDemo file1.html file2.html");
			System.exit(0);
		}
		for (String file : args) {
			String name = "";
			if (file.endsWith(".html"))
				name = file.substring(0, file.length() - 5);
			else if (file.endsWith(".htm"))
				name = file.substring(0, file.length() - 4);
			else {
				System.out.println("Skipping " + file + ": only processing files with htm or html extension.");
				continue;
			}
			String outfile1 = name + "-1.pdf";
			String outfile2 = name + "-2.pdf";
			System.out.println("Converting " + file + " to " + outfile1 + ", using XMLWorkerHelper");
			convert1(file, outfile1);
			System.out.println("Converting " + file + " to " + outfile2 + ", using custom pipeline");
			convert2(file, outfile2);
		}
	}

	public static void convert1(String infile, String outfile) throws FileNotFoundException, IOException, DocumentException {
		Document document = new Document();
		PdfWriter writer = PdfWriter.getInstance(document,
				new FileOutputStream(outfile));
		document.open();
		
		// convert the HTML with the built-in convenience method
		XMLWorkerHelper.getInstance().parseXHtml(writer, document,
				new FileInputStream(infile));
		
		document.close();
	}
	
	public static void convert2(String infile, String outfile)
			throws FileNotFoundException, IOException, DocumentException,
			CssResolverException {
		Document document = new Document();
		PdfWriter writer = PdfWriter.getInstance(document,
				new FileOutputStream(outfile));
		document.open();

		HtmlPipelineContext htmlContext = new HtmlPipelineContext(null);
		htmlContext.setTagFactory(Tags.getHtmlTagProcessorFactory());
		CSSResolver cssResolver = XMLWorkerHelper.getInstance()
				.getDefaultCssResolver(true);

		Pipeline<?> pipeline = new CssResolverPipeline(cssResolver,
				new HtmlPipeline(htmlContext, new PdfWriterPipeline(document,
						writer)));
		XMLWorker worker = new XMLWorker(pipeline, true);
		XMLParser p = new XMLParser(worker);
		File input = new File(infile);
		p.parse(new InputStreamReader(new FileInputStream(input), "UTF-8"));

		document.close();

	}
}
