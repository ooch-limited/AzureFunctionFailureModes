namespace Samples.Debug.v2.CrashSimulation


module Simulate =
    open Microsoft.Azure.WebJobs
    open Microsoft.Extensions.Logging
    open Samples.Debug.v2.Common.Operators
    open Samples.Debug.v2.Common.Http
    open Samples.Debug.v2.Common.Logging
    open Samples.Debug.v2.Common.AzureWorkArounds
    open System.Net.Http

    open Microsoft.Azure.WebJobs.Extensions.Http
    open System.Net
    open System

    type MyType = {name: string}

    [<FunctionName("v2_message_emitter_http")>]
    let v2_message_emitter_http 
        ( [<HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "trigger/{message}")>]
          req : HttpRequestMessage, 
          message : string,
          [<ServiceBus ("debug.bus", ServiceBus.EntityType.Topic, Connection = "debug.bus.pub")>]
          output : ICollector<string>,
          executionContext : ExecutionContext, log : ILogger) =

        let logInfo = log.LogInformation

        executionContext 
        |> FunctionGuid logInfo
        |> ignore

        match message with
        | message when not (String.IsNullOrEmpty (message)) ->
            message
            |> logInfo

            output.Add message

            (HttpStatusCode.OK, message)
            |> req.CreateResponse 
        | _ ->
            "No message"
            |>! logInfo
            |>+ HttpStatusCode.BadGateway
            |> req.CreateResponse 

    [<FunctionName("v2_message_emitter")>]
    [< NoAutomaticTrigger() >]
    let v2_message_emitter 
        ( message : string, 
          [<ServiceBus ("debug.bus", ServiceBus.EntityType.Topic, Connection = "debug.bus.pub")>]
          output : ICollector<string>,
          executionContext : ExecutionContext, log : ILogger) =

        let logInfo = log.LogInformation

        executionContext 
        |> FunctionGuid logInfo
        |> ignore

        message
        |> logInfo

        output.Add message

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
            //"{Exception"
            //|> JsonConvert.DeserializeObject<MyType>

            //|> LogAsObject logInfo

            failwith "Manual Error"

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
    