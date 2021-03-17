using System;
using System.Collections.Generic;

#nullable disable

namespace CommunityLibrary.Models
{
    public partial class User
    {
        public User()
        {
            BookBookOwnerNavigations = new HashSet<Book>();
            BookCurrentHolderNavigations = new HashSet<Book>();
            BookReviews = new HashSet<BookReview>();
            LoanBookLoanerNavigations = new HashSet<Loan>();
            LoanBookOwnerNavigations = new HashSet<Loan>();
        }

        public int Id { get; set; }
        public string UserLocation { get; set; }
        public int? CumulatvieRating { get; set; }
        public string UserId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string ProfileImage { get; set; }

        public virtual AspNetUser UserNavigation { get; set; }
        public virtual ICollection<Book> BookBookOwnerNavigations { get; set; }
        public virtual ICollection<Book> BookCurrentHolderNavigations { get; set; }
        public virtual ICollection<BookReview> BookReviews { get; set; }
        public virtual ICollection<Loan> LoanBookLoanerNavigations { get; set; }
        public virtual ICollection<Loan> LoanBookOwnerNavigations { get; set; }
    }
}
