using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GrooveVinyles.DataAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GrooveVinyles.Controllers
{
    public class StoreController : Controller
    {
        private readonly StoreDbContext _storeContext;

        private readonly ILogger<HomeController> _logger;
        public StoreController(StoreDbContext storeDbContext,ILogger<HomeController> logger)
        {
            _storeContext = storeDbContext;
            _logger = logger;
        }
        
        [HttpGet]
        [Route("[controller]/{page:int=1}")]
        public IActionResult Index(int page)
        {
            ViewBag.Page = page;
            var artists = (from artist in _storeContext.Artist.Include(artist => artist.Vinyles).OrderBy(artist => artist.ArtistId).Skip((page - 1) * 12).Take(12)
                select artist).ToList();
            ViewBag.Artists = artists;
            var count = Math.Ceiling((from artist in _storeContext.Artist select artist).Count() / 12.0);
            ViewBag.MaxPages = Math.Max(count, 1);
            return View("Index");
        }
        
        
        public IActionResult Cart()
        {
            var cartVinyles = GetCart();
            var vinyles = new List<Vinyl>();
            var total = 0;
            foreach (var cartVinyl in cartVinyles)
            {
                total += cartVinyl.Price;
                try
                {
                    var vinyl = vinyles.First(v => v.VinylId == cartVinyl.VinylId);
                    vinyl.VinylStock += 1;
                }
                catch (InvalidOperationException)
                {
                    cartVinyl.VinylStock = 1;
                    vinyles.Add(cartVinyl);
                }
            }
            ViewBag.Vinyles = vinyles;
            ViewBag.Total = total;
            return View("Cart");
        }
        
        
        
        [HttpPost]
        public async Task<RedirectResult> BuyAll(string fullName, string address)
        {
            TempData["Error"] = "";
            var vinyles = GetCart();
            var vinylIds = new List<int>();
            vinyles.ForEach(v => vinylIds.Add(v.VinylId));
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("token", "b1ec9431-ea54-4911-9375-997746074e93");
                var result = await client.PutAsJsonAsync("https://localhost:5001/api/", new {VinylIds = vinylIds, FullName = fullName, Address = address});
                var resultContent = await result.Content.ReadFromJsonAsync<List<int>>();
                var message = "";
                var leftVinyles = new List<Vinyl>();
                if (resultContent != null)
                    foreach (var vinyl in resultContent.Select(vinylId => vinyles.First(v => v.VinylId == vinylId)))
                    {
                        leftVinyles.Add(vinyl);
                        message += "No stock remaining for the vinyl " + vinyl.VinylName + "\n";
                    }
                TempData["Error"] = message;
                SetCart(leftVinyles);
                _logger.LogInformation("API PUT request status code: " + result.StatusCode);
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
        public void SetCart(List<Vinyl> vinyles)
        {
            foreach (var vinyl in vinyles)
            {
                if (vinyl.Artist != null)
                    vinyl.Artist.Vinyles = null;
            }

            HttpContext.Session.SetString("cartData", JsonConvert.SerializeObject(vinyles));
        }

        public IActionResult RemoveFromCart(int id)
        {
            var vinyles = GetCart();
            vinyles.Remove(vinyles.First(vinyl => vinyl.VinylId == id));
            HttpContext.Session.SetString("cartData", JsonConvert.SerializeObject(vinyles));
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public List<Vinyl> GetCart()
        {
            string jsonObject = HttpContext.Session.GetString("cartData") ?? "[]";
            List<Vinyl> vinyles = JsonConvert.DeserializeObject<List<Vinyl>>(jsonObject);
            return vinyles;
        }

        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var vinyl = _storeContext.Vinyl.Include(v => v.Artist).First(v => v.VinylId == id);
            var countInCart = GetCart().Count(v => v.VinylId == id);
            if (vinyl.VinylStock > countInCart)
            {
                if (vinyl.Artist != null)
                    vinyl.Artist.Vinyles = null;
                var vinyles = GetCart();
                vinyles.Add(vinyl);
                HttpContext.Session.SetString("cartData", JsonConvert.SerializeObject(vinyles));
            }
            else
            {
                TempData["Error"] = countInCart > 0 ? "Can't add more of this item, no more stock remaining!" : "Can't add item! No stock remaining!";
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }
        
        
        [HttpGet]
        [Route("[controller]/[action]/{artistId:int}/{page:int=1}")]
        public IActionResult Vinyles(int artistId, int page)
        {
            ViewBag.Page = page;
            var vinyles = (from vinyl in _storeContext.Vinyl.Include(vinyl => vinyl.Artist).Where(vinyl => vinyl.Artist.ArtistId == artistId)
                    .OrderBy(vinyl => vinyl.VinylId).Skip((page - 1) * 12).Take(12)
                select vinyl).ToList();
            ViewBag.Vinyles = vinyles;
            ViewBag.ArtistId = artistId;
            ViewBag.ArtistName = _storeContext.Artist.First(artist => artist.ArtistId == artistId).ArtistName;
            var count = Math.Ceiling((from vinyl in _storeContext.Vinyl.Include(vinyl => vinyl.Artist).Where(vinyl => vinyl.Artist.ArtistId == artistId) select vinyl)
                .Count() / 12.0);
            ViewBag.MaxPages = Math.Max(count, 1);
            return View("Vinyles");
        }
        
    }
}