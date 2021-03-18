using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.Models
{
    public class Review
    {
        public BookInfo ApiBook { get; set; }
        public BookReview review { get; set; }
    }
}
