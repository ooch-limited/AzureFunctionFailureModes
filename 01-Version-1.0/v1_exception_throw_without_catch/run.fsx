

#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging


let Run(message: string, executionContext: ExecutionContext, log: TraceWriter) = 
    let logInfo = log.Info
    let logError ex = log.Error ("Execution Failed", ex, "message")


    executionContext 
    |> FunctionGuid logInfo
    |> ignore

    match message.ToLowerInvariant() with
    | "reboot" ->
        exn "Exception"
        |>! logError
        |> raise 
    | _ ->
        "Do Nothing"
        |> logInfo