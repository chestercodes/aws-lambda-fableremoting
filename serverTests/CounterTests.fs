namespace Server.Tests


open Xunit
open Amazon.Lambda.TestUtilities
open Amazon.Lambda.APIGatewayEvents

open System.IO
open Newtonsoft.Json

open Server


module CounterTests =
    [<Fact>]
    let ``Request HTTP Get at /api/Counter``() = async {
        let lambdaFunction = LambdaEntryPoint()
        let requestStr = File.ReadAllText("./SampleRequests/Counter-Get.json")
        let request = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(requestStr)
        let context = TestLambdaContext()
        let! response = lambdaFunction.FunctionHandlerAsync(request, context) |> Async.AwaitTask

        Assert.Equal(200, response.StatusCode)
        Assert.True(response.MultiValueHeaders.ContainsKey("Content-Type"))
        Assert.Equal("application/json; charset=utf-8", response.MultiValueHeaders.Item("Content-Type").[0])
    }

    [<EntryPoint>]
    let main _ = 0
