using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;


namespace HomeReadingManager.MyClasses
{
    public class ValidationClasses
    {
        public class ValidateFileAttribute : ValidationAttribute
        {

            public override bool IsValid(object value)
            {

                int maxContent = 1024 * 1024; //1 MB
                string[] sAllowedExt = new string[] { ".jpg", ".gif", ".png" };
                var file = value as HttpPostedFileBase;

                if (file == null)
                    return false;

                else if (!sAllowedExt.Contains(file.FileName.Substring(file.FileName.LastIndexOf('.'))))
                {
                    ErrorMessage = "Please upload image of type: " + string.Join(", ", sAllowedExt);
                    return false;
                }

                else if (file.ContentLength > maxContent)
                {
                    ErrorMessage = "Your jacket image is too large; maximum allowed size is : " + (maxContent / 1024).ToString() + "MB";
                    return false;
                }

                else
                    return true;
            }
        }
    }
}