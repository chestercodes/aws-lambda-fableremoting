namespace Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Startup

module LocalEntryPoint =
    let exitCode = 0

    let CreateWebHostBuilder args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Startup>()

    [<EntryPoint>]
    let main args =
        CreateWebHostBuilder(args).Build().Run()
        exitCode
