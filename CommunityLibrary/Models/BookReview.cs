using System;
using System.Collections.Generic;

#nullable disable

namespace CommunityLibrary.Models
{
    public partial class BookReview
    {
        public int Id { get; set; }
        public int? Rating { get; set; }
        public string Review { get; set; }
        public int? UserId { get; set; }
        public string TitleIdApi { get; set; }

        public virtual User User { get; set; }
    }
}
