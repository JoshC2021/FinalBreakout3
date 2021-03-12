using System;
using System.Collections.Generic;

#nullable disable

namespace CommunityLibrary.Models
{
    public partial class Book
    {
        public Book()
        {
            Loans = new HashSet<Loan>();
        }

        public int Id { get; set; }
        public bool? AvailibilityStatus { get; set; }
        public int? LoanPeriod { get; set; }
        public int? TitleIdApi { get; set; }
        public int? CurrentHolder { get; set; }
        public int? BookOwner { get; set; }

        public virtual User BookOwnerNavigation { get; set; }
        public virtual User CurrentHolderNavigation { get; set; }
        public virtual ICollection<Loan> Loans { get; set; }
    }
}
