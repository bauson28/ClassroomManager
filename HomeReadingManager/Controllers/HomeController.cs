using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using HomeReadingManager.ViewModels;
using HomeReadingManager.Models;
using System.Threading.Tasks;
using Microsoft.Owin.Security;

namespace HomeReadingManager.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private HomeReadingEntities db = new HomeReadingEntities();
        private ApplicationUserManager _userManager;

        public HomeController()
        {

        }

        public HomeController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ActionResult Index()
        {
           HomeViewModel model = new HomeViewModel();
          
           var user = UserManager.FindById(User.Identity.GetUserId());
           
           if (user != null && user.TeacherId > 0)
           { 
                model.LoggedIn = true;
                model.LoggedIn = UserManager.FindById(User.Identity.GetUserId()) != null;
                var query = (from t in db.Teachers
                             where t.Teacher_Id == user.TeacherId
                             select t).FirstOrDefault();
                if (query != null)
                {
                    model.UserName = query.FirstName.Trim() + " " + query.LastName.Trim();
                    model.School = query.Library.Licensee.Trim();
                }
           }
           else
                model.LoggedIn = false;
           
            return View(model);
        }

        [HttpGet]
        public JsonResult GuestLogin()
        {
            var user = UserManager.FindByName("bauson@outlook.com");
            if (user != null)
            {
                var identity = UserManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                var authenticationManager = HttpContext.GetOwinContext().Authentication;
                authenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);
                return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Success = false }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        //[Authorize]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
       
    }
}