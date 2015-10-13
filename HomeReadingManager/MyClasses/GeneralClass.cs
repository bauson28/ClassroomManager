using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.WebPages.Html;

namespace HomeReadingManager.MyClasses
{
    public class GeneralClass
    {
        public IEnumerable<SelectListItem> AddDefaultOption(IEnumerable<SelectListItem> list, string dataTextField, string selectedValue, bool addLine)
        {
            var items = new List<SelectListItem>();
            if (addLine)
                items.Add(new SelectListItem() { Text = dataTextField, Value = selectedValue });
            items.AddRange(list);
            return items;
        }
    }
}