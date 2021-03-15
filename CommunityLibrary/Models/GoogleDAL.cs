using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CommunityLibrary.Models
{
    public class GoogleDAL
    {
        private string GetData(string address)
        {
            string url = @$"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key={Secret.GoogleAPIKey}";
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string JSON = reader.ReadToEnd();

            return JSON;
        }

        public List<Result> GetResults(string address)
        {
            string data = GetData(address);

            GeoCode addressInfo = JsonConvert.DeserializeObject<GeoCode>(data);

            if (addressInfo.results == null)
            {
                return new List<Result>();
            }
            else
            {
                return addressInfo.results.ToList();
            }

        }

    }
}
