using GMC.BL.GMC;
using GMC.BL.GMC.ColumnMatching;
using GMC.DAL;
using GMC.DAL.Repository.GMC;
using GMC.Helper;
using GMC.Interface;
using GMC.Interface.GMC;
using OfficeOpenXml;

// EPPlus 8 requires an explicit licence; set it once at startup.
ExcelPackage.License.SetNonCommercialOrganization("GMC");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionAuthFilter>();
});
builder.Services.AddScoped<IGMCUploader, GMCUploaderBL>();
builder.Services.AddScoped<IGMCUploaderRepo, GMCUploaderRepo>();
builder.Services.AddScoped<GMCUploaderDAL>();
builder.Services.AddScoped<IGMCCalculatorDetails, GMCCalculatorDetailsBL>();
builder.Services.AddScoped<IGMCCalculatorDetailsRepo, GMCCalculatorDetailsRepo>();
builder.Services.AddScoped<GMCCalculatorDetailsDAL>();
builder.Services.AddScoped<ILoginBL, LoginBL>();
builder.Services.AddScoped<ILoginRepo, LoginRepo>();
builder.Services.AddScoped<LoginDAL>();
builder.Services.AddScoped<GMC.Models.GMC.BusinessLogic.IUserRegistrationBL, GMC.Models.GMC.BusinessLogic.UserRegistrationBL>();
builder.Services.AddScoped<IUserRegistrationRepo, UserRegistrationRepo>();
builder.Services.AddScoped<UserRegistrationDAL>();
builder.Services.AddScoped<GMC.DAL.Repository.GMC.MasterDAL>();
builder.Services.AddScoped<ISqlHelperQuery, SqlHelperQuery>();
builder.Services.AddScoped<CommonBAL>();

// --- Sales-Person  →  Underwriter  →  Calculator rollover pipeline -------
builder.Services.AddScoped<IRolloverUploadRepo, RolloverUploadRepo>();
builder.Services.AddScoped<ISalesUploadBL, SalesUploadBL>();
builder.Services.AddScoped<IMappedExcelIngestor, MappedExcelIngestor>();
builder.Services.AddScoped<IUnderwriterBL, UnderwriterBL>();
builder.Services.AddScoped<IDashboardBL, DashboardBL>();
builder.Services.Configure<AiColumnMappingOptions>(
    builder.Configuration.GetSection(AiColumnMappingOptions.SectionName));
builder.Services.AddScoped<IAiColumnMappingService, GeminiColumnMappingService>();
builder.Services.AddScoped<IColumnMatcher, LegacyColumnMatcher>();
builder.Services.AddScoped<IDbDiagnosticsRepo, DbDiagnosticsRepo>();
builder.Services.AddScoped<IDbDiagnosticsBL, DbDiagnosticsBL>();
builder.Services.AddScoped<IMasterListRepo, MasterListRepo>();
builder.Services.AddScoped<IMasterListBL, MasterListBL>();

builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    // Harden the session cookie — applies to both legacy and new pipeline.
    options.Cookie.Name        = "GMC.Session";
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite    = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout        = TimeSpan.FromMinutes(45);
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name     = "GMC.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName      = "RequestVerificationToken";
});
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
app.UsePathBase("/GMC");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Enable Session
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
