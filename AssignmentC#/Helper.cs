using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace AssignmentC_;

// TODO
public class Helper(IWebHostEnvironment en, IHttpContextAccessor ct)
{
    // ------------------------------------------------------------------------
    // Photo Upload
    // ------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot more than 1MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
         using var img = SixLabors.ImageSharp.Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    // TODO
    private readonly PasswordHasher<object> ph = new();

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

    public string SaveImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return null;

        // Ensure the path to the wwwroot/img folder exists (we'll assume the path is handled by the injected environment)
        // We'll use the WebRootPath (which points to wwwroot)
        var uploadsFolder = Path.Combine(en.WebRootPath, "img");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate a unique file name to prevent conflicts
        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Save the file to the server's disk
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(fileStream);
        }

        // Return the path that the browser can access
        return "/img/" + uniqueFileName;
    }

    // Deletes an image from the server given its URL path
    public void DeleteImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        // Get the full physical path on the server
        // We remove the leading '/' to correctly combine the path
        string physicalPath = Path.Combine(en.WebRootPath, imageUrl.TrimStart('/'));

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }
}
