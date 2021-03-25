using CommunityLibrary.Models;
using CommunityLibrary.ViewModels;
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
            return View();
        }


        public IActionResult Profile()
        {
            
            User currentUser = CurrentUser();

            ProfileViewModel profile = new ProfileViewModel();

            profile.User = currentUser;
            profile.OwnedBooksCount = _libraryDB.Books.Count(x => x.BookOwner == currentUser.Id && x.IsActive == true);
            profile.LendingCount = _libraryDB.Loans.Count(x => x.BookOwner == currentUser.Id && x.LoanStatus == true);
            profile.BorrowingCount = _libraryDB.Loans.Count(x => x.BookLoaner == currentUser.Id && x.LoanStatus == true);
            profile.CurrentRating = Convert.ToInt32(currentUser.CumulatvieRating);
            profile.ReviewCount = _libraryDB.BookReviews.Count(x => x.UserId == currentUser.Id);
            profile.RequestCount = 0;

            foreach (var l in _libraryDB.Loans.Where(x => x.BookOwner == currentUser.Id && x.LoanStatus))
            {
                if(l.IsDateEmpty())
                {
                    profile.RequestCount++;
                }
            }

            if (profile.User.ProfileImage == null)
            {
                profile.User.ProfileImage = "../default.png";
            }

            return View(profile);
        }

        public IActionResult UpdateProfile(int Id)
        {
            User currentUser = CurrentUser();
            return View(currentUser);
        }

        [HttpPost]
        public IActionResult UpdateProfile(User updated)
        {

            if (ModelState.IsValid)
            {

                User currentUser = CurrentUser();
                List<Result> latLng = _googleDAL.GetResults(updated.UserLocation);
                // check to see if enetered user address exists
                if (latLng.Count != 0)
                {
                    // set top match to user's location
                    currentUser.Latitude = latLng[0].geometry.location.lat.ToString();
                    currentUser.Longitude = latLng[0].geometry.location.lng.ToString();
                    currentUser.UserLocation = updated.UserLocation;
                }

                currentUser.ProfileImage = updated.ProfileImage;
                currentUser.UserName = updated.UserName;

                _libraryDB.Users.Update(currentUser);
                _libraryDB.SaveChanges();
            }
            return RedirectToAction("Profile");
        }

        public IActionResult Transactions()
        {

            User currentUser = CurrentUser();
            TempData["CurrentUser"] = currentUser.Id;

            // grab all loans user is involved in, both sides
            List<Loan> userLoans = _libraryDB.Loans.Where(x => x.BookLoaner == currentUser.Id || x.BookOwner == currentUser.Id).ToList();
            //-------
            List<LoanRating> userLoansMoreInfo = new List<LoanRating>();
            foreach (Loan loan in userLoans)
            {
                //Create loan review object to pass to view
                LoanRating l = new LoanRating();

                //assign current user to LoanRating object
                l.currentUser = currentUser;
                l.loan = loan;

                //find book for bookInfo
                Book book = _libraryDB.Books.Find(loan.BookId);
                BookInfo apibook = _libraryDAL.GetBookInfo(book.TitleIdApi);
                l.ApiBook = apibook;

                if (loan.BookOwner ==currentUser.Id)
                {
                    l.otherUser = _libraryDB.Users.Find(loan.BookLoaner);
                }
                else
                {
                    l.otherUser = _libraryDB.Users.Find(loan.BookOwner);
                }

                l.otherEmail = _libraryDB.AspNetUsers.Find(l.otherUser.UserId).UserName;
                userLoansMoreInfo.Add(l);
            }

        
            
            return View(userLoansMoreInfo);
        }

        public IActionResult Approval(int loanId)
        {
            // Populate container with necessary info then send to view
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = _libraryDB.Users.First(x => x.UserId == user);

            // There are two key things the view needs to know:
            // 1. who is viewing the page? owner or borrower (figured out above, set below)
            // 2. what is the state of the loan? pending, checked out, or returned? (handled below)

            ApprovalViewModel approvalViewModel = new ApprovalViewModel();

            approvalViewModel.CurrentUser = currentUser;
            approvalViewModel.Loan = _libraryDB.Loans.First(x => x.Id == loanId);
            approvalViewModel.BookBorrower = _libraryDB.Users.First(x => x.Id == approvalViewModel.Loan.BookLoaner);
            approvalViewModel.BookOwner = _libraryDB.Users.First(x => x.Id == approvalViewModel.Loan.BookOwner);
            approvalViewModel.Book = _libraryDB.Books.First(x => x.Id == approvalViewModel.Loan.BookId);
            approvalViewModel.BookTitle = _libraryDAL.GetBookInfo(approvalViewModel.Book.TitleIdApi).title;
            approvalViewModel.BookBorrowerRating = Convert.ToInt32(approvalViewModel.BookBorrower.CumulatvieRating);

            // If a date is empty, that is how we siganal that a loan is PENDING (book owner has yet to approve/decline)
            if (approvalViewModel.Loan.IsDateEmpty() && approvalViewModel.Loan.LoanStatus == true)
            {
                approvalViewModel.CurrentState = CurrentState.Pending;
            }
            // The date is not empty, which is how we signal a due date was created, thus the book must have been checked out 
            else if (approvalViewModel.Loan.LoanStatus == true)
            {
                approvalViewModel.CurrentState = CurrentState.CheckedOut;
            }
            else
            {
                approvalViewModel.CurrentState = CurrentState.Returned;
            }

            return View(approvalViewModel);
        }

        [HttpPost]
        public IActionResult Approval(Loan newDetails)
        {
            User currentUser = CurrentUser();

            Loan oldDetails = _libraryDB.Loans.First(x => x.Id == newDetails.Id);
            Book loanedBook = _libraryDB.Books.First(x => x.Id == oldDetails.BookId);

            // check to see if user has loaned this book out already
            if (loanedBook.AvailibilityStatus == false && newDetails.LoanStatus)
            {
                // unable to approve loan request
                TempData["Loaned"] = "Could not complete request: You have already loaned out this book";
                return RedirectToAction("Transactions");
            }

            // update status and comments
            if (newDetails.OwnerNote is not null)
            {
                oldDetails.OwnerNote = newDetails.OwnerNote;
            }
            if (newDetails.LoanerNote is not null)
            {
                oldDetails.LoanerNote = newDetails.LoanerNote;

            }
            oldDetails.LoanStatus = newDetails.LoanStatus;
            

            // newly approved rentals tasks
            if(oldDetails.LoanStatus && oldDetails.IsDateEmpty() && oldDetails.BookOwner == currentUser.Id)

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

            if(!oldDetails.LoanStatus && oldDetails.IsDateEmpty())
            {
                _libraryDB.Loans.Remove(oldDetails);
                _libraryDB.SaveChanges();
                return RedirectToAction("Transactions");
            }

            // Ended transactions, revert book to owner
            if (!oldDetails.LoanStatus)
            {
                loanedBook.AvailibilityStatus = true;
                loanedBook.CurrentHolder = oldDetails.BookOwner;
            }

            _libraryDB.Loans.Update(oldDetails);
            _libraryDB.SaveChanges();

            return RedirectToAction("Transactions");
        }


        public IActionResult RequestLoan(int Id)
        {
            
            User currentUser = CurrentUser();

            // grab book details and see if user loaning already
            Book renting = _libraryDB.Books.First(x => x.Id == Id);

            List<Loan> alreadyLoaning = _libraryDB.Loans.Where(x => (int)x.BookLoaner == currentUser.Id && x.BookId == renting.Id && x.LoanStatus).ToList();

            if (alreadyLoaning.Count < 1)
            {
                Loan newLoan = new Loan();
                newLoan.BookId = Id;

                // grab user details
                newLoan.BookLoaner = currentUser.Id;
                newLoan.RecipientRating = 0;


                

                // grab owner details
                User bookOwner = _libraryDB.Users.First(x => x.Id == renting.BookOwner);
                newLoan.OwnerRating = 0;
                newLoan.BookOwner = bookOwner.Id;
                newLoan.LoanStatus = true;

                newLoan.DueDate = null;

                // update database
                _libraryDB.Loans.Add(newLoan);
                _libraryDB.SaveChanges();
            }
            else
            {
                // unable to process loan request
                TempData["Pending"] = "Could not complete request: You have already requested this book";
            }
            // go to transaction page to see added request
            return RedirectToAction("Transactions");
        }
        public IActionResult RateLoan(int loanId)
        {
            //Create loan review object to pass to view
            LoanRating loanRating = new LoanRating();

            //find current user
            
            User currentUser = CurrentUser();
            //assign current user to LoanRating object
            loanRating.currentUser = currentUser;

            //find loan in database
            Loan loanToRate = _libraryDB.Loans.Find(loanId);

            loanRating.loan = loanToRate;

            //find book for bookInfo
            Book book = _libraryDB.Books.Find(loanToRate.BookId);
            BookInfo apibook = _libraryDAL.GetBookInfo(book.TitleIdApi);

            loanRating.ApiBook = apibook;


            return View(loanRating);
        }

        [HttpPost]
        public IActionResult RateLoan(int loanId, int Rating)
        {
            User currentUser = CurrentUser();
            User userRecievingRating = new User();

            Loan loanToReview = _libraryDB.Loans.Find(loanId);

            //if person leaving rating is the owner--we're rating the book borrower
            if (currentUser.Id == loanToReview.BookOwner)
            {
                loanToReview.RecipientRating = Rating;
                
                userRecievingRating = _libraryDB.Users.Find((int)loanToReview.BookLoaner);

            }
            else /*othewise the person rating is the book borrower and  we're rating the owner of the book*/
            {
                loanToReview.OwnerRating = Rating;
                
                userRecievingRating = _libraryDB.Users.Find((int)loanToReview.BookOwner);
            }
            
            //update loan rating
            _libraryDB.Loans.Update(loanToReview);
            
            //update user rating

            List<Loan> borrowingLoans = _libraryDB.Loans.Where(x => x.BookLoaner == userRecievingRating.Id && x.RecipientRating != 0).ToList();
            List<Loan> lendingLoans = _libraryDB.Loans.Where(x =>x.BookOwner == userRecievingRating.Id && x.OwnerRating != 0).ToList();

            List<int> totalLoanRatings = new List<int>();
            totalLoanRatings.Add(Rating);
            foreach (Loan loan in borrowingLoans)
            {
                totalLoanRatings.Add((int)loan.RecipientRating);
            }

            foreach (Loan loan in lendingLoans)
            {
                totalLoanRatings.Add((int)loan.OwnerRating);
            }

            //Do we want to change this to a double instead of an int since it's an average?
            double newRating = Math.Round(totalLoanRatings.Average(),2);
            userRecievingRating.CumulatvieRating = newRating;
       
            _libraryDB.Users.Update(userRecievingRating);
            _libraryDB.SaveChanges();
            return RedirectToAction("Transactions");
        }



        public IActionResult UserMap()
        {
            // Grab users lat and lng
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = CurrentUser();
            TempData["lat"] = currentUser.Latitude;
            TempData["lng"] = currentUser.Longitude;

            // get local users
            List<User> notUser = _libraryDB.Users.Where(x => x.UserId != user).ToList();
            List<User> withinDistance = GetLocalUsers(currentUser,notUser);

            // Remove users with no books in their collection
            withinDistance.RemoveAll(x => _libraryDB.Books.Count(j=> j.BookOwner == x.Id && (bool)j.IsActive) < 1);

            return View(withinDistance);
        }
        public IActionResult ViewApiInfoForSingleBook(string bookId)
        {

            try
            {
            User currentUser = CurrentUser();

            BookInfo apiBook = _libraryDAL.GetBookInfo(bookId);
            List<Author> authors = new List<Author>();

            TempData["AlreadyHasBook"] = DoesUserHaveThisBook(currentUser.Id, bookId);
            TempData["AlreadyWroteReview"] = HasUserWrittenReview(currentUser.Id, bookId);
            
            if (apiBook.authors is not null)
            {
                foreach (Author author in apiBook.authors)
                {
                    if (author.author.key is not null)
                    {

                    string authorId = author.author.key;
                    Author apiAuthor = _libraryDAL.GetAuthorInfo(authorId);

                    authors.Add(apiAuthor);

                    }
                }
                apiBook.authors = authors;
            }

                List<BookReview> reviewForThisBook = _libraryDB.BookReviews.Where(x => x.TitleIdApi == apiBook.key).ToList();

                if (reviewForThisBook.Count>0)
                {
                    TempData["reviewsExist"] = "true";
                }

            return View(apiBook);


            }
            catch (Exception)
            {

                TempData["errorMessage"] = "Something went wrong. We cannot display that title at the moment";
                return RedirectToAction("ErrorMessage");
            }
        }

        public IActionResult AddBookToLibrary(string bookId)
        {
            try
            {
                User currentUser = CurrentUser();
                List<Book> personalLibrary = _libraryDB.Books.Where(x => x.BookOwner == currentUser.Id).ToList();

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
            catch (Exception)
            {
                TempData["errorMessage"] = "Something went wrong. We cannot add that book to your library right now";
                return RedirectToAction("ErrorMessage");
            }

        }

        public IActionResult MyLibrary()
        {
            //find current user
            User currentUser = CurrentUser();

            //Find all books where the owner is the current user and the book is active
            List<Book> dbPersonalLibrary = _libraryDB.Books.Where(x => x.BookOwner == currentUser.Id && x.IsActive== true).ToList();

            List<LibraryBook> libraryBooks = new List<LibraryBook>();

            foreach (Book book in dbPersonalLibrary)
            {
                //let's get their info about that book in the api
                BookInfo apiBook = _libraryDAL.GetBookInfo(book.TitleIdApi);
                //let's get the author info
                List<Author> authors = new List<Author>();
                if (apiBook.authors is not null)
                {
                    foreach (Author author in apiBook.authors)
                    {
                        string authorId = author.author.key;
                        Author apiAuthor = _libraryDAL.GetAuthorInfo(authorId);

                        authors.Add(apiAuthor);
                    }
                    apiBook.authors = authors;
                }
                LibraryBook libraryBook = new LibraryBook();

                User bookHolder = _libraryDB.Users.First(x => x.Id == book.CurrentHolder);
                User bookOwner = _libraryDB.Users.First(x => x.Id == book.BookOwner);
            
                libraryBook.ApiBook = apiBook;
                libraryBook.DbBook = book;
                libraryBook.BookOwner = bookOwner.UserName;
                libraryBook.BookHolder = bookHolder.UserName;
                libraryBooks.Add(libraryBook);
            }


            return View(libraryBooks);
        }

        public IActionResult RemoveFromLibrary(int bookId)
        {
            Book currentBook = _libraryDB.Books.Find(bookId);

            List<Loan> thisBooksLoans = _libraryDB.Loans.Where(x => x.BookId == bookId).ToList();

            if (thisBooksLoans.Count == 0)
            {

                _libraryDB.Books.Remove(currentBook);

            }
            else
            {
                currentBook.IsActive = false;
                _libraryDB.Update(currentBook);

            }
            _libraryDB.SaveChanges();
            return RedirectToAction("MyLibrary");
        }

        public IActionResult EditLoanPeriod(int bookId, int loanPeriod)
        {
            Book currentBook = _libraryDB.Books.Find(bookId);
            currentBook.LoanPeriod = loanPeriod;
            _libraryDB.Books.Update(currentBook);
            _libraryDB.SaveChanges();
            return RedirectToAction("MyLibrary");
        }

        public IActionResult ReviewBook(string bookId)
        {
            User currentUser = CurrentUser();
            List<BookReview> myBookReviews = _libraryDB.BookReviews.Where(x => x.UserId == currentUser.Id).ToList();

            BookInfo apiBook = _libraryDAL.GetBookInfo(bookId);
            if (myBookReviews.Where(x => x.TitleIdApi == bookId).Count() > 0)
            {
                return RedirectToAction("MyBookReviews");
            }
            else
            {
                
                return View(apiBook);
            }
        }

        [HttpPost]
        public IActionResult ReviewBook(BookReview bookReview)
        {
            User currentUser = CurrentUser();
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
        public IActionResult UpdateBookReview (int reviewId)
        {
            Review review = new Review();
            BookReview bookReview = _libraryDB.BookReviews.Find(reviewId);
            review.review = bookReview;
            review.ApiBook = _libraryDAL.GetBookInfo(bookReview.TitleIdApi);
            return View(review);
        }
        [HttpPost]
        public IActionResult UpdateBookReview(BookReview bookReview)
        {
            User currentUser = CurrentUser();
            List<BookReview> myBookReviews = _libraryDB.BookReviews.Where(x => x.UserId == currentUser.Id).ToList();
            BookReview reviewToUpdate = myBookReviews.Where(x => x.TitleIdApi == bookReview.TitleIdApi).First();

            reviewToUpdate.Review = bookReview.Review;
            reviewToUpdate.Rating = bookReview.Rating;
            _libraryDB.BookReviews.Update(reviewToUpdate);
            _libraryDB.SaveChanges();
            return RedirectToAction("MyBookReviews");
        }
        public IActionResult DeleteReview(int reviewId)
        {
            BookReview bookReview = _libraryDB.BookReviews.Find(reviewId);
            _libraryDB.BookReviews.Remove(bookReview);
            _libraryDB.SaveChanges();
            return RedirectToAction("MyBookReviews");

        }
        

        public IActionResult MyBookReviews()
        {
            User currentUser = CurrentUser();
            
            List<BookReview> myBookReviews = _libraryDB.BookReviews.Where(x => x.UserId == currentUser.Id).ToList();
            List<Review> reviews = new List<Review>();
            
            foreach (BookReview review in myBookReviews)
            {
                BookInfo apiBook = _libraryDAL.GetBookInfo(review.TitleIdApi);
                Review review1 = new Review();
                review1.review = review;
                review1.ApiBook = apiBook;
                reviews.Add(review1);
            }


            return View(reviews);
        }
        public IActionResult ReviewsForThisBook(string bookId)
        {
            User currentUser = CurrentUser();
            BookInfo apiBook1 = _libraryDAL.GetBookInfo(bookId);
            List<BookReview> reviewsForthisbook = _libraryDB.BookReviews.Where(x => x.TitleIdApi==apiBook1.key).ToList();
            List<Review> reviews = new List<Review>();

            foreach (BookReview review in reviewsForthisbook)
            {
                BookInfo apiBook = _libraryDAL.GetBookInfo(review.TitleIdApi);
                User reviewer = _libraryDB.Users.Find(review.UserId);
                Review review1 = new Review();
                review1.review = review;
                review1.ApiBook = apiBook;
                review1.reviewer = reviewer;
                reviews.Add(review1);
            }
           

            return View(reviews);
        }

        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SearchResultsTitles(string query)
        {
            User currentUser = CurrentUser();

            List<Doc> results = new List<Doc>();
            results = _libraryDAL.GetSearchTitles(query);
            TempData["query"] = query;
            return View(results);
        }


        public IActionResult LocalLibraries(int? id)
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            User currentUser = CurrentUser();

            // get local users
            List<User> notUser = _libraryDB.Users.Where(x => x.UserId != user).ToList();
            List<User> withinDistance = GetLocalUsers(currentUser, notUser);
            
            
            // list of local book ids
            List<Book> localLibraries = new List<Book>();

            // only want specific id within distance if it is set
            if(id is not null)
            {
                TempData["OneLibrary"] = "true";
                withinDistance = withinDistance.Where(x => x.Id == id).ToList();
                TempData["Name"] = withinDistance[0].UserName;
                TempData["Rating"] = withinDistance[0].CumulatvieRating;
            }
            
            foreach (User local in withinDistance)
            {
                localLibraries.AddRange(_libraryDB.Books.Where(x => x.BookOwner == local.Id && x.IsActive==true));
            }

            // list of book containers
            List<LibraryBook> libraryBooks = new List<LibraryBook>();

            foreach (Book book in localLibraries)
            {
                BookInfo bookDetails = _libraryDAL.GetBookInfo(book.TitleIdApi);

                List<Author> authors = new List<Author>();
                if (bookDetails.authors is not null)
                {
                    foreach (Author author in bookDetails.authors)
                    {
                        string authorId = author.author.key;
                        Author apiAuthor = _libraryDAL.GetAuthorInfo(authorId);

                        authors.Add(apiAuthor);
                    }
                    bookDetails.authors = authors;
                }

                // container info for a book
                LibraryBook libraryBook = new LibraryBook();

                User bookHolder = _libraryDB.Users.First(x => x.Id == book.CurrentHolder);
                User bookOwner = _libraryDB.Users.First(x => x.Id == book.BookOwner);

                libraryBook.ApiBook = bookDetails;
                libraryBook.DbBook = book;
                libraryBook.BookOwner = bookHolder.UserName;
                libraryBook.BookHolder = bookOwner.UserName;
                libraryBook.BookOwnerId = bookOwner.Id;
                libraryBooks.Add(libraryBook);
            }

            return View(libraryBooks);
        }
        public IActionResult ErrorMessage()
        {
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
            const int d = 15; // only show libraries within 15km
            double userLat = Double.Parse(currentUser.Latitude) * radConv;
            double otherLat;
            double deltaLng;
            foreach (User other in notUser)
            {
                otherLat = Double.Parse(other.Latitude) * radConv;
                deltaLng = (Double.Parse(currentUser.Longitude) - Double.Parse(other.Longitude)) * radConv;

                if ((Math.Acos(Math.Sin(userLat) * Math.Sin(otherLat) + Math.Cos(userLat) * Math.Cos(otherLat) * Math.Cos(deltaLng))) * R < d)
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

        public bool DoesUserHaveThisBook(int userId, string bookId)
        {
            User currentUser = _libraryDB.Users.First(x => x.Id == userId);
            List<Book> personalLibrary = _libraryDB.Books.Where(x => x.BookOwner == currentUser.Id).ToList();
            if (personalLibrary.Where(x => x.TitleIdApi == bookId).Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasUserWrittenReview(int userId, string bookId)
        {
            User currentUser = _libraryDB.Users.First(x => x.Id == userId);
            List<BookReview> myBookReviews = _libraryDB.BookReviews.Where(x => x.UserId == currentUser.Id).ToList();
            if (myBookReviews.Exists(x => x.TitleIdApi == bookId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public User CurrentUser()
        {
            string user = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            try
            {

                //If they're logged in- find that identity userId within the user table 
                User currentUser = _libraryDB.Users.Where(x => x.UserId == user).First();
                return currentUser;
            }
            catch (Exception)
            {
                //if the identity userId isn't in the user table--make a new user
                //doing this here because all users are routed to the index page after logging in
                User newCurrentUser = new User();
                newCurrentUser.UserId = user;
                newCurrentUser.CumulatvieRating = 5;
                newCurrentUser.UserName = _libraryDB.AspNetUsers.Find(user).UserName;

                //Maps doesn't work if ALL users in the database don't have a location,
                //automatically setting all users to Detroit- they can change in user profile
                newCurrentUser.UserLocation = "Detroit";
                newCurrentUser.Latitude = "42.33143";
                newCurrentUser.Longitude = "-83.04575";
                _libraryDB.Users.Add(newCurrentUser);
                _libraryDB.SaveChanges();
                return newCurrentUser;

            }

        }
    }
}
