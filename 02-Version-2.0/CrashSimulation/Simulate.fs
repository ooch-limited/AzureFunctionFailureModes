namespace Samples.Debug.v2.CrashSimulation


module Simulate =
    open Microsoft.Azure.WebJobs
    open Microsoft.Extensions.Logging
    open Samples.Debug.v2.Common
    open Samples.Debug.v2.Common.Logging
    open Samples.Debug.v2.Common.AzureWorkArounds
    open Newtonsoft.Json

    type MyType = {name: string}

    [<FunctionName("v2_exception_throw_without_catch")>]
    let v2_exception_throw_no_catch 
        ( [<ServiceBusTrigger ("debug.bus", "v2.noCatch", Connection = "debug.bus.sub")>]
         message : string, 
         executionContext : ExecutionContext, log : ILogger) =
        let logInfo = log.LogInformation

        executionContext 
        |> FunctionGuid logInfo
        |> ignore

        match message.ToLowerInvariant() with
        | "reboot" ->
            "{Exception"
            |> JsonConvert.DeserializeObject<MyType>

            |> LogAsObject logInfo
        | _ ->
            "Do Nothing"
            |> logInfo

    [<FunctionName("v2_exception_throw_with_catch")>]
    let v2_exception_throw_with_catch
        ( [<ServiceBusTrigger ("debug.bus", "v2.withCatch", Connection = "debug.bus.sub")>]
         message : string,
         executionContext : ExecutionContext,
         log : ILogger) =

        let logInfo = log.LogInformation

        executionContext 
        |> FunctionGuid logInfo
        |> ignore

        let logError (a : exn, b) = log.LogError (a, b)

        match message.ToLowerInvariant() with
        | "catch" ->
            "Raise Exception To Be Caught" |> logInfo

            "Wraps crashing function in protective wrapper" |> logInfo

            ("reboot", executionContext, log)
            |> ExceptionWrapper logError v2_exception_throw_no_catch
        | _ ->
            "Do Nothing"
            |> logInfo
    