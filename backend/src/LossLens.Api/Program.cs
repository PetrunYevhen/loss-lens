using DotNetEnv;
using LossLens.Api;

// Must run before the builder is created so the configuration sees .env values.
// NoClobber: a variable that is already set (Docker, CI, the endpoint tests) wins over the .env file.
Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);

app.Run();

/// <summary>Makes the entry point visible to WebApplicationFactory in the endpoint tests.</summary>
public partial class Program;
