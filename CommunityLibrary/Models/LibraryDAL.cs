using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace CommunityLibrary.Models
{
    public class LibraryDAL
    {
        private string GetSearchData(string title)
        {
            string url = @$"https://openlibrary.org/search.json?q={title}";
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string JSON = reader.ReadToEnd();

            return JSON;
        }

        private string GetSearchDataByTitle(string title)
        {
            string url = @$"https://openlibrary.org/search.json?title={title}";
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string JSON = reader.ReadToEnd();

            return JSON;
        }

        private string GetKeyData(string key)
        {
            string url = @$"https://openlibrary.org{key}.json";
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string JSON = reader.ReadToEnd();

            return JSON;
        }

        public BookInfo GetBookInfo(string key)
        {
            string data = GetKeyData(key);
            BookInfo bookInfo;
            SpecialBook bookSpecial;
            

            try
            {
               bookInfo = JsonConvert.DeserializeObject<BookInfo>(data);
               return bookInfo;
            }
            catch
            {
             
                    bookSpecial = JsonConvert.DeserializeObject<SpecialBook>(data);
                    return bookSpecial;
             
            }
            

        }
        public Author GetAuthorInfo(string key)
        {
            string data = GetKeyData(key);
            Author authorInfo = JsonConvert.DeserializeObject<Author>(data);
            return authorInfo;
        }
        public List<Doc> GetSearchTitles(string title)
        {
            string data = GetSearchDataByTitle(title);
            SearchTitleResults searchTitleResults = JsonConvert.DeserializeObject<SearchTitleResults>(data);

            if (searchTitleResults.docs == null)
            {
                return new List<Doc>();
            }
            else
            {
                return searchTitleResults.docs.ToList();
            }
        }
    }

}

