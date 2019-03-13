open System.Net


#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging

#load "..\Shared\Common\Http.fsx"
open Samples.Debug.Common.Http

#r "System.Net.Http"
open System.Net
open System.Net.Http


let Run (req: HttpRequestMessage, output : byref<string>, executionContext : ExecutionContext, log : TraceWriter) =
    let logInfo = log.Info

    executionContext 
    |> FunctionGuid logInfo
    |> ignore

    match GetValue "message" req logInfo with
    | Some message ->

        message
        |> logInfo

        output <- message

        (HttpStatusCode.OK, message)
        |> req.CreateResponse 
    | _ ->
        "No message"
        |>! logInfo
        |>+ HttpStatusCode.BadGateway
        |> req.CreateResponse 
