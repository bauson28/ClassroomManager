using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeReadingManager.ViewModels
{
    public class HomeViewModel
    {
        public string UserName { get; set; }
        public string School { get; set; }
        public bool LoggedIn { get; set; }
    }
}