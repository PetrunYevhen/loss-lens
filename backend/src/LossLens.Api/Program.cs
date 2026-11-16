using DotNetEnv;
using LossLens.Api;

// Must run before the builder is created so the configuration sees .env values.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);

app.Run();