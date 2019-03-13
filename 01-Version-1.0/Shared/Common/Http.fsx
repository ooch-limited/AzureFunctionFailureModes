namespace Samples.Debug.Common


open System

#r "Newtonsoft.Json"
open Newtonsoft.Json

#r "System.Net.Http"
open System.Net
open System.Net.Http


module Http =
    let ReadMessage<'T> (json: string) (logInfo : string -> unit): 'T option = 
        match json |> System.String.IsNullOrWhiteSpace with
        | true -> logInfo("Empty Message Received")
                  None
        | false ->
            try
                json 
                |> JsonConvert.DeserializeObject<'T>
                |> Some
            with
            | ex ->
                logInfo(sprintf "Exception: %s" ex.Message)
                
                logInfo(sprintf "Unrecognised Message Received: **%s**" json)
                None

    let GetPostBody (req:HttpRequestMessage) =
        async {
            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
            return data 
        } |> Async.RunSynchronously

    let GetValue (key: string)(req:HttpRequestMessage)(logInfo : string -> unit) =
        let getPostParam (key: string)(req:HttpRequestMessage)(logInfo : string -> unit)=
            match GetPostBody req
                  |> ReadMessage<Map<string, string>> <| logInfo with
            | Some items -> 
                items |> Map.tryFind key // (fun q -> q.Key = key)
            | _ -> None
    
    
        match req.GetQueryNameValuePairs() 
              |> Seq.tryFind (fun q -> q.Key = key) with
        | Some x -> Some x.Value
        | None -> getPostParam key req logInfo
