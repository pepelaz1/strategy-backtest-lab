using Backtest;
if(args.Contains("--self-test")){Tests.Run();return;}
var builder=WebApplication.CreateBuilder(args);var port=int.Parse(Environment.GetEnvironmentVariable("PORT")??"8765");builder.WebHost.UseUrls($"http://127.0.0.1:{port}");builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=1000000);
var app=builder.Build();app.Use(async(context,next)=>{var host=context.Request.Host.ToString();if(host!=$"127.0.0.1:{port}"&&host!=$"localhost:{port}"){context.Response.StatusCode=403;return;}var origin=context.Request.Headers.Origin.ToString();if(context.Request.Method=="POST"&&origin.Length>0&&origin!=$"http://{host}"){context.Response.StatusCode=403;return;}context.Response.Headers["X-Content-Type-Options"]="nosniff";await next();});
app.MapGet("/",()=>Results.Text(File.ReadAllText(Path.Combine(app.Environment.ContentRootPath,"index.html")),"text/html"));
app.MapGet("/api/state",()=>Results.Json(new {bars=Engine.Sample(),report=Engine.Run(Engine.Sample(),new Settings())}));
app.MapPost("/api/run",(RunRequest request)=>{try{return Results.Json(Engine.Run(request.Bars??Engine.Sample(),request.Settings??new Settings()));}catch(ArgumentException e){return Results.BadRequest(new {error=e.Message});}});
app.Run();
record RunRequest(List<Bar>? Bars,Settings? Settings);
