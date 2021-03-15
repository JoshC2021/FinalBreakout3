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
            TempData["lat"] = currentUser.Latitude;
            TempData["lng"] = currentUser.Longitude;


            // grab  everyone else's lat and lng from table
            // create a marker list of lat and lngs close to user
            List<User> notLogged = _libraryDB.Users.Where(x => x.UserId != user).ToList();
            List<User> withinDistance = new List<User>();
            int max = 0; // only show so many libraries

            const double radConv = Math.PI / 180;
            const int R = 6371; // radius of earth in km
            const int d = 20; // only show libraries within 20km
            double userLat = Double.Parse(currentUser.Latitude) * radConv;
            double otherLat;
            double deltaLng;
            foreach (User other in notLogged)
            {
                otherLat = Double.Parse(other.Latitude) * radConv;
                deltaLng = Double.Parse(currentUser.Longitude) - Double.Parse(other.Longitude);

                if ((Math.Acos(Math.Sin(userLat) * Math.Sin(otherLat) + Math.Cos(userLat) * Math.Cos(otherLat) * Math.Cos(deltaLng)) * R) < d)
                {
                    withinDistance.Add(other);
                }

                if (max >= 15)
                {
                    break;
                }
                max++;
            }

            return View(withinDistance);
        }
        public IActionResult ViewApiInfoForSingleBook(/*string bookId*/)
        {
          //Switch this out to the parameter
            string bookId = "/works/OL45883W";
            BookInfo apiBook = _libraryDAL.GetBookInfo(bookId);
            List<Author> authors = new List<Author>();
            foreach (Author author in apiBook.authors)
            {
                string authorId = author.author.key;
                Author apiAuthor = _libraryDAL.GetAuthorInfo(authorId);

                authors.Add(apiAuthor);

            }
            apiBook.authors = authors;
            return View(apiBook);
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
