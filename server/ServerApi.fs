module ServerApi

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Shared

/// An implementation of the Shared IServerApi protocol.
/// Can require ASP.NET injected dependencies in the constructor and uses the Build() function to return value of `IServerApi`.
type ServerApi(logger: ILogger<ServerApi>, config: IConfiguration) =
    member this.Counter() =
        async {
            logger.LogInformation("Executing {Function}", "counter")
            let ret: Counter = { value = 10 }
            return ret
        }

    member this.Print(toPrint: ToPrint) =
        async {
            logger.LogInformation("Executing {Function}", "print")
            logger.LogInformation(sprintf "%s" toPrint.value)
            let ret: PrintResult = { ok = true }
            return ret
        }

    member this.Build() : IServerApi =
        {
            Counter = this.Counter
            Print = this.Print
        }