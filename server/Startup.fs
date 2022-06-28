module Startup

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Fable.Remoting.AspNetCore
open ServerApi
open Fable.Remoting.Server
open Microsoft.AspNetCore.Http
open Shared


type Startup private () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member this.AddRemoting(app: IApplicationBuilder) =
        
        let impl =
            Remoting.createApi()
            |> Remoting.fromContext (fun (ctx: HttpContext) -> ctx.GetService<ServerApi>().Build())
            |> Remoting.withRouteBuilder routerPaths
        
        app.UseRemoting(impl)
        
    // This method gets called by the runtime. Use this method to add services to the container.
    member this.ConfigureServices(services: IServiceCollection) =
        // To add AWS services to the ASP.NET Core dependency injection add
        // the AWSSDK.Extensions.NETCore.Setup NuGet package. Then
        // use the "AddAWSService" method to add AWS service clients.
        // services.AddAWSService<Amazon.S3.IAmazonS3>() |> ignore
        
        services.AddSingleton<ServerApi>() |> ignore

        // // Add framework services.
        // services.AddControllers() |> ignore
        ()

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore

        this.AddRemoting(app)

    member val Configuration : IConfiguration = null with get, set