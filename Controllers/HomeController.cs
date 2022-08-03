using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SampleWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SampleWebApp.Controllers;

public class HomeController : Controller
{
    private MyContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, MyContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.NotLoggedIn = true;
        HttpContext.Session.Clear();
        return View();
    }

    [HttpPost("user/register")]
    public IActionResult Register(User newUser)
    {
        ViewBag.NotLoggedIn = false;
        if(ModelState.IsValid){
            if(_context.Users.Any(a =>a.Email == newUser.Email))
            {
                ModelState.AddModelError("Email","Email is already in use");
                return View("Index");
            }
            PasswordHasher<User> Hasher = new PasswordHasher<User>();
            newUser.Password = Hasher.HashPassword(newUser, newUser.Password);
            _context.Add(newUser);
            _context.SaveChanges();
            HttpContext.Session.SetInt32("user", newUser.UserId);
            return RedirectToAction("Dashboard");
        } else{
            return View("Index");
        }
    }

    [HttpPost("user/login")]
    public IActionResult Login(LogUser loginUser)
    {
        ViewBag.NotLoggedIn = false;
        if(ModelState.IsValid)
        {
            User userInDb = _context.Users.FirstOrDefault(a => a.Email == loginUser.LogEmail);
            if(userInDb == null)
            {
                ModelState.AddModelError("LogEmail","Invalid Login Attempt");
                return View("Index");
            }

            PasswordHasher<LogUser> hasher = new PasswordHasher<LogUser>();

            var result = hasher.VerifyHashedPassword(loginUser, userInDb.Password, loginUser.LogPassword);
            if(result == 0)
            {
                ModelState.AddModelError("LogEmail","Invalid login attempt");
                return View("Index");
            }
            HttpContext.Session.SetInt32("user", userInDb.UserId);
            return RedirectToAction("Dashboard");
        } else {
            return View("Index");
        }
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        ViewBag.AllWeddings = _context.Weddings.Include(m => m.GuestsInWedding).ThenInclude(d => d.User).ToList();
        ViewBag.SingleUser = _context.Users.FirstOrDefault(u => u.UserId == HttpContext.Session.GetInt32("user"));
        ViewBag.loggedInUser = HttpContext.Session.GetInt32("user");
        return View();
    }

    [HttpGet("unrsvp/{rsvpId}")]
    public IActionResult UnRsvp(int rsvpId)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        GuestList RsvpToDelete = _context.GuestLists.SingleOrDefault(d => d.UserId == HttpContext.Session.GetInt32("user") && d.WeddingId == rsvpId);
        _context.GuestLists.Remove(RsvpToDelete);
        _context.SaveChanges();
        return RedirectToAction("Dashboard");
    }

    [HttpGet("deletewedding/{wedId}")]
    public IActionResult DeleteWedding(int wedId)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.NotLoggedIn = false;
        Wedding? WeddingToDelete = _context.Weddings.SingleOrDefault(d => d.UserId == HttpContext.Session.GetInt32("user") && d.WeddingId == wedId);
        if(WeddingToDelete == null)
        {
            return RedirectToAction("Dashboard");
        }

        if(WeddingToDelete.UserId != HttpContext.Session.GetInt32("user"))
        {
            return RedirectToAction("Index");
        }
        
        _context.Weddings.Remove(WeddingToDelete);
        _context.SaveChanges();
        return RedirectToAction("Dashboard");
    }

    [HttpGet("addrsvp/{rsvpId}")]
    public IActionResult AddRsvp(int rsvpId)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        GuestList RsvpToAdd = new GuestList();
        RsvpToAdd.UserId = Convert.ToInt32(HttpContext.Session.GetInt32("user"));
        RsvpToAdd.WeddingId = rsvpId;
        _context.GuestLists.Add(RsvpToAdd);
        _context.SaveChanges();
        return RedirectToAction("Dashboard");
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        return View ();
    }

    [HttpPost("weddingadd")]
    public IActionResult WeddingAdd(Wedding newWedding)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.NotLoggedIn = false;
        if(ModelState.IsValid)
        {
            if(newWedding.WedDate <= DateTime.Now){
                ModelState.AddModelError("WedDate","Wedding Date is Invalid");
                return View("Create");
            }

            newWedding.UserId = (int)HttpContext.Session.GetInt32("user");
            _context.Add(newWedding);
            _context.SaveChanges();

            return Redirect($"/showwedding/{newWedding.WeddingId}");
        } else {
            return View("Create");
        }
    }

    [HttpGet("showwedding/{oneWed}")]
    public IActionResult Showwedding(int oneWed)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        Wedding WeddingToView = _context.Weddings.Include(d => d.GuestsInWedding).ThenInclude(c => c.User).FirstOrDefault(d => d.WeddingId == oneWed);
        string str = WeddingToView.WedderOne;
        string str2 = WeddingToView.WedderTwo;
        string firstname;
        string secondname;
        if(str.Contains(" "))
        {
            firstname = str.Remove(str.IndexOf(' '));
            WeddingToView.WedderOne = firstname;
        }

        if(str2.Contains(" "))
        {
            secondname = str2.Remove(str2.IndexOf(' '));
            WeddingToView.WedderTwo = secondname;
        }
    
        return View(WeddingToView);
    }

    [HttpGet("/wedding/editwedding/{WeddingId}")]
    public IActionResult EditWedding(int WeddingId)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        Wedding WeddingToEdit = _context.Weddings.FirstOrDefault(d => d.WeddingId == WeddingId);
        return View(WeddingToEdit);
    }

    [HttpPost("/wedding/update/{WeddingId}")]
    public IActionResult UpdateWedding(int WeddingId, Wedding newVersionOfWedding)
    {
        if(HttpContext.Session.GetInt32("user") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;

        if(ModelState.IsValid){
            if(newVersionOfWedding.WedDate <= DateTime.Now){
                ModelState.AddModelError("WedDate","Wedding Date is Invalid");
                Wedding WeddingToEdit = _context.Weddings.FirstOrDefault(a => a.WeddingId == WeddingId);
                return View("EditWedding", WeddingToEdit);
            }
            Wedding oldWedding = _context.Weddings.FirstOrDefault(a => a.WeddingId == WeddingId);
        
            oldWedding.WedderOne = newVersionOfWedding.WedderOne;
            oldWedding.WedderTwo = newVersionOfWedding.WedderTwo;
            oldWedding.WedDate = newVersionOfWedding.WedDate;
            oldWedding.WedAddress = newVersionOfWedding.WedAddress;
            oldWedding.UpdatedAt = DateTime.Now;
            _context.SaveChanges();

            return Redirect($"/showwedding/{oldWedding.WeddingId}");
        } else {
            Wedding WeddingToEdit = _context.Weddings.FirstOrDefault(a => a.WeddingId == WeddingId);
            return View("EditWedding", WeddingToEdit);
        }
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
