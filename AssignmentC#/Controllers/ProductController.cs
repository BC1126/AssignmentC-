using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;
using static AssignmentC_.Models.ProductUpdateVM;

namespace AssignmentC_;

public class ProductController(DB db,
                               Helper hp) : Controller
{
    private List<dynamic> GetOutlets()
    {
        return db.Outlets
                 .Select(o => new { Name = o.Name.Trim(), City = o.City.Trim() })
                 .ToList<dynamic>();
    }

    public IActionResult Index(
    string? name,
    string sort = "Stock",
    string dir = "asc",
    int page = 1
)
    {
        var query = db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p =>
                p.Id.Contains(name) ||
                p.Name.Contains(name) ||
                p.Category.Contains(name) ||
                p.Region.Contains(name) ||
                p.Cinema.Contains(name));
        }

        query = (sort, dir) switch
        {
            ("Stock", "asc") => query.OrderBy(p => p.Stock),
            ("Stock", "des") => query.OrderByDescending(p => p.Stock),

            ("Name", "asc") => query.OrderBy(p => p.Name),
            ("Name", "des") => query.OrderByDescending(p => p.Name),

            ("Price", "asc") => query.OrderBy(p => p.Price),
            ("Price", "des") => query.OrderByDescending(p => p.Price),

            ("Description", "asc") => query.OrderBy(p => p.Description),
            ("Description", "des") => query.OrderByDescending(p => p.Description),

            ("Region", "asc") => query.OrderBy(p => p.Region),
            ("Region", "des") => query.OrderByDescending(p => p.Region),

            ("Cinema", "asc") => query.OrderBy(p => p.Cinema),
            ("Cinema", "des") => query.OrderByDescending(p => p.Cinema),

            ("Category", "asc") => query.OrderBy(p => p.Category),
            ("Category", "des") => query.OrderByDescending(p => p.Category),

            ("Id", "asc") => query.OrderBy(p => p.Id),
            ("Id", "des") => query.OrderByDescending(p => p.Id),

            _ => query.OrderBy(p => p.Stock) // default to lowest stock
        };

        var model = query.ToPagedList(page, 5);

        ViewBag.Name = name;
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        if (Request.IsAjax())
            return PartialView("_ProductTable", model);

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

    // GET: Product/UserSelectRegion
    public IActionResult UserSelectRegion()
    {
        var outlets = GetOutlets();
        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();

        return View(new UserSelectCinemaVM());
    }

    // POST: Product/UserSelectRegion
    [HttpPost]
    public IActionResult UserSelectRegion(UserSelectCinemaVM vm)
    {
        var outlets = GetOutlets();
        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();

        if (vm.Date == null)
        {
            ModelState.AddModelError("Date", "Please select a collect date.");
            return View(vm);
        }

        var today = DateTime.Today;
        var maxDate = today.AddDays(3);

        if (vm.Date < today || vm.Date > maxDate)
        {
            ModelState.AddModelError(
                "Date",
                $"Collect date must be between {today:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}."
            );
            return View(vm);
        }

        if (string.IsNullOrEmpty(vm.Region) || string.IsNullOrEmpty(vm.Cinema))
        {
            ModelState.AddModelError("", "Please select region and cinema.");
            return View(vm);
        }

        // ✅ Save to session
        HttpContext.Session.SetString("SelectedRegion", vm.Region);
        HttpContext.Session.SetString("SelectedCinema", vm.Cinema);
        HttpContext.Session.SetString("CollectDate", vm.Date.Value.ToString("yyyy-MM-dd"));

        return RedirectToAction("UserIndex");
    }





    // POST: Product/UpdateCart
    [HttpPost]
    public IActionResult UpdateCart(string productId, int quantity)
    {
        var cart = hp.GetCart();

        if (quantity >= 1 && quantity <= 10)
        {
            cart[productId] = quantity;
        }
        else
        {
            cart.Remove(productId);
        }

        hp.SetCart(cart);

        return Redirect(Request.Headers.Referer.ToString());
    }

    // GET: Product/UserIndex
    public IActionResult UserIndex()
    {
        ViewBag.Cart = hp.GetCart();

        var region = HttpContext.Session.GetString("SelectedRegion");
        var cinema = HttpContext.Session.GetString("SelectedCinema");

        if (region == null || cinema == null)
            return RedirectToAction("UserSelectRegion");

        var products = db.Products
            .Where(p => p.Region == region && p.Cinema == cinema)
            .ToList();

        ViewBag.Region = region;
        ViewBag.Cinema = cinema;
        ViewBag.CollectDate = HttpContext.Session.GetString("CollectDate");

        if (Request.IsAjax())
            return PartialView("_Index", products);

        return View(products);
    }



    // GET: Product/ShoppingCart
    public IActionResult ShoppingCart()
    {
        // TODO
        var cart = hp.GetCart();
        var m = db.Products
          .Where(p => cart.Keys.Contains(p.Id))
          .Select(p => new ProductCartItem
          {
              Product = p,
              Quantity = cart[p.Id],
              Subtotal = p.Price * cart[p.Id],
          })
          .ToList();

        if (Request.IsAjax()) return PartialView("_ShoppingCart", m);

        return View(m);
    }


}
