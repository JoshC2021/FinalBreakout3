using System;
using System.Collections.Generic;

#nullable disable

namespace CommunityLibrary.Models
{
    public partial class Loan
    {
        public int Id { get; set; }
        public bool LoanStatus { get; set; }
        public DateTime DueDate { get; set; }
        public int? BookId { get; set; }
        public int? RecipientRating { get; set; }
        public int? OwnerRating { get; set; }
        public string OwnerNote { get; set; }
        public string LoanerNote { get; set; }
        public int? BookLoaner { get; set; }
        public int? BookOwner { get; set; }

        public virtual Book Book { get; set; }
        public virtual User BookLoanerNavigation { get; set; }
        public virtual User BookOwnerNavigation { get; set; }
        public bool IsOwner(int id)
        {
            return id == BookOwner;
        }

        public bool IsDueDateSet()
        {
            return this.DueDate == DateTime.MinValue;
        }

    }
}
