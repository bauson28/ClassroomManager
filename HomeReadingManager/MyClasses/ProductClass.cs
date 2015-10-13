using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeReadingManager.MyClasses
{
    public class ProductClass
    {
        public static string CheckCheckDigit(string isbn)
        {
            if (isbn.Length == 10)
            {
                isbn = isbn.ToUpper();//bf catch isbns ending in X
                if (isbn == Isbncheck(isbn))
                    isbn = GetBarcode(isbn);
                else
                    isbn = string.Empty;
            }
            else if (isbn.Length == 13)// && (isbn.StartsWith("978") || isbn.StartsWith("979"))) //bf 201113 just need valid EAN
            {
                string calculated = string.Empty;
                calculated = GetBarcode(isbn);
                if (isbn != calculated)
                    isbn = string.Empty;
            }
            else
                isbn = string.Empty;
            return isbn;
        }

        public static string GetLineBreaks(object Blurb)
        {
            return (Blurb.ToString().Replace(Environment.NewLine, "<br/>"));
        }

        public static string GetBarcode(string isbn)
        {
            string cShort;
            int temp = 0, total = 0;
            if (isbn.Trim().Length == 10)
                cShort = "978" + isbn.Substring(0, 9);
            else
                cShort = isbn.Substring(0, 12);

            for (int i = 2; i < 13; i += 2)
            {
                total += Convert.ToInt32(cShort.Substring(i - 1, 1)) * 3;
                total += Convert.ToInt32(cShort.Substring(i - 2, 1));
            }

            temp = 10 - (total % 10);
            if (temp == 10)
                temp = 0;

            return (cShort + temp.ToString());
        }

        public static string Isbncheck(string isbn)
        {
            int nTotal = 0;
            int nMod = 0;
            string check = string.Empty;

            if (isbn.StartsWith("978"))
                isbn = isbn.Substring(4, 10);
            else if (isbn.Trim().Length != 10)
                return isbn;

            isbn = isbn.Substring(0, 9);
            for (int i = 0; i < 9; i++)
            {
                nTotal += Convert.ToInt32(isbn.Substring(i, 1)) * (11 - (i + 1));
            }
            nMod = nTotal % 11;

            switch (nMod)
            {
                case 10:
                    check = "1";
                    break;
                case 0:
                    check = "0";
                    break;
                case 1:
                    check = "X";
                    break;
                default:
                    check = (11 - nMod).ToString();
                    break;
            }
            return isbn + check;
        }
    }
}