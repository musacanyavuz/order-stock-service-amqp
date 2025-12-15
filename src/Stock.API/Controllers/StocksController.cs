using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.API.Infrastructure;

namespace Stock.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly StockDbContext _context;

        public StocksController(StockDbContext context)
        {
            _context = context;
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetStocks()
        {
            var stocks = await _context.Stocks.ToListAsync();
            
            if (!stocks.Any())
               return BadRequest("No stocks found to reset.");
            

            foreach (var stock in stocks)
            {
                stock.Count = 100;
            }

            await _context.SaveChangesAsync();
            
            Console.WriteLine("[StocksController] Stocks manually reset to 100 via API.");
            return Ok(new { message = "Stocks reset to 100 successfully." });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetStocks()
        {
            var stocks = await _context.Stocks.ToListAsync();
            return Ok(stocks);
        }
    }
}
