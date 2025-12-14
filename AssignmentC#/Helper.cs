using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.IO; // Required for FileStream and Path operations and Directory.CreateDirectory
using System.Linq;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace AssignmentC_;

// CRITICAL MODIFICATION: Added DB context to the constructor for GenerateNextUserId method.
// NOTE: Make sure your DB context type (DB) is available to this class.
public class Helper(IWebHostEnvironment en, IHttpContextAccessor ct, DB db)
{
    // Private field for PasswordHasher
    private readonly PasswordHasher<object> ph = new();

    // ------------------------------------------------------------------------
    // Photo Upload (FIXED: Size check condition)
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        // Corrected 10MB condition to 2MB to match the error message.
        else if (f.Length > 2 * 1024 * 1024)
        {
            return "Photo size cannot more than 2MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        // 1. Define the folder path within wwwroot
        string uploadsFolder = Path.Combine(en.WebRootPath, folder);

        // 2. CRITICAL FIX: CHECK AND CREATE DIRECTORY
        // If the folder (e.g., wwwroot/photos) does not exist, create it.
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 3. Define the file name and full path
        var fileName = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(uploadsFolder, fileName); // Use uploadsFolder instead of re-combining from 'folder'

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        // 4. Save the file
        using var stream = f.OpenReadStream();
        using var img = SixLabors.ImageSharp.Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path); // This line will no longer throw DirectoryNotFoundException

        // 5. Return the URL path
        // The controller expects a URL path relative to wwwroot, which starts with the folder name
        return $"/{folder}/{fileName}";
    }

    public void DeletePhoto(string file, string folder)
    {
        // FIX: Ensure 'file' is just the file name, not a URL path (e.g. /photos/123.jpg)
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // ------------------------------------------------------------------------
    // User ID Generation (Integrated Database dependency via constructor)
    // ------------------------------------------------------------------------

    // HELPER.CS

    public string GenerateNextUserId(string rolePrefix)
    {
        // 1. Get the last ID for the specified prefix (e.g., "M")
        // NOTE: If db.Users is not the correct collection, change it to db.Members.
        var lastId = db.Users
            .AsEnumerable()
            .Where(u => u.UserId != null && u.UserId.StartsWith(rolePrefix))
            .OrderByDescending(u => u.UserId)
            .Select(u => u.UserId)
            .FirstOrDefault();

        int nextSequence = 1;

        // This block handles incrementing only if a matching ID was found.
        if (lastId != null && lastId.Length > rolePrefix.Length)
        {
            // 2. Extract the numeric part (e.g., "A0005" -> 0005)
            string numericPart = lastId.Substring(rolePrefix.Length);

            // Try to parse the number and increment it
            if (int.TryParse(numericPart, out int currentSequence))
            {
                nextSequence = currentSequence + 1;
            }
            // If parsing fails, nextSequence remains 1.
        }
        // If lastId is null (first member), nextSequence remains 1.

        // 3. Format the new sequence number (e.g., 1 -> "0001")
        string sequenceString = nextSequence.ToString("D4");

        // 4. Combine and return (e.g., "M" + "0001" = "M0001")
        // This line guarantees a non-null string is always returned.
        return rolePrefix + sequenceString;
    }


    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
            == PasswordVerificationResult.Success;
    }

    public void SignIn(string email, string role, bool rememberMe)
    {
        // (1) Claim, identity and principal
        List<Claim> claims =
        [
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        // (2) Remember me (authentication properties)
        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };
        // (3) Sign in
        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        // Sign out
        ct.HttpContext!.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 0; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];
        }

        return password;
    }

    // NOTE: This method duplicates functionality of SavePhoto; ensure you only use one.
    public string SaveImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return null;

        var uploadsFolder = Path.Combine(en.WebRootPath, "img");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(fileStream);
        }

        return "/img/" + uniqueFileName;
    }

    // NOTE: This method duplicates functionality of SavePhoto; ensure you only use one.
    public void DeleteImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        string physicalPath = Path.Combine(en.WebRootPath, imageUrl.TrimStart('/'));

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    // NOTE: This method duplicates functionality of SavePhoto; ensure you only use one.
    public string SavePhotoNoResize(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            return null;

        // CRITICAL FIX FOR DirectoryNotFoundException
        string uploadsFolder = Path.Combine(en.WebRootPath, folder);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = Guid.NewGuid().ToString("n") + Path.GetExtension(file.FileName);
        var path = Path.Combine(uploadsFolder, fileName); // Use uploadsFolder

        using (var stream = new FileStream(path, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return fileName;
    }

    // ------------------------------------------------------------------------
    // Shopping Cart Helper Functions
    // ------------------------------------------------------------------------

    public Dictionary<string, int> GetCart()
    {
        // TODO
        return ct.HttpContext!.Session.Get<Dictionary<string, int>>("Cart") ?? [];
    }

    public void SetCart(Dictionary<string, int>? dict = null)
    {
        if (dict == null)
        {
            // TODO
            ct.HttpContext!.Session.Remove("Cart");
        }
        else
        {
            // TODO
            ct.HttpContext!.Session.Set("Cart", dict);
        }
    }


}
