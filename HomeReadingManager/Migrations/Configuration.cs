namespace HomeReadingManager.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using HomeReadingManager.Models;
    using System.Web.Security;
    using Microsoft.AspNet.Identity.Owin;
   

    internal sealed class Configuration : DbMigrationsConfiguration<HomeReadingManager.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(HomeReadingManager.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
           

            //AddUserAndRole(context);
        }

        //bool AddUserAndRole(HomeReadingManager.Models.ApplicationDbContext context)
        //{
        //    IdentityResult ir;
        //    var rm = new RoleManager<IdentityRole>
        //        (new RoleStore<IdentityRole>(context));
        //    ir = rm.Create(new IdentityRole("Parent helper"));
        //    ir = rm.Create(new IdentityRole("Teacher"));
        //    ir = rm.Create(new IdentityRole("Supervisor"));
        //    ir = rm.Create(new IdentityRole("Administrator"));
        //    ir = rm.Create(new IdentityRole("Manager"));
        //    var um = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
        //    var user = new ApplicationUser()
        //    {
        //        UserName = "bernard.foley@gmail.com",
        //        Email = "bernard.foley@gmail.com",
        //        LibraryId = 1017,
        //        TeacherId = 3245
        //    };
        //    ir = um.Create(user, "xerxes");
        //    if (ir.Succeeded == false)
        //        return ir.Succeeded;

        //    ir = um.AddToRole(user.Id, "Supervisor");
        //    ir = um.AddToRole(user.Id, "Administrator");
        //    ir = um.AddToRole(user.Id, "Manager");
        //    return ir.Succeeded;
           
        //}
    }
}
