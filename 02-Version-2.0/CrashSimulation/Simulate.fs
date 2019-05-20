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
    open System.Threading.Tasks

    type MyType = {name: string}

    [<FunctionName("v2_message_emitter_http")>]
    let v2MessageEmitterHttp 
        ( [<HttpTrigger(
            AuthorizationLevel.Function,
            "get",
            Route = "trigger/{message}")>]
          req : HttpRequestMessage, 
          message : string,
          [<ServiceBus ("debug.bus", ServiceBus.EntityType.Topic, Connection = "debug.bus.pub")>]
          output : IAsyncCollector<string>,
          executionContext : ExecutionContext, log : ILogger) =

        let logInfo = log.LogInformation

        executionContext 
        |> FunctionGuid logInfo
        |> ignore

        match message with
        | message when not (String.IsNullOrEmpty (message)) ->
            message
            |> logInfo

            output.AddAsync message
            |> Async.AwaitTask
            |> Async.Start

            (HttpStatusCode.OK, message)
            |> req.CreateResponse 
        | _ ->
            "No message"
            |>! logInfo
            |>+ HttpStatusCode.BadGateway
            |> req.CreateResponse 




    [<FunctionName("v2_message_emitter")>]
    [< NoAutomaticTrigger() >]
    let v2MessageEmitter 
        ( message : string, 
          [<ServiceBus ("debug.bus", ServiceBus.EntityType.Topic, Connection = "debug.bus.pub")>]
          output : IAsyncCollector<string>,
          executionContext : ExecutionContext, log : ILogger) =

        let logInfo = log.LogInformation

        executionContext 
        |> FunctionGuid logInfo
        |> ignore

        message
        |> logInfo

        output.AddAsync message

    [<FunctionName("v2_exception_throw_without_catch")>]
    let v2ExceptionThrowNoCatch 
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
    let v2ExceptionThrowWithCatch
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
            |> ExceptionWrapper logError v2ExceptionThrowNoCatch
        | _ ->
            "Do Nothing"
            |> logInfo
    