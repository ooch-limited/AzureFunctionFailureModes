

#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging

let ExceptionWrapper (logError : exn * string -> unit ) run args =
    try
        run args
    with
    | ex ->
        logError (ex, "Caught by Exception Wrapper")

let run_no_catch  ( message : string, executionContext : ExecutionContext, log : TraceWriter) =
    let logInfo = log.Info

    executionContext 
    |> FunctionGuid logInfo
    |> ignore

    match message.ToLowerInvariant() with
    | "reboot" ->
        "{Exception" |> Error
        |> LogAsObject logInfo
    | _ ->
        "Do Nothing"
        |> logInfo

let Run(message: string, executionContext: ExecutionContext, log: TraceWriter) = 
    let logInfo = log.Info
    let logError (ex, message) = log.Error ("Execution Failed", ex, message)

    executionContext 
    |> FunctionGuid logInfo
    |> ignore

    match message.ToLowerInvariant() with
    | "catch" ->
        "Raise Exception To Be Caught" |> logInfo

        "Wraps crashing function in protective wrapper" |> logInfo

        ("reboot", executionContext, log)
        |> ExceptionWrapper logError run_no_catch
    | _ ->
        "Do Nothing"
        |> logInfo
