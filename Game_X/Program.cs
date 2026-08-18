var builder = WebApplication.CreateBuilder(args);

// Configura apenas o suporte a Controllers de API
builder.Services.AddControllers();

// =============================
// Configuração da Session
// =============================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// =============================
// Servir Arquivos Estáticos (index.html, JS, CSS)
// =============================
// 1. Procura por padrão por arquivos como "index.html" na pasta wwwroot
app.UseDefaultFiles();

// 2. Permite o download de arquivos estáticos (.html, .css, .js)
app.UseStaticFiles();

app.UseRouting();

// O UseSession DEVE vir entre UseRouting e UseAuthorization/MapControllers
app.UseSession();

app.UseAuthorization();

// Mapeia as rotas dos seus Controllers (como [Route("Game/State")])
app.MapControllers();

app.Run();