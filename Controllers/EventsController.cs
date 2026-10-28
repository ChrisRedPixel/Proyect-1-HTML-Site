using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
   private readonly ILogger<EventsController> _logger;
   private readonly ApplicationDbContext _context;

   public EventsController(
         ILogger<EventsController> logger,
         ApplicationDbContext context)
   {
      _logger = logger;
      _context = context;
   }

   public async Task<IActionResult> Index()
   {
      _logger.LogInformation("Loading Published upcoming CEMS events.");

      var now = DateTime.Now;

      var query = _context.Events
            .AsNoTracking()
            .Include(eventItem => eventItem.Category)
            .Where(eventItem =>
                  eventItem.Status == EventStatus.Published &&
                  eventItem.StartDateTime > now)
            .OrderBy(eventItem => eventItem.StartDateTime)
            .ThenBy(eventItem => eventItem.Id);

      var events = await query.ToListAsync();

      return View(events);
   }
}
