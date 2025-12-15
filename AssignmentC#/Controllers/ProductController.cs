using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using static AssignmentC_.Models.ProductUpdateVM;

namespace AssignmentC_;

public class ProductController(DB db, Helper hp) : Controller
{
    private List<dynamic> GetOutlets()
    {
        return db.Outlets
                 .Select(o => new { Name = o.Name.Trim(), City = o.City.Trim() })
                 .ToList<dynamic>();
    }

    public IActionResult Index(string? name, string sort = "Stock", string dir = "asc", int page = 1)
    {
        if (page < 1) page = 1;

        try
        {
            var query = db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
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

                _ => query.OrderBy(p => p.Stock)
            };

            var model = query.ToPagedList(page, 5);

            ViewBag.Name = name;
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            if (Request.IsAjax())
                return PartialView("_ProductTable", model);

            return View(model);
        }
        catch
        {
            TempData["Error"] = "Failed to load product list.";
            return View();
        }
    }

    public bool CheckId(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return !db.Products.Any(p => p.Id == id);
    }

    private string NextId()
    {
        string max = db.Products.Max(p => p.Id) ?? "P000";
        int n = int.Parse(max[1..]);
        return (n + 1).ToString("'P'000");
    }

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
        ViewBag.Categories = new List<string> { "Ala cart", "Drinks", "Merchandise", "Snack" };

        return View(vm);
    }

    [HttpPost]
    public IActionResult Insert(ProductInsertVM vm)
    {
        try
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
            ViewBag.Categories = new List<string> { "Ala cart", "Drinks", "Merchandise", "Snack" };

            return View(vm);
        }
        catch
        {
            TempData["Error"] = "Insert failed.";
            return View(vm);
        }
    }

    public IActionResult Update(string? id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Index");

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
        ViewBag.Categories = new List<string> { "Ala cart", "Drinks", "Merchandise", "Snack" };

        return View(vm);
    }

    [HttpPost]
    public IActionResult Update(ProductUpdateVM vm)
    {
        var p = db.Products.Find(vm.Id);
        if (p == null) return RedirectToAction("Index");

        try
        {
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
            ViewBag.Categories = new List<string> { "Ala cart", "Drinks", "Merchandise", "Snack" };

            return View(vm);
        }
        catch
        {
            TempData["Error"] = "Update failed.";
            return View(vm);
        }
    }

    [HttpPost]
    public IActionResult Delete(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return RedirectToAction("Index");

        try
        {
            var p = db.Products.Find(id);
            if (p != null)
            {
                hp.DeletePhoto(p.PhotoURL, "products");
                db.Products.Remove(p);
                db.SaveChanges();
                
            }TempData["Info"] = "Product deleted.";
        }
        catch
        {
            TempData["Error"] = "Delete failed.";
        }

        return RedirectToAction("Index");
    }

    public JsonResult GetCinemas(string region)
    {
        if (string.IsNullOrEmpty(region))
            return Json(new List<dynamic>());

        try
        {
            var cinemas = db.Outlets
                .Where(o => o.City.Trim().ToLower() == region.Trim().ToLower())
                .Select(o => new { name = o.Name.Trim() })
                .ToList();

            return Json(cinemas);
        }
        catch
        {
            return Json(new List<dynamic>());
        }
    }

    public IActionResult UserSelectRegion()
    {
        var outlets = GetOutlets();
        ViewBag.Regions = outlets.Select(o => o.City).Distinct().ToList();
        return View(new UserSelectCinemaVM());
    }

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
            ModelState.AddModelError("Date",
                $"Collect date must be between {today:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}.");
            return View(vm);
        }

        if (string.IsNullOrEmpty(vm.Region) || string.IsNullOrEmpty(vm.Cinema))
        {
            ModelState.AddModelError("", "Please select region and cinema.");
            return View(vm);
        }

        HttpContext.Session.SetString("SelectedRegion", vm.Region);
        HttpContext.Session.SetString("SelectedCinema", vm.Cinema);
        HttpContext.Session.SetString("CollectDate", vm.Date.Value.ToString("yyyy-MM-dd"));

        return RedirectToAction("UserIndex");
    }

    [HttpPost]
    public IActionResult UpdateCart(string productId, int quantity)
    {
        try
        {
            var cart = hp.GetCart();

            if (quantity >= 1 && quantity <= 10)
                cart[productId] = quantity;
            else
                cart.Remove(productId);

            hp.SetCart(cart);
        }
        catch { }

        return Redirect(Request.Headers.Referer.ToString());
    }

    public IActionResult UserIndex(string? category)
    {
        ViewBag.Cart = hp.GetCart();

        var region = HttpContext.Session.GetString("SelectedRegion");
        var cinema = HttpContext.Session.GetString("SelectedCinema");

        if (region == null || cinema == null)
            return RedirectToAction("UserSelectRegion");

        var products = db.Products
            .Where(p => p.Region == region && p.Cinema == cinema)
            .Where(p => string.IsNullOrEmpty(category) || p.Category == category)
            .ToList();

        ViewBag.Region = region;
        ViewBag.Cinema = cinema;
        ViewBag.CollectDate = HttpContext.Session.GetString("CollectDate");

        // ✅ ALWAYS show all 4 categories
        ViewBag.Categories = new List<string>
    {
        "Ala cart",
        "Drinks",
        "Merchandise",
        "Snack"
    };

        ViewBag.SelectedCategory = category;

        if (Request.IsAjax())
            return PartialView("_UserIndex", products);

        return View(products);
    }



    public IActionResult ShoppingCart()
    {
        try
        {
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

            if (Request.IsAjax())
                return PartialView("_ShoppingCart", m);

            return View(m);
        }
        catch
        {
            TempData["Error"] = "Failed to load cart.";
            return View(new List<ProductCartItem>());
        }
    }

    // POST: Product/Checkout
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Checkout()
    {
        // 1. Checking (shoping cart NOT empty)
        var cart = hp.GetCart();
        if (cart.Count() == 0) return RedirectToAction("ShoppingCart");

        // 2. Create [Order] (parent record)
        var order = new Order
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Paid = false,
            MemberEmail = User.Identity!.Name!,
        };
        db.Orders.Add(order);

        // 3. Create [OrderLine] (child record)
        foreach (var (productId, quantity) in cart)
        {
            var p = db.Products.Find(productId);
            if (p == null) continue;

            order.OrderLines.Add(new()
            {
                Price = p.Price,
                Quantity = quantity,
                ProductId = productId,
            });
        }

        // 4. Save changes + clear shopping cart
        // TODO
        db.SaveChanges();
        hp.SetCart();

        // Continue with other processing
        // For example: payment, etc. (Using third party payment such as Stripe, Paypal)

        // TODO
        return RedirectToAction("OrderComplete", new { order.Id });
    }

    public IActionResult OrderComplete(int id)
    {
        ViewBag.Id = id;
        return View();
    }

    // GET: Product/Order
    [Authorize(Roles = "Member")]
    public IActionResult Order()
    {
        //TODO
        var m = db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product)
            .Where(o => o.MemberEmail == User.Identity!.Name)
            .OrderByDescending(o => o.Id);

        return View(m);
    }

    // GET: Product/OrderDetail
    [Authorize(Roles = "Member")]
    public IActionResult OrderDetail(int id)
    {
        // TODO
        var m = db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product)
            .FirstOrDefault(o => o.Id == id &&
            o.MemberEmail == User.Identity!.Name);

        if (m == null) return RedirectToAction("Order");

        return View(m);
    }
}
