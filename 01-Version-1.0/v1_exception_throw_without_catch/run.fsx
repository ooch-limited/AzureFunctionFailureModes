

#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging

open Newtonsoft.Json

type MyType = {name: string}

let Run(message: string, executionContext: ExecutionContext, log: TraceWriter) = 
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