using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HomeReadingManager.Models;
using HomeReadingManager.ViewModels;

namespace HomeReadingManager.Controllers
{
    public class ProdAuthorsController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();

        private SelectList GetAuthorRoleList(int id)
        {
            var query = from r in db.OnixAuthorRoles
                        orderby r.RoleCode
                        select new
                        {
                            r.Role_Id,
                            r.Role
                        };

            return new SelectList(query, "Role_Id", "Role", id);
        }
        // GET: ProdAuthors
        //[ChildActionOnly]
        public ActionResult Index(int id, string title)
        {
            AuthorsModel model = new AuthorsModel();
           
            model.ProductId = id;
            model.Title = title;
            IQueryable<Author> authors = from pa in db.ProdAuthors
                                         where pa.Product_Id == id
                                         orderby pa.Role_Id
                                         select new Author()
                                         {
                                             Id = pa.Author_Id,
                                             AuthorName = pa.Author,
                                             RoleId = pa.Role_Id,
                                             Role = pa.OnixAuthorRole.Role
                                         };
            model.Authors = authors;

            return PartialView("_Index", model);
        }
       

        // GET: ProdAuthors/Create
        public ActionResult Create(int productId, string title)
        {
            AuthorEditModel model = new AuthorEditModel();
            model.Id = 0;
            model.Role_Id = 1;
            model.ProductId = productId;
            model.Title = title;
            
            IQueryable<AuthorRole> roles = from r in db.OnixAuthorRoles
                                           orderby r.RoleCode
                                           select new AuthorRole
                                           {
                                               Role_Id = r.Role_Id,
                                               Role = r.Role
                                           };
            model.Roles = roles;

            return PartialView("_Create", model);
        }

        // POST: ProdAuthors/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AuthorEditModel model)
        {
            if (ModelState.IsValid)
            {
                ProdAuthor pa = new ProdAuthor();
                pa.Author = model.Author;
                pa.Role_Id = model.Role_Id;
                pa.Product_Id = model.ProductId;
                db.ProdAuthors.Add(pa);
                try
                {
                    db.SaveChanges();
                    string url = Url.Action("Index", "ProdAuthors", new { id = model.ProductId, title = model.Title });
                    return Json(new { success = true, url = url });
                }
                catch (Exception)
                {

                }
            }
            return PartialView("_Create", model);
        }

        // GET: ProdAuthors/Edit/5
       public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProdAuthor pa = db.ProdAuthors.Find(id);
           
            if (pa == null)
            {
                return HttpNotFound();
            }

            AuthorEditModel model = GetAuthorEditModel(pa);

            return PartialView("_Edit", model);
        }

        // POST: ProdAuthors/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost, ActionName("Edit")]
       
        ////baf xxxx 
         [HttpPost]
         [ValidateAntiForgeryToken] 
        public ActionResult Edit(AuthorEditModel model)
        {
            if (model == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProdAuthor paToUpdate = db.ProdAuthors.Find(model.Id);
            if (ModelState.IsValid)
            {
                
                if (TryUpdateModel(paToUpdate, "", new string[] { "Author",  "Role_Id"}))
          
                try
                {
                    db.Entry(paToUpdate).State = EntityState.Modified;

                    db.SaveChanges();
                    //return RedirectToAction("Index", new{id = paToUpdate.Product_Id, title = paToUpdate.Product.Title});
                    string url = Url.Action("Index", "ProdAuthors", new { id = paToUpdate.Product_Id, title = paToUpdate.Product.Title });
                    return Json(new { success = true, url = url });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                 }
            }
            //AuthorEditModel model = GetAuthorEditModel(paToUpdate);

            return PartialView("_Edit", model);
        }
         
        private AuthorEditModel GetAuthorEditModel(ProdAuthor pa)
        {
            AuthorEditModel model = new AuthorEditModel();
            model.Id = pa.Author_Id;
            model.Author = pa.Author;
            model.Role_Id = pa.Role_Id;
            model.Role = pa.OnixAuthorRole.Role;
            model.ProductId = pa.Product_Id;
            model.Title = pa.Product.Title;

            IQueryable<AuthorRole> roles = from r in db.OnixAuthorRoles
                                           orderby r.RoleCode
                                           select new AuthorRole
                                           {
                                               Role_Id = r.Role_Id,
                                               Role = r.Role
                                           };
            model.Roles = roles;

            return model;
        }
        // GET: ProdAuthors/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProdAuthor prodAuthor = db.ProdAuthors.Find(id);
            if (prodAuthor == null)
            {
                return HttpNotFound();
            }
            AuthorEditModel model = GetAuthorEditModel(prodAuthor);

            return PartialView("_Delete", model);
        }

        // POST: ProdAuthors/Delete/5
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProdAuthor prodAuthor = db.ProdAuthors.Find(id);
            db.ProdAuthors.Remove(prodAuthor);
            db.SaveChanges();
            string url = Url.Action("Index", "ProdAuthors", new { id = prodAuthor.Product_Id, title = prodAuthor.Product.Title });
            return Json(new { success = true, url = url });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
