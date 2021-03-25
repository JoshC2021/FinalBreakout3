using CommunityLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.ViewModels
{
    public class ApprovalViewModel
    {
        public User BookOwner { get; set; }
        public User BookBorrower { get; set; }
        public User CurrentUser { get; set; }
        public Loan Loan { get; set; }
        public Book Book { get; set; }
        public string BookTitle { get; set; }
        public int BookBorrowerRating { get; set; }
        public CurrentState CurrentState { get; set; }
    }
    public enum CurrentState { Pending, CheckedOut, Returned }
}
