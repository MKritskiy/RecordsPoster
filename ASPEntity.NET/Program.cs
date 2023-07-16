using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder();

builder.Services.AddDbContext<ApplicationContext>();
using (ApplicationContext db = new ApplicationContext())
{
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();

    Role adminRole = new Role() { Id = 1, Name = "Admin" };
    Role userRole = new Role() { Id = 2, Name = "User" };

    User tom = new User() { Nickname = "Tom", Password = "12345", Role = "Admin" };
    User tim = new User() { Nickname = "Tim", Password = "11111", Role = "User" };
    User jerry = new User() { Nickname = "Jerry", Password = "123", Role = "User" };
    db.Users.AddRange(tom, tim, jerry);

    Record record = new Record() { Name = "Test Record", Text = "Test Record Text", User=tom };
    db.Records.AddRange(record);
    db.SaveChanges();
}
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


app.UseDefaultFiles();
app.UseStaticFiles();


app.MapGet("/login",  (HttpContext context) =>
{
    Console.WriteLine(context.Request.Method);
    context.Response.StatusCode = 401;
    return Results.Redirect("/login.html");
});

app.MapPost("/login", async (string? returnUrl, HttpContext context, ApplicationContext db) =>
{
    var form = context.Request.Form;

    if (!form.ContainsKey("nickname") || !form.ContainsKey("password"))
        return Results.BadRequest("Nickname или пароль не установлен");
    var nickname = form["nickname"];
    var password = form["password"];
    
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname.ToString() && u.Password == password.ToString());
    if (user is null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Results.Redirect("/login"); 
    }
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await context.SignInAsync(claimsPrincipal);
    return Results.Redirect(returnUrl ?? "/");
});
app.MapGet("/registration", () => Results.Redirect("/registration.html"));
app.MapGet("/api/user/", [Authorize] async (HttpContext context, ApplicationContext db) =>
{
    int id = int.Parse(context.User.FindFirst(ClaimTypes.Name)?.Value??"0");
    User? user = await db.Users.FirstOrDefaultAsync(u=>u.Id==id);
    Console.WriteLine(user?.Nickname??"NULL");
    if (user==null)
        return Results.NotFound();
    return Results.Json(new { user = user });
});
app.MapPost("/api/user/reg", async (string? returnUrl, HttpContext context, ApplicationContext db) =>
{
    var form = context.Request.Form;
    if (!form.ContainsKey("nickname") || !form.ContainsKey("password"))
        return Results.BadRequest("Nickname или пароль не установлен");
    var nickname = form["nickname"].ToString();
    var password = form["password"].ToString();
    User? tmpUser = await db.Users.FirstOrDefaultAsync(u => u.Nickname == nickname);
    if (tmpUser != null)
        return Results.BadRequest(new { message = "ѕользователь с таким именем уже существует" });
    User user = new User() { 
        Nickname = nickname, 
        Password = password, 
        Role = "User" 
    };
    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await context.SignInAsync(claimsPrincipal);
    return Results.Redirect(returnUrl ?? "/");
});

app.MapGet("/api/user/check/${string:name}", async (string name, ApplicationContext db) =>
{
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Nickname == name);
    return Results.Json(new {
        data = user == null
    });
});





app.MapGet("/api/records", async (ApplicationContext db) =>
{
    var records = db.Records.Include(rec => rec!.User!).ToList();
    return records;
});

app.MapGet("/api/records/{id:int}", async (int id, ApplicationContext db) =>
{
    Record? rec = await db.Records.FirstOrDefaultAsync(rec => rec.Id == id);
    if (rec == null)
        return Results.NotFound(new { message = "«апись не найдена" });
    return Results.Json(rec);
});

app.MapPost("/api/records", [Authorize] async (Record rec, ApplicationContext db, HttpContext context) =>
{
    int id = int.Parse(context.User.FindFirst(ClaimTypes.Name)?.Value??"0");

    User? user = await db.Users.FirstOrDefaultAsync(u=>u.Id==id);

    rec.User = user;
    await db.Records.AddAsync(rec);
    await db.SaveChangesAsync();
    return Results.Json(rec);
});

app.MapGet("/api/auth", (HttpContext context) =>
{
    return Results.Json(new { data = context.User.Identity?.IsAuthenticated ?? false });
});

app.MapGet("/api/records/check/{id:int}", async (int id, ApplicationContext db, HttpContext context) =>
{
    string? userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
    if (userRole == "Admin")
        return Results.Json(new { isChecked = true });

    int userId = int.Parse(context.User.FindFirst(ClaimTypes.Name)?.Value??"0");
    User? user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    Record? record = await db.Records.FirstOrDefaultAsync(r => r.Id == id);
    
    if (user == null || record == null || record.User != user)
    {
        return Results.Json(new { isChecked = false });
    }
    return Results.Json(new { isChecked = true });
});
app.MapDelete("/api/records/{id:int}", [Authorize] async  (int id, ApplicationContext db, HttpContext context) =>
{
    Record? rec = await db.Records.FirstOrDefaultAsync(r=>r.Id == id);
    if (rec == null)
        return Results.NotFound(new { message = "«апись не найдена" });
    db.Records.Remove(rec);
    await db.SaveChangesAsync();
    return Results.Json(rec);
});
app.MapPut("/api/records", [Authorize] async (Record record, ApplicationContext db) =>
{
    Console.WriteLine("POOOOOOOOOOOOO");

    Record? rec = await db.Records.Include(r=>r.User).FirstOrDefaultAsync(r => r.Id == record.Id);
    if (rec == null)
        return Results.NotFound(new { message = "«апись не найдена" });
    rec.Name = record.Name;
    rec.Text = record.Text;
    await db.SaveChangesAsync();
    return Results.Json(rec);
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});
app.Run();


