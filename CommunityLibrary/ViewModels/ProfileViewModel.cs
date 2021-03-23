using CommunityLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.ViewModels
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public int OwnedBooksCount { get; set; }
        public int BorrowingCount { get; set; }
        public int LendingCount { get; set; }
        public int RequestCount { get; set; }
        public int ReviewCount { get; set; }
        public int CurrentRating { get; set; }

    }
}
