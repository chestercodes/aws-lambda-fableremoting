module Shared

/// Defines how routes are generated on server and mapped from client
let routerPaths typeName method = sprintf "/api/%s" method

// this is a massive hack...
// basically as the cloudfront distribution is being used to route the calls to /api/
// it means that by the time is gets to the lambda via api gateway it loses the "/api" part
// and it won't recognise the route and will return a 404
let routerPathsNoApi typeName method = sprintf "/%s" method

type Counter = { value : int }

type ToPrint = { value : string }
type PrintResult = { ok : bool }

/// A type that specifies the communication protocol between client and server
/// to learn more, read the docs at https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type IServerApi = {
    Counter : unit -> Async<Counter>
    Print : ToPrint -> Async<PrintResult>
}