using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GrooveVinyles.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GrooveVinyles.Controllers
{
    public class AdminController : Controller
    {
        private readonly StoreDbContext _context;

        private readonly ILogger<HomeController> _logger;

        public AdminController(ILogger<HomeController> logger, StoreDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddArtist(string name)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            _logger.LogInformation($"Creating artist using name {name}");
            await _context.Artist.AddAsync(new Artist {ArtistName = name});
            await _context.SaveChangesAsync();
            return Redirect("/Admin/ManageArtists");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteArtist(int id)
        {
            _logger.LogInformation("Deleting artist with id " + id);
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            var artist = new Artist {ArtistId = id};
            _context.Artist.Attach(artist);
            _context.Artist.Remove(artist);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/ManageArtists");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateArtist(int id, string name)
        {
            _logger.LogInformation($"Updating artist with id {id} to name {name}");

            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            var artist = await _context.Artist.FirstAsync(a => a.ArtistId == id);
            artist.ArtistName = name;
            await _context.SaveChangesAsync();
            return Redirect("/Admin/ManageArtists");
        }

        [HttpGet]
        [Route("[controller]/[action]/{page:int=1}")]
        public IActionResult ManageArtists(int page)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            ViewBag.Page = page;
            var artists = (from artist in _context.Artist.Include(artist => artist.Vinyles).OrderBy(artist => artist.ArtistId).Skip((page - 1) * 10).Take(10)
                select artist).ToList();
            ViewBag.Artists = artists;
            var count = Math.Ceiling((from artist in _context.Artist select artist).Count() / 10.0);
            ViewBag.MaxPages = Math.Max(count, 1);
            return View("ManageArtists");
        }


        [HttpPost]
        public async Task<IActionResult> AddVinyl(string name, string genre, int artistId, int stock, int price)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            _logger.LogInformation($"Creating vinyl using name {name} genre {genre} stock {stock} and artistID {artistId} and price {price}");
            await _context.Vinyl.AddAsync(new Vinyl
            {
                VinylName = name, VinylStock = stock, Genre = genre,
                Artist = artistId == 0 ? null : await _context.Artist.FirstAsync(a => a.ArtistId == artistId),
                Price = price
            });
            await _context.SaveChangesAsync();
            return Redirect("/Admin/ManageVinyles");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVinyl(int id)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            _logger.LogInformation("Deleting vinyl with id " + id);
            var vinyl = new Vinyl {VinylId = id};
            _context.Vinyl.Attach(vinyl);
            _context.Vinyl.Remove(vinyl);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/ManageVinyles");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVinyl(int id, string name, int stock, string genre, int artistId, int price)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            _logger.LogInformation($"Updating vinyl with id {id} to name {name} genre {genre} stock {stock} and artist ID {artistId} and price {price}");
            var vinyl = await _context.Vinyl.FirstAsync(a => a.VinylId == id);
            vinyl.VinylName = name;
            vinyl.VinylStock = stock;
            vinyl.Genre = genre;
            vinyl.Price = price;
            if (artistId != 0)
                vinyl.Artist = await _context.Artist.FirstAsync(a => a.ArtistId == artistId);
            await _context.SaveChangesAsync();
            return Redirect("/Admin/ManageVinyles");
        }

        [HttpGet]
        [Route("[controller]/[action]/{page:int=1}")]
        public IActionResult ManageVinyles(int page)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            ViewBag.Page = page;
            var vinyles = (from vinyl in _context.Vinyl.Include(vinyl => vinyl.Artist).OrderBy(vinyl => vinyl.VinylId).Skip((page - 1) * 10).Take(10) select vinyl)
                .ToList();
            ViewBag.Vinyles = vinyles;

            var count = Math.Ceiling((from vinyl in _context.Vinyl select vinyl).Count() / 10.0);
            ViewBag.MaxPages = Math.Max(count, 1);
            return View("ManageVinyles");
        }

        [HttpGet]
        [Route("[controller]/[action]/{page:int=1}")]
        public async Task<IActionResult> ManageOrders(int page)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            ViewBag.Page = page;
            page--;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("token", "b1ec9431-ea54-4911-9375-997746074e93");
                var orders = await (await client.GetAsync("https://localhost:5001/api/" + page)).Content.ReadFromJsonAsync<List<Order>>();
                ViewBag.Orders = orders;
                ViewBag.MaxPages = await (await client.GetAsync("https://localhost:5001/api/pages")).Content.ReadFromJsonAsync<int>();
            }

            return View("ManageOrders");
        }

        [HttpGet]
        public async Task<IActionResult> ManageOrder(int orderId)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("token", "b1ec9431-ea54-4911-9375-997746074e93");
                var order = await (await client.GetAsync("https://localhost:5001/api/order/" + orderId)).Content.ReadFromJsonAsync<Order>();
                ViewBag.Order = order;
            }

            return View("ManageOrder");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("token", "b1ec9431-ea54-4911-9375-997746074e93");
                var result = await client.PostAsJsonAsync("https://localhost:5001/api/", id);
                _logger.LogInformation($"Approved Order with id {id} and status code is {result.StatusCode}");
            }

            return await ManageOrders(1);
        }

        [HttpPost]
        public async Task<IActionResult> DenyOrder(int id)
        {
            if (!(User.Identity is {IsAuthenticated: true} && User.IsInRole("Admin"))) return Problem();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("token", "b1ec9431-ea54-4911-9375-997746074e93");
                var result = await client.DeleteAsync("https://localhost:5001/api/" + id);
                _logger.LogInformation($"Denied Order with id {id} and status code is {result.StatusCode}");
            }

            return await ManageOrders(1);
        }
    }
}