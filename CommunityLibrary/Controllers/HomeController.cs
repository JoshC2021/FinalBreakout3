using CommunityLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CommunityLibrary.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LibraryDAL _libraryDAL = new LibraryDAL();

        public IActionResult Index()
        {
            /*            List<Doc> results = new List<Doc>();

                        results = _libraryDAL.GetSearchTitles("fsdfsdfsdfsdfsdf");*/

            BookInfo bookInfo = new BookInfo();

            bookInfo = _libraryDAL.GetBookInfo("/isbn/9780140328721");

            return View(bookInfo);
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
