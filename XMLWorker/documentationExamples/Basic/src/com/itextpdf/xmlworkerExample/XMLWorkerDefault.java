package com.itextpdf.xmlworkerExample;

import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

import com.itextpdf.text.Document;
import com.itextpdf.text.DocumentException;
import com.itextpdf.text.Element;
import com.itextpdf.text.Paragraph;
import com.itextpdf.text.pdf.PdfDiv;
import com.itextpdf.text.pdf.PdfWriter;
import com.itextpdf.tool.xml.ElementHandler;
import com.itextpdf.tool.xml.Writable;
import com.itextpdf.tool.xml.XMLWorkerHelper;
import com.itextpdf.tool.xml.pipeline.WritableElement;

public class XMLWorkerDefault {
	public static void main(String[] args) {
		defaultExample();
	}
	private static void defaultExample(){
		//create a new document
		Document document = new Document();
		try {
			//create a pdfwriter instance
			PdfWriter writer = PdfWriter.getInstance(document, new FileOutputStream("results/loremipsum.pdf"));
			writer.setInitialLeading(12.5f);
			//open the document
			document.open();
			//create a file inputstream to read the html file
			FileInputStream fis = new FileInputStream("htmls/loremipsum.htm");
			//use parseXHtml to parse the file read by the inputstream
			XMLWorkerHelper.getInstance().parseXHtml(writer, document, fis);
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (DocumentException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
		document.close();
	}
	private static void elementExample(){
		
		try {
			FileInputStream fis = new FileInputStream("htmls/loremipsum.htm");
			XMLWorkerHelper.getInstance().parseXHtml(new ElementHandler() {
			
				@Override
				public void add(Writable w) {
					// TODO Auto-generated method stub
					 if (w instanceof WritableElement) {
				            List<Element> elements = ((WritableElement)w).elements();
				            // write class names of elements to file
				            recursiveElements(elements);
				        }
				}
			},fis, null);
		} catch (IOException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
	}
	private static void recursiveElements(List<Element> elements){
		List<Element> els = new ArrayList<Element>();
		List<Element> els2 = new ArrayList<Element>();
		 for(Element e:elements){
         	System.out.append(e.getClass().getSimpleName()+"\n");
         	
         		
         		if(e instanceof PdfDiv){
         			els = ((PdfDiv)e).getContent();
         			System.out.append("div\n");
         			System.out.append(els.size()+"\n");
         		}else if(e instanceof Paragraph){
         			els2 = ((Paragraph)e).breakUp();
         			System.out.append(els2.size()+"\n");
         			System.out.append(((Paragraph)e).getContent()+"\n");
         		}
         		if(els.size()>0){
         			recursiveElements(els);
         		}
         	}
	}
}
