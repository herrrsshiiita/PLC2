using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniPM;
using MiniPM.Models;
using MiniPM.DTOs;
using MiniPM.Services;

var builder = WebApplication.CreateBuilder(args);

// JWT settings - for assignment use local secret; in prod use secure secrets.
var jwtKey = builder.Configuration.GetValue<string>("JwtKey") ?? "super_secret_development_key_please_change";
var jwtIssuer = builder.Configuration.GetValue<string>("JwtIssuer") ?? "MiniPM";

// Add services
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=minipm.db"));

builder.Services.AddScoped<AuthHelper>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

// ---------- Auth Endpoints ----------
app.MapPost("/api/v1/auth/register", async (RegisterDto dto, AppDbContext db, AuthHelper auth) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3) return Results.BadRequest(new { error = "Username must be 3+ chars" });
    if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6) return Results.BadRequest(new { error = "Password must be 6+ chars" });

    var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
    if (existing != null) return Results.Conflict(new { error = "Username already exists" });

    var user = new User { Username = dto.Username };
    user.PasswordHash = auth.HashPassword(dto.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/users/{user.Id}", new { user.Id, user.Username });
});

app.MapPost("/api/v1/auth/login", async (LoginDto dto, AppDbContext db, AuthHelper auth) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
    if (user == null) return Results.Unauthorized();

    if (!auth.VerifyPassword(dto.Password, user.PasswordHash)) return Results.Unauthorized();

    var token = auth.GenerateJwtToken(user, jwtKey, jwtIssuer);
    return Results.Ok(new { token });
});

// ---------- Projects ----------
app.MapGet("/api/v1/projects", async (AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier();
    if (userId == null) return Results.Unauthorized();
    var projects = await db.Projects
        .Where(p => p.UserId == userId)
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new ProjectDto(p.Id, p.Title, p.Description, p.CreatedAt))
        .ToListAsync();
    return Results.Ok(projects);
}).RequireAuthorization();

app.MapPost("/api/v1/projects", async (CreateProjectDto dto, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier();
    if (userId == null) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3 || dto.Title.Length > 100)
        return Results.BadRequest(new { error = "Title must be 3-100 chars" });
    if (dto.Description?.Length > 500) return Results.BadRequest(new { error = "Description max 500 chars" });

    var project = new Project
    {
        Title = dto.Title,
        Description = dto.Description,
        CreatedAt = DateTime.UtcNow,
        UserId = userId.Value
    };
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/projects/{project.Id}", new ProjectDto(project.Id, project.Title, project.Description, project.CreatedAt));
}).RequireAuthorization();

app.MapGet("/api/v1/projects/{id:int}", async (int id, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier();
    if (userId == null) return Results.Unauthorized();
    var p = await db.Projects.Include(x => x.Tasks).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    if (p == null) return Results.NotFound();
    var dto = new ProjectWithTasksDto(p.Id, p.Title, p.Description, p.CreatedAt, p.Tasks.Select(t => new TaskDto(t.Id, t.Title, t.DueDate, t.IsCompleted, t.CreatedAt)));
    return Results.Ok(dto);
}).RequireAuthorization();

app.MapDelete("/api/v1/projects/{id:int}", async (int id, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier();
    if (userId == null) return Results.Unauthorized();
    var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    if (p == null) return Results.NotFound();
    db.Projects.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// ---------- Tasks ----------
app.MapPost("/api/v1/projects/{projectId:int}/tasks", async (int projectId, CreateTaskDto dto, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier(); if (userId == null) return Results.Unauthorized();
    var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
    if (project == null) return Results.NotFound(new { error = "Project not found" });

    if (string.IsNullOrWhiteSpace(dto.Title)) return Results.BadRequest(new { error = "Title required" });

    var t = new ProjectTask
    {
        Title = dto.Title,
        DueDate = dto.DueDate,
        IsCompleted = false,
        CreatedAt = DateTime.UtcNow,
        ProjectId = projectId
    };
    db.Tasks.Add(t);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/projects/{projectId}/tasks/{t.Id}", new TaskDto(t.Id, t.Title, t.DueDate, t.IsCompleted, t.CreatedAt));
}).RequireAuthorization();

app.MapPut("/api/v1/tasks/{taskId:int}", async (int taskId, UpdateTaskDto dto, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier(); if (userId == null) return Results.Unauthorized();
    var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId && x.Project.UserId == userId);
    if (t == null) return Results.NotFound();
    if (!string.IsNullOrWhiteSpace(dto.Title)) t.Title = dto.Title;
    if (dto.DueDate.HasValue) t.DueDate = dto.DueDate;
    if (dto.IsCompleted.HasValue) t.IsCompleted = dto.IsCompleted.Value;
    await db.SaveChangesAsync();
    return Results.Ok(new TaskDto(t.Id, t.Title, t.DueDate, t.IsCompleted, t.CreatedAt));
}).RequireAuthorization();

app.MapDelete("/api/v1/tasks/{taskId:int}", async (int taskId, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier(); if (userId == null) return Results.Unauthorized();
    var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId && x.Project.UserId == userId);
    if (t == null) return Results.NotFound();
    db.Tasks.Remove(t);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPut("/api/v1/tasks/{taskId:int}/toggle", async (int taskId, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier(); if (userId == null) return Results.Unauthorized();
    var t = await db.Tasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == taskId && x.Project.UserId == userId);
    if (t == null) return Results.NotFound();
    t.IsCompleted = !t.IsCompleted;
    await db.SaveChangesAsync();
    return Results.Ok(new TaskDto(t.Id, t.Title, t.DueDate, t.IsCompleted, t.CreatedAt));
}).RequireAuthorization();

// ---------- Smart Scheduler (basic implementation) ----------
app.MapPost("/api/v1/projects/{projectId:int}/schedule", async (int projectId, ScheduleRequest req, AppDbContext db, HttpContext ctx) =>
{
    var userId = ctx.UserIdentifier(); if (userId == null) return Results.Unauthorized();
    var p = await db.Projects.Include(x => x.Tasks).FirstOrDefaultAsync(x => x.Id == projectId && x.UserId == userId);
    if (p == null) return Results.NotFound();
    // Very simple scheduler: list incomplete tasks and assign them sequential dates starting from input startDate or today.
    var start = req.StartDate ?? DateTime.UtcNow.Date;
    var incomplete = p.Tasks.Where(t => !t.IsCompleted).OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();

    var schedule = new List<ScheduledTaskDto>();
    var day = start;
    foreach (var t in incomplete)
    {
        schedule.Add(new ScheduledTaskDto(t.Id, t.Title, day));
        // increment day based on estimatedHours or default 1
        day = day.AddDays(req.DaysPerTask > 0 ? req.DaysPerTask : 1);
    }
    return Results.Ok(new { projectId = p.Id, schedule });
}).RequireAuthorization();

app.Run();

// Helper: extract user id from claims
static class HttpContextExtensions
{
    public static int? UserIdentifier(this HttpContext ctx)
    {
        if (ctx.User?.Identity?.IsAuthenticated != true) return null;
        var idClaim = ctx.User.FindFirst("id")?.Value;
        if (int.TryParse(idClaim, out var id)) return id;
        return null;
    }
}
