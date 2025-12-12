using Microsoft.AspNetCore.Mvc;

namespace AssignmentC_;

public class ProductController(DB db,
                               Helper hp) : Controller
{
    // Helper: Always trim Name and City to avoid filter breaking
    private List<dynamic> GetOutlets()
    {
        return db.Outlets
                 .Select(o => new { Name = o.Name.Trim(), City = o.City.Trim() })
                 .ToList<dynamic>();
    }

    // GET: Product/Index
    public IActionResult Index()
    {
        var model = db.Products;
        return View(model);
    }

    // GET: Product/CheckId
    public bool CheckId(string id)
    {
        return !db.Products.Any(p => p.Id == id);
    }

    private string NextId()
    {
        string max = db.Products.Max(p => p.Id) ?? "P000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'P'000");
    }

    // GET: Product/Insert
    public IActionResult Insert()
    {
        var vm = new ProductInsertVM
        {
            Id = NextId(),
            Price = 0.01m
        };

        var outlets = GetOutlets();

        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();
        ViewBag.Cinemas = outlets;
        ViewBag.Categories = new List<string> {
            "Ala cart", "Drinks", "Merchandise", "Snack"
        };

        return View(vm);
    }

    // POST: Product/Insert
    [HttpPost]
    public IActionResult Insert(ProductInsertVM vm)
    {
        if (ModelState["Id"]?.Errors.Count == 0 && db.Products.Any(p => p.Id == vm.Id))
            ModelState.AddModelError("Id", "Duplicated Id.");

        if (ModelState["Photo"]?.Errors.Count == 0)
        {
            var e = hp.ValidatePhoto(vm.Photo);
            if (e != "") ModelState.AddModelError("Photo", e);
        }

        var outlets = GetOutlets();

        if (!outlets.Any(o => o.Name == vm.Cinema && o.City == vm.Region))
            ModelState.AddModelError("Cinema", "Selected cinema does not match the selected region.");

        if (ModelState.IsValid)
        {
            db.Products.Add(new Product
            {
                Id = vm.Id,
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                Stock = vm.Stock,
                Region = vm.Region,
                Cinema = vm.Cinema,
                Category = vm.Category,
                PhotoURL = hp.SavePhoto(vm.Photo, "products")
            });
            db.SaveChanges();

            TempData["Info"] = "Product inserted.";
            return RedirectToAction("Index");
        }

        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();
        ViewBag.Cinemas = outlets;
        ViewBag.Categories = new List<string> {
            "Ala cart", "Drinks", "Merchandise", "Snack"
        };

        return View(vm);
    }

    // GET: Product/Update
    public IActionResult Update(string? id)
    {
        var p = db.Products.Find(id);
        if (p == null) return RedirectToAction("Index");

        var vm = new ProductUpdateVM
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            Region = p.Region,
            Cinema = p.Cinema,
            Category = p.Category,
            PhotoURL = p.PhotoURL
        };

        var outlets = GetOutlets();

        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();
        ViewBag.Cinemas = outlets;
        ViewBag.Categories = new List<string> {
            "Ala cart", "Drinks", "Merchandise", "Snack"
        };

        return View(vm);
    }

    // POST: Product/Update
    [HttpPost]
    public IActionResult Update(ProductUpdateVM vm)
    {
        var p = db.Products.Find(vm.Id);
        if (p == null) return RedirectToAction("Index");

        if (vm.Photo != null)
        {
            var e = hp.ValidatePhoto(vm.Photo);
            if (e != "") ModelState.AddModelError("Photo", e);
        }

        var outlets = GetOutlets();

        if (!outlets.Any(o => o.Name == vm.Cinema && o.City == vm.Region))
            ModelState.AddModelError("Cinema", "Selected cinema does not match the selected region.");

        if (ModelState.IsValid)
        {
            p.Name = vm.Name;
            p.Description = vm.Description;
            p.Price = vm.Price;
            p.Stock = vm.Stock;
            p.Region = vm.Region;
            p.Cinema = vm.Cinema;
            p.Category = vm.Category;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(p.PhotoURL, "products");
                p.PhotoURL = hp.SavePhoto(vm.Photo, "products");
            }

            db.SaveChanges();
            TempData["Info"] = "Product updated.";
            return RedirectToAction("Index");
        }

        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();
        ViewBag.Cinemas = outlets;
        ViewBag.Categories = new List<string> {
            "Ala cart", "Drinks", "Merchandise", "Snack"
        };

        return View(vm);
    }

    // POST: Product/Delete
    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var p = db.Products.Find(id);

        if (p != null)
        {
            hp.DeletePhoto(p.PhotoURL, "products");
            db.Products.Remove(p);
            db.SaveChanges();

            TempData["Info"] = "Product deleted.";
        }

        return RedirectToAction("Index");
    }

    // AJAX: Get cinemas for selected region
    public JsonResult GetCinemas(string region)
    {
        if (string.IsNullOrEmpty(region))
            return Json(new List<dynamic>());

        var cinemas = db.Outlets
                        .Where(o => o.City.Trim().ToLower() == region.Trim().ToLower())
                        .Select(o => new { name = o.Name.Trim() }) // lowercase
                        .ToList();

        return Json(cinemas);
    }

}
