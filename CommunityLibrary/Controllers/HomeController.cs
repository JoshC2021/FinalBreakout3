using CommunityLibrary.Models;
using Microsoft.AspNetCore.Authorization;
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
            BookInfo bookInfo = new BookInfo();

            bookInfo = _libraryDAL.GetBookInfo("/isbn/9780140328721");

            return View(bookInfo);
        }

        [Authorize]
        public IActionResult Profile()
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            return View(currentUser);
        }

        public IActionResult UpdateProfile(int Id)
        {
            User currentUser = _libraryDB.Users.First(x => x.Id == Id);
            return View(currentUser);
        }

        [HttpPost]
        public IActionResult UpdateProfile(User current)
        {
            if (ModelState.IsValid)
            {
                current.UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                List<Result> latLng = _googleDAL.GetResults(current.UserLocation);
                // check to see if user address exists
                if(latLng.Count != 0)
                {
                    // set top match to user's location
                    current.Latitude = latLng[0].geometry.location.lat.ToString();
                    current.Longitude = latLng[0].geometry.location.lng.ToString();
                }
                _libraryDB.Users.Update(current);
                _libraryDB.SaveChanges();
            }
            return RedirectToAction("Profile");
        }

        public IActionResult Transactions(int Id)
        {
            User currentUser = _libraryDB.Users.First(x => x.Id == Id);
            TempData["CurrentUser"] = currentUser.Id;
            List<Loan> userLoans = _libraryDB.Loans.Where(x => x.BookLoaner == currentUser.Id || x.BookOwner == currentUser.Id).ToList();
            return View(userLoans);
        }

        public IActionResult Approval(int loanId)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            TempData["CurrentUser"] = currentUser.Id;

            Loan currentLoan = _libraryDB.Loans.First(x=> x.Id == loanId);
            Approval loanDetails = new Approval();
            loanDetails.LoanInfo = currentLoan;

            User renterInfo = _libraryDB.Users.First(x => x.Id == currentLoan.BookLoaner);
            loanDetails.ProfileImage = renterInfo.ProfileImage;

            Book loanedBook = _libraryDB.Books.First(x => x.Id == currentLoan.BookId);
            loanDetails.BookTitle = _libraryDAL.GetBookInfo(loanedBook.TitleIdApi).title;

            return View(loanDetails);
        }

        [HttpPost]
        public IActionResult Approval(Loan approvalUpdate)
        {

            Loan oldDetails = _libraryDB.Loans.First(x => x.Id == approvalUpdate.Id);
            oldDetails.LoanStatus = approvalUpdate.LoanStatus;
            if(oldDetails.LoanStatus)
            {
                // set DueDate
                Book loanedBook = _libraryDB.Books.First(x => x.Id == oldDetails.BookId);
                DateTime due = DateTime.Today;
                due = due.AddDays((double)loanedBook.LoanPeriod);
                oldDetails.DueDate = due;

                _libraryDB.Loans.Update(oldDetails);
            }
            else
            {
                _libraryDB.Loans.Update(oldDetails);
            }
            return RedirectToAction("Profile");
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
            string bookId = "/works/OL453936W";
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
        [Authorize]
        public IActionResult AddBookToLibrary(string bookId)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            List<Book> personalLibrary = _libraryDB.Books.Where(x => x.BookOwner == currentUser.Id).ToList();
            if (personalLibrary.Where(x => x.TitleIdApi == bookId).Count() > 0)
            {
                //Book already exists in their personal library--should libraries be allowed to have more than 1 copy of the same book?
                return View();
            }
            else
            {
                Book newLibraryBook = new Book();
                newLibraryBook.AvailibilityStatus = true;
                newLibraryBook.LoanPeriod = 14;
                newLibraryBook.CurrentHolder = currentUser.Id;
                newLibraryBook.BookOwner = currentUser.Id;
                newLibraryBook.TitleIdApi = bookId;
                _libraryDB.Books.Add(newLibraryBook);
                _libraryDB.SaveChanges();
                return RedirectToAction("Index");
            }

        }
        
        public IActionResult SearchByTitle()
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);

            List<Doc> results = new List<Doc>();
            results = _libraryDAL.GetSearchTitles("Lord of the Rings");

            return View(results);

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
