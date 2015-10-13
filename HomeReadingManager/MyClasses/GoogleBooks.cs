using HomeReadingManager.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace HomeReadingManager.MyClasses
{
    public class GoogleBooks
    {
        //public GoogleBooks{};
        public string isbn { get; set; }
        public string errorMessage { get; set; }
        public int userId { get; set; }
        public int productId { get; set; }
        public int libraryId { get; set; }
        public int levelsId { get; set; }
        public bool doLabels { get; set; }
        public int existingId { get; set; }
        public bool addStock { get; set; }
        public string physicalFolder { get; set; }

        public int GoogleSearch()
        {
            int productId = 0;
            string title = string.Empty, annotation = String.Empty, response = String.Empty;
            string imageUrl = String.Empty;
            string uri = "https:" + "//www.googleapis.com/books/v1/volumes?q=isbn:" + isbn + "&maxResults=1";
            string message = "Failed to contact title website.";
            try
            {
                HttpWebRequest req = WebRequest.Create(uri) as HttpWebRequest;
               
                req.KeepAlive = false;
               
                req.Method = "GET";
               
                HttpWebResponse resp = req.GetResponse() as HttpWebResponse;
               
                Encoding enc = System.Text.Encoding.GetEncoding(1252);
                StreamReader loResponseStream = new StreamReader(resp.GetResponseStream(), enc);
                response = loResponseStream.ReadToEnd();
                loResponseStream.Close();
                resp.Close();
               
                if (!string.IsNullOrEmpty(response))
                {
                    JObject o = JObject.Parse(response);
                    var jsonArray = o["items"];
                    
                    if (jsonArray != null)
                    {
                        foreach (var item in jsonArray)
                        {
                            if (item["volumeInfo"]["title"] != null)
                            {
                                title = item["volumeInfo"]["title"].ToString();
                                if (item["volumeInfo"]["subtitle"] != null)
                                    title += " " + item["volumeInfo"]["subtitle"].ToString();
                                if (item["volumeInfo"]["description"] != null)
                                    annotation = item["volumeInfo"]["description"].ToString();
                                if (item["volumeInfo"]["imageLinks"] != null && item["volumeInfo"]["imageLinks"]["thumbnail"] != null)
                                    imageUrl = item["volumeInfo"]["imageLinks"]["thumbnail"].ToString();

                                HomeReadingEntities dbContext = new HomeReadingEntities();
                               
                                if (existingId > 0)
                                {
                                    productId = existingId;
                                    message = "Failed to update title in database.";
                                    if (UpdateProductGoogle(dbContext, libraryId, existingId, title, annotation) && item["volumeInfo"]["authors"] != null)
                                    {
                                        DeleteAuthors(existingId, dbContext);
                                    }
                                }
                                else
                                {
                                    message = "Failed to add title to database.";
                                    productId = AddProductGoogle(dbContext, libraryId, isbn, title, annotation, doLabels);

                                }
                               
                                if (productId > 0 && item["volumeInfo"]["authors"] != null)
                                {
                                    if (existingId > 0)
                                        message = "Title added to database but not all details recorded.";
                                    else
                                        message = "Title updated but not all details recorded.";

                                    var authors = item["volumeInfo"]["authors"];
                                   
                                    foreach (var author in authors)
                                    {
                                        if (author != null)
                                        {
                                            ProdAuthor a = new ProdAuthor();
                                            a.Product_Id = productId;
                                            a.Author = author.ToString();
                                            a.Role_Id = 1;

                                            dbContext.ProdAuthors.Add(a);
                                            dbContext.SaveChanges();
                                        }
                                    }
                                    
                                }
                            }
                            else
                                SetCustomValidator("Isbn not found on the web site.");
                        }
                    }
                    else
                        SetCustomValidator("Failed to connect to website or ISBN not found.");
                }
            }
            catch (Exception)
            {
                //message = ex.Message;
                SetCustomValidator(message);

            }
            if (productId > 0 && !string.IsNullOrEmpty(imageUrl))
                DownloadJacketGoogle(imageUrl, productId);
            return productId;
        }

        private bool UpdateProductGoogle(HomeReadingEntities dbContext, int libraryId, int productId, string title, string annotation)
        {
            bool success = false;

            var query = (from p in dbContext.Products
                         where p.Product_Id == productId
                         select p).FirstOrDefault();

            if (query != null)
            {
                query.Title = title;
                query.Authorised = true;
                query.Inactive = false;
                query.UserId = userId;
                query.Entered = DateTime.Now;

                if (!string.IsNullOrEmpty(annotation))
                {
                    var query2 = (from pa in dbContext.Annotations
                                  where pa.Product_Id == productId && pa.AnnotType == "1"
                                  select pa).SingleOrDefault();
                    if (query2 != null && query2.Product_Id == productId)
                        query2.Description = annotation;

                    else
                    {
                        Annotation pa = new Annotation();
                        pa.Product_Id = productId;
                        pa.Description = annotation;
                        pa.AnnotType = "1";
                        pa.Updated = DateTime.Now;
                        dbContext.Annotations.Add(pa);
                    }
                    //if (doLabels)
                    //{
                    //    PrintLabel(productId, libraryId, dbContext);

                    //}
                }
                try
                {
                    dbContext.SaveChanges();
                    success = true;
                    SetCustomValidator("Title updated.");
                    //ShowProductForm(productId);
                }
                catch (Exception)
                {
                    SetCustomValidator("Failed to save changes to file.");
                }

                //DownloadJacket(isbn, productId, true);
            }
            return success;
        }

        private int AddProductGoogle(HomeReadingEntities dbContext, int libraryId, string isbn, string title, string annotation, bool doLabels)
        {
            int productId = 0;

            try
            {
                Product p = new Product();
                p.Isbn = isbn;
                p.Entered = DateTime.Now;
                p.Title = title;
                p.Library_Id = libraryId;
                p.Authorised = true;
                p.Inactive = false;
                p.UserId = userId; 
                if (levelsId > 0)
                    p.Levels_Id = Convert.ToInt32(levelsId);

                dbContext.Products.Add(p);
                dbContext.SaveChanges();
                productId = p.Product_Id;

                int stock = 0;
                if (addStock)
                    stock = 1;

                if (stock > 0)
                {
                    ProdStock pq = new ProdStock();
                    pq.Product_Id = productId;
                    pq.Onhand = stock;
                    pq.Library_Id = libraryId;
                    dbContext.ProdStocks.Add(pq);
                }

                if (!string.IsNullOrEmpty(annotation))
                {
                    Annotation pa = new Annotation();
                    pa.Product_Id = productId;
                    pa.Description = annotation;
                    pa.AnnotType = "1";
                    pa.Updated = DateTime.Now;
                    dbContext.Annotations.Add(pa);
                }
                if (doLabels)
                    PrintLabel(productId, libraryId, dbContext);

                dbContext.SaveChanges();
            }
            catch (Exception)
            {
            }
            return productId;
        }

        private void DeleteAuthors(int productId, HomeReadingEntities dbContext)
        {
            var query = from a in dbContext.ProdAuthors
                        where a.Product_Id == productId
                        select a;
            foreach (var ProdAuthor in query)
            {
                dbContext.ProdAuthors.Remove(ProdAuthor);
            }
        }

        private bool DownloadJacketGoogle(string imageUrl, int productId)
        {

            int imageId = 0;
            //string virtualFolder = "~/";
            //string physicalFolder = Server.MapPath(virtualFolder);
            string fileName = Guid.NewGuid().ToString() + ".jpg";

            try
            {

                HttpWebRequest req = WebRequest.Create(imageUrl) as HttpWebRequest;
                req.ReadWriteTimeout = 200000;
                req.KeepAlive = false;
                req.Method = "GET";
                HttpWebResponse response = req.GetResponse() as HttpWebResponse;

                //    System.Threading.Thread.Sleep(1);

                if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Moved ||
                      response.StatusCode == HttpStatusCode.Redirect) && response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    using (Stream inputStream = response.GetResponseStream())
                    using (Stream outputStream = File.OpenWrite(System.IO.Path.Combine(physicalFolder, fileName)))
                    {
                        int bytesRead;
                        do
                        {
                            byte[] buffer = new byte[4096];
                            bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                            outputStream.Write(buffer, 0, bytesRead);
                        } while (bytesRead != 0);
                    }
                    var jacket = System.IO.File.ReadAllBytes(physicalFolder + fileName);

                    HomeReadingEntities dbContext = new HomeReadingEntities();

                    var query2 = (from pi in dbContext.ProdImages
                                  where pi.Product_Id == productId && pi.ImageSize == "S"
                                  select pi).SingleOrDefault();

                    if (query2 != null)
                    {
                        query2.Jacket = jacket;
                        dbContext.SaveChanges();
                        imageId = query2.Image_Id;
                    }
                    else
                    {
                        ProdImage pi = new ProdImage();
                        pi.Product_Id = productId;
                        pi.ImageSize = "S";
                        pi.Jacket = jacket;
                        dbContext.ProdImages.Add(pi);
                        dbContext.SaveChanges();
                        imageId = pi.Image_Id;
                    }
                }

                response.Close();

            }
            catch (Exception)
            {
                //Console.WriteLine(ex.Message.ToString());
            }
            finally
            {

                System.IO.File.Delete(physicalFolder + fileName);
            }
            return imageId > 0;
        }

        private void SetCustomValidator(string message)
        {
            errorMessage = message;
        }

        private void PrintLabel(int productId, int libraryId, HomeReadingEntities dbContext)
        {
            PrintLabel pl = new PrintLabel();
            pl.Product_Id = productId;
            pl.Library_Id = libraryId;
            pl.Qty = 1;
            pl.UserId = userId;
            pl.Entered = DateTime.Now;
            dbContext.PrintLabels.Add(pl);
        }
    }
}