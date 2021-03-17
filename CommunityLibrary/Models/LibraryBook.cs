using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.Models
{
    public class LibraryBook
    {
        public BookInfo ApiBook { get; set; }
        public Book DbBook { get; set; }
        public AspNetUser BookOwner { get; set; }
        public AspNetUser BookHolder { get; set; }
    }
}
