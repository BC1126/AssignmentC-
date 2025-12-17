using AssignmentC_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class ActionLogController : Controller
{
    private readonly DB db;
    public ActionLogController(DB _db)
    {
        db = _db;
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ActionLogList(string name, DateTime? from, DateTime? to, int page = 1)
    {
        IQueryable<ActionLog> query = db.ActionLogs;

        // 1. Searching (Email, Name, or Entity)
        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(l => l.UserEmail.Contains(name) ||
                                     l.UserName.Contains(name) ||
                                     l.Entity.Contains(name));
        }

        // 2. Date Range Filtering
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value.AddDays(1).AddSeconds(-1));

        // 3. Paging logic (Demo 4)
        int pageSize = 15;
        int totalCount = query.Count();
        var logs = query.OrderByDescending(l => l.CreatedAt)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

        var vm = new ActionLogViewModel
        {
            Logs = logs,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            SearchName = name,
            FromDate = from,
            ToDate = to
        };

        // AJAX Detection (Demo 1 & 5)
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_ActionLogTable", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteOldLogs()
    {
        // Example: Delete logs older than 30 days
        var threshold = DateTime.Now.AddDays(-30);
        var oldLogs = db.ActionLogs.Where(l => l.CreatedAt < threshold);

        db.ActionLogs.RemoveRange(oldLogs);
        db.SaveChanges();

        TempData["Info"] = "Old logs deleted successfully.";
        return RedirectToAction("ActionLogList");
    }
}