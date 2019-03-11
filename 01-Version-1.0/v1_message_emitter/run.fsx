

#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging

let Run ( message : string, output : byref<string>, executionContext : ExecutionContext, log : TraceWriter) =
    let logInfo = log.Info

    executionContext 
    |> FunctionGuid logInfo
    |> ignore

    message
    |> logInfo

    output <- message
