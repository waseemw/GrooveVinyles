using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrooveVinyles.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace GrooveVinyles.API
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private const string Token = "b1ec9431-ea54-4911-9375-997746074e93";
        private readonly StoreDbContext _context;

        public ApiController(StoreDbContext context)
        {
            _context = context;
        }

        // GET: api/5
        [HttpGet("{id:int=0}")]
        public List<Order> Get(int id, [FromHeader] string token)
        {
            if (token != Token)
                return null;
            var orders = (from order in _context.Order.Include(order => order.VinylPurchases).ThenInclude(v => v.Vinyl).OrderBy(order => order.OrderId).Skip(id * 10)
                    .Take(12)
                select order).ToList();
            orders.ForEach(o =>
            {
                foreach (var vinylPurchase in o.VinylPurchases)
                {
                    vinylPurchase.Orders = null;
                    vinylPurchase.Vinyl.VinylPurchases = null;
                }
            });
            return orders;
        }

        [HttpGet("order/{id:int}")]
        public Order GetOrder(int id, [FromHeader] string token)
        {
            if (token != Token)
                return null;
            var order = _context.Order.Include(o => o.VinylPurchases).ThenInclude(v => v.Vinyl).First(o => o.OrderId == id);
            foreach (var purchase in order.VinylPurchases)
            {
                purchase.Orders = null;
                purchase.Vinyl.VinylPurchases = null;
            }

            return order;
        }

        [HttpGet("pages")]
        public double GetOrderPages(int id, [FromHeader] string token)
        {
            if (token != Token)
                return 0;
            var count = Math.Ceiling((from order in _context.Order select order).Count() / 10.0);
            return Math.Max(count, 1);
        }

        // POST: api
        [HttpPost]
        public async void Post([FromBody] int orderId, [FromHeader] string token)
        {
            if (token != Token)
                return;
            var order = new Order {OrderId = orderId};
            _context.Order.Attach(order);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
        }


        // PUT: api
        [HttpPut]
        public async Task<List<int>> Put([FromBody] OrderPut obj, [FromHeader] string token)
        {
            if (token != Token)
                return null;
            var vinylIds = obj.VinylIds;
            var vinylPurchases = new List<VinylPurchase>();

            var list = new List<int>();
            foreach (var id in vinylIds)
            {
                var actualVinyl = _context.Vinyl.Include(v => v.Artist).First(v => v.VinylId == id);
                if (actualVinyl.VinylStock > 0)
                {
                    actualVinyl.VinylStock--;
                    await UpdateVinyl(actualVinyl.VinylId, actualVinyl.VinylName, actualVinyl.VinylStock, actualVinyl.Genre, actualVinyl.Artist.ArtistId,
                        actualVinyl.Price);
                    try
                    {
                        var vinylPurchase = vinylPurchases.First(v => v.Vinyl.VinylId == id);
                        vinylPurchase.Amount += 1;
                    }
                    catch (InvalidOperationException)
                    {
                        vinylPurchases.Add(new VinylPurchase {Amount = 1, Vinyl = actualVinyl});
                    }
                }
                else
                {
                    list.Add(id);
                }
            }

            await _context.Order.AddAsync(new Order
            {
                Address = obj.Address,
                FullName = obj.FullName,
                VinylPurchases = vinylPurchases
            });
            await _context.SaveChangesAsync();
            return list;
        }

        public class OrderPut
        {
            public string Address { get; set; }
            public string FullName { get; set; }
            public List<int> VinylIds { get; set; }
        }

        // DELETE: api/5
        [HttpDelete("{id:int}")]
        public async Task Delete(int id, [FromHeader] string token)
        {
            if (token != Token)
                return;
            var order = _context.Order.Include(p => p.VinylPurchases).ThenInclude(v => v.Vinyl).ThenInclude(v => v.Artist).First(p => p.OrderId == id);
            foreach (var vinylPurchase in order.VinylPurchases)
                await UpdateVinyl(vinylPurchase.Vinyl.VinylId, vinylPurchase.Vinyl.VinylName, vinylPurchase.Vinyl.VinylStock + vinylPurchase.Amount,
                    vinylPurchase.Vinyl.Genre, vinylPurchase.Vinyl.Artist.ArtistId, vinylPurchase.Vinyl.Price);
            _context.Order.Attach(order);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
        }


        private async Task UpdateVinyl(int id, string name, int stock, string genre, int artistId, int price)
        {
            var vinyl = await _context.Vinyl.FirstAsync(a => a.VinylId == id);
            vinyl.VinylName = name;
            vinyl.VinylStock = stock;
            vinyl.Genre = genre;
            vinyl.Price = price;
            if (artistId != 0)
                vinyl.Artist = await _context.Artist.FirstAsync(a => a.ArtistId == artistId);
            await _context.SaveChangesAsync();
        }
    }
}