using CommunityLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                //see if they're logged in---if this string fails-they're not logged-in
                //if they're not logged in--just send them to the view via the catch
                string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                try
                {
                    //If they're logged in- find that identity userId within the user table 
                    User currentUser = _libraryDB.Users.Where(x=>x.UserId==user).First();
                }
                catch (Exception)
                {
                    //if the identity userId isn't in the user table--make a new user
                    //doing this here because all users are routed to the index page after logging in
                    User newCurrentUser = new User();
                    newCurrentUser.UserId = user;
                    newCurrentUser.CumulatvieRating = 5;
                    _libraryDB.Users.Add(newCurrentUser);
                    _libraryDB.SaveChanges();
                    
                }
                
                return View();
            }
            catch (Exception)
            {
                
                return View();
            }

        }


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
        public IActionResult UpdateProfile(User updated)
        {

            if (ModelState.IsValid)
            {
                string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                User currentUser = _libraryDB.Users.First(x => x.UserId == user);
                List<Result> latLng = _googleDAL.GetResults(updated.UserLocation);
                // check to see if user address exists
                if (latLng.Count != 0)
                {
                    // set top match to user's location
                    currentUser.Latitude = latLng[0].geometry.location.lat.ToString();
                    currentUser.Longitude = latLng[0].geometry.location.lng.ToString();
                    currentUser.UserLocation = updated.UserLocation;
                }

                currentUser.ProfileImage = updated.ProfileImage;

                _libraryDB.Users.Update(currentUser);
                _libraryDB.SaveChanges();
            }
            return RedirectToAction("Profile");
        }

        public IActionResult Transactions(int Id)
        {
            User currentUser = _libraryDB.Users.First(x => x.Id == Id);
            TempData["CurrentUser"] = currentUser.Id;
            // grab all loans user is involved in, both sides
            List<Loan> userLoans = _libraryDB.Loans.Where(x => x.BookLoaner == currentUser.Id || x.BookOwner == currentUser.Id).ToList();
            return View(userLoans);
        }

        public IActionResult Approval(int loanId)
        {
            // Populate container with necessary info then send to view
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            TempData["CurrentUser"] = currentUser.Id;

            Loan currentLoan = _libraryDB.Loans.First(x => x.Id == loanId);
            LoanContainer loanDetails = new LoanContainer();
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
            Book loanedBook = _libraryDB.Books.First(x => x.Id == oldDetails.BookId);

            // update status and comments
            oldDetails.LoanStatus = approvalUpdate.LoanStatus;
            oldDetails.LoanerNote = approvalUpdate.LoanerNote;
            oldDetails.OwnerNote = approvalUpdate.OwnerNote;

            // newly approved rentals tasks
            if (oldDetails.LoanStatus && !oldDetails.IsDueDateSet())
            {
                // set DueDate
                DateTime due = DateTime.Today;
                due = due.AddDays((double)loanedBook.LoanPeriod);
                oldDetails.DueDate = due;

                // update book status
                loanedBook.AvailibilityStatus = false;
                loanedBook.CurrentHolder = oldDetails.BookLoaner;
            }

            // Remove denied rentals
            if (!oldDetails.LoanStatus && !oldDetails.IsDueDateSet())
            {
                _libraryDB.Loans.Remove(oldDetails);
                return RedirectToAction("Profile");
            }

            // Ended transactions, revert book to owner
            if (!oldDetails.LoanStatus)
            {
                loanedBook.AvailibilityStatus = true;
                loanedBook.CurrentHolder = oldDetails.BookOwner;
            }
            _libraryDB.Loans.Update(oldDetails);


            return RedirectToAction("Profile");
        }


        public IActionResult RequestLoan(int bookId)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);

            Loan newLoan = new Loan();

            // grab user details
            newLoan.BookLoaner = currentUser.Id;
            newLoan.RecipientRating = currentUser.CumulatvieRating;


            // grab book details
            Book renting = _libraryDB.Books.First(x => x.Id == bookId);
            newLoan.BookId = bookId;

            // grab owner details
            User bookOwner = _libraryDB.Users.First(x => x.Id == renting.BookOwner);
            newLoan.OwnerRating = bookOwner.CumulatvieRating;
            newLoan.BookOwner = bookOwner.Id;


            // go to transaction page to see added request
            return RedirectToAction("Transactions", currentUser.Id);
        }


        public IActionResult UserMap()
        {
            // Grab users lat and lng
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            TempData["lat"] = currentUser.Latitude;
            TempData["lng"] = currentUser.Longitude;

            // get local users
            List<User> notUser = _libraryDB.Users.Where(x => x.UserId != user).ToList();
            List<User> withinDistance = GetLocalUsers(currentUser,notUser);

            return View(withinDistance);
        }
        public IActionResult ViewApiInfoForSingleBook(string bookId)
        {
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
                return RedirectToAction("MyLibrary");
            }

        }

        public IActionResult MyLibrary()
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            List<Book> dbPersonalLibrary = _libraryDB.Books.Where(x => x.BookOwner == currentUser.Id).ToList();

            List<LibraryBook> libraryBooks = new List<LibraryBook>();

            foreach (Book book in dbPersonalLibrary)
            {
                BookInfo apiBook = _libraryDAL.GetBookInfo(book.TitleIdApi);
                List<Author> authors = new List<Author>();
                foreach (Author author in apiBook.authors)
                {
                    string authorId = author.author.key;
                    Author apiAuthor = _libraryDAL.GetAuthorInfo(authorId);

                    authors.Add(apiAuthor);
                }
                apiBook.authors = authors;
                LibraryBook libraryBook = new LibraryBook();

                User bookHolder = _libraryDB.Users.First(x => x.Id == book.CurrentHolder);
                AspNetUser identityBookHolder = _libraryDB.AspNetUsers.First(x => x.Id == bookHolder.UserId);

                User bookOwner = _libraryDB.Users.First(x => x.Id == book.BookOwner);
                AspNetUser identityBookOwner = _libraryDB.AspNetUsers.First(x => x.Id == bookOwner.UserId);
                libraryBook.ApiBook = apiBook;
                libraryBook.DbBook = book;
                libraryBook.BookOwner = identityBookOwner;
                libraryBook.BookHolder = identityBookHolder;
                libraryBooks.Add(libraryBook);
            }


            return View(libraryBooks);
        }

        public IActionResult RemoveFromLibrary(int bookId)
        {
            Book currentBook = _libraryDB.Books.Find(bookId);
            _libraryDB.Books.Remove(currentBook);
            _libraryDB.SaveChanges();
            return RedirectToAction("MyLibrary");
        }


        public IActionResult ReviewBook(string bookId)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            List<BookReview> myBookReviews = _libraryDB.BookReviews.Where(x => x.UserId == currentUser.Id).ToList();
            if (myBookReviews.Where(x => x.TitleIdApi == bookId).Count() > 0)
            {
                //User Already Reviewed this book
                TempData["ReviewBookError"] = "You have already reviewed this book. Go to 'My Book Reviews' if you would like to edit your review";
                return RedirectToAction("ViewApiInfoForSingleBook", bookId);
            }
            else
            {
                BookInfo apiBook = _libraryDAL.GetBookInfo(bookId);

                return View(apiBook);
            }
        }

        [HttpPost]
        public IActionResult ReviewBook(BookReview bookReview)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            bookReview.UserId = currentUser.Id;
            if (ModelState.IsValid)
            {
                _libraryDB.BookReviews.Add(bookReview);
                _libraryDB.SaveChanges();
                return RedirectToAction("MyBookReviews");
            }

            //We need validation in case that doesn't work
            return View();
        }

        public IActionResult MyBookReviews()
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);
            List<BookReview> myBookReviews = _libraryDB.BookReviews.Where(x => x.UserId == currentUser.Id).ToList();

            return View(myBookReviews);
        }

        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SearchResultsTitles(string query)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);

            List<Doc> results = new List<Doc>();
            results = _libraryDAL.GetSearchTitles(query);

            return View(results);
        }


        public IActionResult ViewLocalLibraries()
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);

            // get local users
            List<User> notUser = _libraryDB.Users.Where(x => x.UserId != user).ToList();
            List<User> withinDistance = GetLocalUsers(currentUser, notUser);
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

        public static List<User> GetLocalUsers(User currentUser, List<User> notUser)
        {
            List<User> within = new List<User>();
            // grab  everyone else's lat and lng from table
            // create a marker list of lat and lngs close to user
    
            int max = 0; // only show so many libraries

            const double radConv = Math.PI / 180;
            const int R = 6371; // radius of earth in km
            const int d = 20; // only show libraries within 20km
            double userLat = Double.Parse(currentUser.Latitude) * radConv;
            double otherLat;
            double deltaLng;
            foreach (User other in notUser)
            {
                otherLat = Double.Parse(other.Latitude) * radConv;
                deltaLng = Double.Parse(currentUser.Longitude) - Double.Parse(other.Longitude);

                if ((Math.Acos(Math.Sin(userLat) * Math.Sin(otherLat) + Math.Cos(userLat) * Math.Cos(otherLat) * Math.Cos(deltaLng)) * R) < d)
                {
                    within.Add(other);
                }

                if (max >= 15)
                {
                    break;
                }
                max++;
            }
            return within;
        }
    }
}
