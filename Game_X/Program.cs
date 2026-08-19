using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 1. Serviços de Controllers e Views
builder.Services.AddControllersWithViews();

// 2. Serviços de Sessão (necessário para o HttpContext.Session funcionar)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Leitura dinâmica da porta da hospedagem
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

// Configurações para ambiente de Produção
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 4. Configuração para buscar o index.html na RAIZ do projeto
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    RequestPath = ""
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    RequestPath = ""
});

// 5. Mapeamento da pasta /cs externa (para o Game.css)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "cs")),
    RequestPath = "/cs"
});

// 6. Mapeamento da pasta /js externa (para o Game.js)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "js")),
    RequestPath = "/js"
});

app.UseRouting();

// 7. Ativação do Middleware de Sessão (Importante: deve ficar APÓS UseRouting e ANTES dos Controllers)
app.UseSession();

app.UseAuthorization();

// 8. Rota Padrão para os Controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();