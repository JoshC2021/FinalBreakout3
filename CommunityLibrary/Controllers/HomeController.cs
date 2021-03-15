using CommunityLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


namespace CommunityLibrary.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LibraryDAL _libraryDAL = new LibraryDAL();
        private readonly GoogleDAL _googleDAL = new GoogleDAL(); // when creating user, need this for determining lat and lng
        private readonly LibraryDbContext _libraryDB;

        public HomeController(LibraryDbContext libraryContext)
        {
            _libraryDB = libraryContext;
        }
        public IActionResult Index()
        {
            /*            List<Doc> results = new List<Doc>();

                        results = _libraryDAL.GetSearchTitles("fsdfsdfsdfsdfsdf");*/

            BookInfo bookInfo = new BookInfo();

            bookInfo = _libraryDAL.GetBookInfo("/isbn/9780140328721");

            return View(bookInfo);
        }

        public IActionResult UserMap()
        {
            // Grab users lat and lng
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            TempData["lat"] = currentUser.lattitude;
            TempData["lng"] = currentUser.longitude;


            // grab  everyone else's lat and lng from table
            // create a marker list of lat and lngs close to user
            List<User> notLogged = _libraryDB.Users.Where(x => x.UserId != user).ToList();
            List<string> marker = new List<string>();
            int max = 0; // only show so many libraries

            const double radConv = Math.PI / 180;
            const int R = 6371; // radius of earth in km
            const int d = 20; // only show libraries within 20km
            double userLat = currentUser.lattitude * radConv;
            double otherLat;
            double deltaLng;
            foreach (User other in notLogged)
            {
                otherLat = other.lattitude * radConv;
                deltaLng = currentUser.longitude - other.longitude;

                if ((Math.Acos(Math.Sin(userLat) * Math.Sin(otherLat) + Math.Cos(userLat) * Math.Cos(otherLat) * Math.Cos(deltaLng)) * R) < d)
                {
                    marker.Add($"{other.lattitude}, {other.longitude}");
                }

                if (max >= 15)
                {
                    break;
                }
                max++;
            }

            return View(marker);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
