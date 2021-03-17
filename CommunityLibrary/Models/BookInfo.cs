using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.Models
{

    public class SpecialBook : BookInfo
    {
        public Description description { get; set; }
    }

    public class CrazyBook : BookInfo
    {
        SpecialAuthor[] authors { get; set; }
    }

    public class SpecialAuthor : Author
    {
        public string type { get; set; }
    }

    public class BookInfo
    {
        public string description { get; set; }
        public Link[] links { get; set; }
        public string title { get; set; }
        public int[] covers { get; set; }
        public string[] subject_places { get; set; }
        public string[] subjects { get; set; }
        public string first_publish_date { get; set; }
        public string[] subject_people { get; set; }
        public string key { get; set; }
        public List<Author> authors { get; set; }
        public Excerpt[] excerpts { get; set; }
        public string[] subject_times { get; set; }
        public Type type { get; set; }
        public int latest_revision { get; set; }
        public int revision { get; set; }
        public Created created { get; set; }
        public Last_Modified last_modified { get; set; }
    }

    public class Description
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class Type
    {
        public string key { get; set; }
    }

    public class Created
    {
        public string type { get; set; }
        public DateTime value { get; set; }
    }

    public class Last_Modified
    {
        public string type { get; set; }
        public DateTime value { get; set; }
    }

    public class Link
    {
        public string title { get; set; }
        public string url { get; set; }
        public Type1 type { get; set; }
    }

    public class Author
    {
        public string name { get; set; }

        public int[] photos { get; set; }
        public Author1 author { get; set; }
        public string key { get; set; }
    

   

    }
    public class Bio
    {
        public string type { get; set; }
        public string value { get; set; }
    }
    public class Remote_Ids
    {
        public string viaf { get; set; }
        public string wikidata { get; set; }
        public string isni { get; set; }
    }
    public class Type1
    {
        public string key { get; set; }
    }


    public class Author1
    {
        public string key { get; set; }
    }

    public class Type2
    {
        public string key { get; set; }
    }

    public class Excerpt
    {
        public string pages { get; set; }
        public string excerpt { get; set; }
        public Author2 author { get; set; }
        public string comment { get; set; }
    }

    public class Author2
    {
        public string key { get; set; }
    }

}
