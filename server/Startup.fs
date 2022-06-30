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
open Amazon.Lambda.APIGatewayEvents
open Amazon.Lambda.AspNetCoreServer
open System

let errorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) = 
    // https://github.com/Zaid-Ajaj/Fable.Remoting/blob/master/documentation/src/error-handling.md
    printfn "Error at %s on method %s - %A" routeInfo.path routeInfo.methodName ex
    Ignore

type LogRequestMiddleware(next: RequestDelegate) = 
    let logRequest =
        let logDebugOrNull = Environment.GetEnvironmentVariable("LOG_REQUEST")
        logDebugOrNull <> null && logDebugOrNull.ToLowerInvariant() = "true"
    
    member this.Invoke (context: HttpContext) =
        if logRequest then
            let r = context.Request
            printfn "%A - %A" r.PathBase r.Path

            try
                let key = AbstractAspNetCoreFunction.LAMBDA_REQUEST_OBJECT
                let lambdaRequest = context.Items[key] :?>  APIGatewayProxyRequest
                if lambdaRequest <> null then
                    printfn "%s" (System.Text.Json.JsonSerializer.Serialize(lambdaRequest))
            with
                | _ -> printfn "Failed"

        let task = next.Invoke(context);
        task

type Startup private () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member this.AddRemoting(app: IApplicationBuilder) =
        
        let impl =
            Remoting.createApi()
            |> Remoting.withErrorHandler errorHandler
            |> Remoting.fromContext (fun (ctx: HttpContext) -> ctx.GetService<ServerApi>().Build())
#if DEBUG
            |> Remoting.withRouteBuilder routerPaths
#else
            // please see comment in shared file for info on why this is necessary
            |> Remoting.withRouteBuilder routerPathsNoApi
#endif        
        
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

        app.UseMiddleware<LogRequestMiddleware>() |> ignore

        this.AddRemoting(app)

    member val Configuration : IConfiguration = null with get, set