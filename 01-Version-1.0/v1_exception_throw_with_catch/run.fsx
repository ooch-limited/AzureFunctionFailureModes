

#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging

let ExceptionWrapper (logError : exn-> unit ) run args =
    try
        run args
    with
    | ex ->
        logError ex


let Run(message: string, executionContext: ExecutionContext, log: TraceWriter) = 
    let logInfo = log.Info
    let logError ex = log.Error ("Execution Failed", ex, "message")

    executionContext 
    |> FunctionGuid logInfo
    |> ignore

    match message.ToLowerInvariant() with
    | "catch" ->
        let fail() =
            exn "Exception To Be Caught"
            |>! logError
            |> raise

        ExceptionWrapper logError fail () 
    | _ ->
        "Do Nothing"
        |> logInfo