using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();
string connection = "Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=NaGorshkeSiditKoro1!";
builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connection));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/records", async (ApplicationContext db)=> await db.Records.ToListAsync());

app.MapGet("/api/records/{id:int}", async (int id, ApplicationContext db) =>
{
    Record? rec = await db.Records.FirstOrDefaultAsync(rec => rec.Id == id);
    if (rec == null)
        return Results.NotFound(new { message = "Запись не найдена" });
    return Results.Json(rec);
});
app.MapPost("/api/records", async (Record rec, ApplicationContext db) =>
{
    await db.Records.AddAsync(rec);
    await db.SaveChangesAsync();
    return rec;
});

app.MapDelete("/api/records/{id:int}", async  (int id, ApplicationContext db) =>
{
    Record? rec = await db.Records.FirstOrDefaultAsync(r=>r.Id == id);
    if (rec == null)
        return Results.NotFound(new { message = "Запись не найдена" });
    db.Records.Remove(rec);
    await db.SaveChangesAsync();
    return Results.Json(rec);
});

app.MapPut("/api/records", async (Record record, ApplicationContext db) =>
{
    Record? rec = await db.Records.FirstOrDefaultAsync(r => r.Id == record.Id);
    if (rec == null)
        return Results.NotFound(new { message = "Запись не найдена" });
    rec.Name = record.Name;
    rec.Text = record.Text;
    await db.SaveChangesAsync();
    return Results.Json(rec);
});


app.Run();
