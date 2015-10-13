using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HomeReadingManager.ViewModels
{
    public class TitlesModel
    {
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public int Page { get; set; }
        public string SearchString { get; set; }
        public bool ShowDelete { get; set; }
        public int LibraryId { get; set; }
        public virtual IPagedList<ProductList> TitlesList { get; set; }
    }

    public class ProductList
    {
        [Display(Name = "Id")]
        public int Product_Id { get; set; }
        [Required]
        [StringLength(150, ErrorMessage = "Title cannot be longer than 150 characters.")]
        public string Title { get; set; }
        [Required]
        //[StringLength(14, ErrorMessage = "ISBN cannot be longer than 14 characters.")]
        [Remote("IsValidIsbn", "Products", HttpMethod = "POST", ErrorMessage = "Not a valid Isbn.")]
        //[Remote("IsIsbnUnique", "Students", HttpMethod = "POST", ErrorMessage = "The title is already in the database.")]
        public string Isbn { get; set; }
        //[Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }
        public int Annotation_Id { get; set; }
        public bool Inactive { get; set; }
        public bool Authorised { get; set; }
        [Display(Name = "Author")]
        public string MainAuthor { get; set; }
        [Display(Name = "Author role")]
        public int Role_Id { get; set; }
        [Display(Name = "Reading level")]
        public int Levels_Id { get; set; }
        public string ReadLevel { get; set; }
        public string Entered { get; set; }
        public int Image_Id { get; set; }
        public byte[] Jacket { get; set; }
        [Display(Name = "Stock")]
        public int Onhand { get; set; }
        public int OnLoan { get; set;}
        public int Available { get; set; }
        [Display(Name = "Barcode labels")]
        public int Labels { get; set; }
        public string SortOrder { get; set; }
        public bool Ascending { get; set; }
        public string SearchString { get; set; }
        public int Page { get; set; }
        public int LibraryId { get; set; }
        public virtual ICollection<ProdLoans> ProdLoans { get; set; }
        [Display(Name = "Borrowed")]
        public List<YearLoans> YearLoans { get; set; }
        [Display(Name = "Author")]
        public virtual ICollection<Authors> Authors { get; set; }
        [Display(Name = "Current loans")]
        public virtual ICollection<ProdLoans> CurrentLoans { get; set; }
        public virtual IQueryable<LevelItem> Levels { get; set; }
        public virtual IQueryable<AuthorRole> Roles { get; set; }
    }

    public class AuthorsModel
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public virtual IQueryable<Author> Authors { get; set; }
    }

    public class Author
    {
        public int Id { get; set; }
        public string AuthorName { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }
    }

    public class AuthorEditModel
    {
        public int Id { get; set; }
        public string Author { get; set; }
        [Display(Name = "Role")]
        public int Role_Id { get; set; }
        public string Role { get; set; }
        public int ProductId { get; set; }
        public string Title { get; set; }
        public virtual IQueryable<AuthorRole> Roles { get; set; }
    }

    public class Authors
    {
        public string Author { get; set; }
        public int Role_Id { get; set; }
        public string Role { get; set; }
        public Authors(string author, int id, string role)
        {
            Author = author;
            Role_Id = id;
            Role = role;
        }
    }

    public class AuthorRole
    {
        public int Role_Id { get; set; }
        public string Role { get; set; }

    }

    public class LevelItem
    {
        public int Id { get; set; }
        public string ReadLevel { get; set; }

    }

    public class ProdLoans
    {
        public string FullName { get; set; }
        public String Borrowed { get; set; }
        public String Returned { get; set; }
        public ProdLoans(string fullName, String borrowed, String returned)
	    {
	       FullName = fullName;
           Borrowed = borrowed;
           Returned = returned;
	    }
    }

    public class YearLoans
    {
        public int Year { get; set; }
        public int Loans { get; set; }
        public YearLoans(int year, int loans)
        {
            Year = year;
            Loans = loans;
        }
    }
}