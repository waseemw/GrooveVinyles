using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using GrooveVinyles.Data;
using GrooveVinyles.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GrooveVinyles.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace GrooveVinyles.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StoreDbContext _storeContext;

        public HomeController( ApplicationDbContext context, StoreDbContext storeDbContext)
        {
            _context = context;
            _storeContext = storeDbContext;
        }

        public IActionResult Index()
        {
            var adminExists = _context.Roles.Any(r => r.Name == "Admin");
            ViewBag.AdminExists = adminExists;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        [HttpPost]
        public IActionResult Index(string email, string password)
        {
            var adminExists = _context.Roles.Any(r => r.Name == "Admin");
            if (adminExists)
                return Problem();
            _context.SeedAdminUser(email, password);
            ViewBag.AdminExists = true;
            return View();
        }
        
        [HttpPost]
        public void Test(IList<IFormFile> file)
        {
            Console.WriteLine(file[0].FileName);
        }
    }
}