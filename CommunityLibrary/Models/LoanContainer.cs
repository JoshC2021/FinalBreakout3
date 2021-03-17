using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.Models
{
    public class LoanContainer
    {
        public string BookTitle { get; set; }

        public Loan LoanInfo { get; set; }

        public string ProfileImage { get; set; }

        public string LoanerName { get; set; }

    }
}
