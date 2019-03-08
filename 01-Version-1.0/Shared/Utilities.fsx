namespace Samples.Debug.Common

#load "..\Shared\Common\Operators.fsx"
open Samples.Debug.Common.Operators

#load "..\Shared\Common\Logging.fsx"
open Samples.Debug.Common.Logging


#r "System.Net.Http"


#r "Microsoft.Azure.WebJobs.Extensions"
open Microsoft.Azure.WebJobs

#r "Microsoft.Azure.WebJobs.Host"
open Microsoft.Azure.WebJobs.Host


open System
open System.Collections.Specialized
open System.Linq
open System.Net
open System.Net.Http
open System.Collections.Generic

#r "Newtonsoft.Json"
open Newtonsoft.Json

module Utilities =

    let ProcessOutput(logInfo: string -> unit) (event) =
        match event with 
        | Ok x -> 
            x 
            |> JsonConvert.SerializeObject
            |>! LogPrefixInformation logInfo "OutputValue= "
            |> Some

        | Error y -> 
            y |> logInfo
            "No Message" |> logInfo
            None

    let outputMessageOption (logInfo: string -> unit)(output: byref<string>)(x: string option) =
        match x with
        | Some msg -> 
            output <- msg |>! logInfo
        | _ -> 
            "No Message Returned" |> logInfo

    let SafeDecode<'T> (logInfo: string -> unit)(message: string): Result<'T, string> =
        match message |> System.String.IsNullOrWhiteSpace with
        | true -> 
            "Empty Message Received" 
            |>! logInfo 
            |> Error
        | false ->   
            try
                message 
                |>! logInfo
                |> JsonConvert.DeserializeObject<'T>
                |> Ok
            with
            | :? Newtonsoft.Json.JsonReaderException -> "Bad Message" |> Error

    let ReadMessage<'T> (logInfo: string -> unit) (json: string): 'T option = 
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


    let ToObjOption (item) =
        match item with
        | Some x -> x :> obj |> Some
        | _ -> None

    let MakeResponse<'T>(req: HttpRequestMessage)(item: 'T option) = 
        match item with
        | Some x -> (HttpStatusCode.OK, x:> obj)
        | _ -> (HttpStatusCode.BadRequest, null)
        |> req.CreateResponse

    let now() = System.DateTime.UtcNow.ToString "o"

    let guidStr() = Guid.NewGuid().ToString()

    let ValueOrDefault (defval: string) (key: string) (values: Dictionary<string, string>): string =
       match values.TryGetValue(key) with
       | true, v -> v
       | _ -> defval

    let ValueOrDefaultMap (defval: string) (key: string) (values: Map<string, string>): string =
       match values.TryFind(key) with
       | Some v -> v
       | _ -> defval

    let ValueOrDefaultMapAry (defval: string) (key: string) (values: Map<string, string[]>): string =
       match values.TryFind(key) with
       | Some v -> v.First()
       | _ -> defval

    let ValueOrUnknown x y = ValueOrDefault "<unknown>" x y 
    let ValueOrNow x y = ValueOrDefault (now()) x y 
    let ValueOrEmpty x y = ValueOrDefault String.Empty x y 

    let ValueOrUnknownMap x y = ValueOrDefaultMap "<unknown>" x y 
    let ValueOrNowMap x y = ValueOrDefaultMap (now()) x y 
    let ValueOrEmptyMap x y = ValueOrDefaultMap String.Empty x y 

    let ValueOrUnknownMapAry x y = ValueOrDefaultMapAry "<unknown>" x y 
    let ValueOrNowMapAry x y = ValueOrDefaultMapAry (now()) x y 
    let ValueOrEmptyMapAry x y = ValueOrDefaultMapAry String.Empty x y 

    let DeepClone<'T> (input): 'T = 
        input
        |> JsonConvert.SerializeObject
        |> JsonConvert.DeserializeObject<'T>


    let GetPostBody (req:HttpRequestMessage) =
        async {
            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
            return data 
        } |> Async.RunSynchronously

    let ReadPostBodyAsDictionary (logInfo: string -> unit)(req:HttpRequestMessage): Map<string, string> =
        match 
            GetPostBody req
            |> ReadMessage<Map<string, string>> logInfo with
        | Some x -> x
        | _ -> Map.empty

    let PutInto<'T> (output :byref<'T>)(value) = output <- value

    let mapOfNameValueCollection (collection : NameValueCollection) =
        (Map.empty, collection.AllKeys)
        ||> Array.fold (fun map key ->
            let value = collection.[key]
            Map.add key value map)
    
    let GetQueryNameValuePairs(req:HttpRequestMessage) = req.RequestUri.ParseQueryString() |> mapOfNameValueCollection
        
    let GetValue (key: string)(req:HttpRequestMessage)(logInfo: string -> unit) =
        let getPostParam (key: string)(req:HttpRequestMessage)(logInfo: string -> unit)=
            match GetPostBody req
                  |> ReadMessage<Map<string, string>> logInfo with
            | Some items -> 
                items |> Map.tryFind key // (fun q -> q.Key = key)
            | _ -> None
    
        match req 
              |> GetQueryNameValuePairs
              |> Map.tryFind key with
        | Some x -> Some x
        | None -> getPostParam key req logInfo

    let MapSubset keyList map =
        keyList
        |> List.choose (fun k -> Option.map (fun v -> k,v) (Map.tryFind k map))
        |> Map.ofList

    let subset map keyList = MapSubset keyList map

    let subsetKeys (seq: seq<string>) (keyList: string list) =
        seq
        |> Seq.filter (fun x -> (List.tryFind  (fun y-> x = y) keyList) = None)

    let MapKeys(map) = 
        map 
        |> Map.toSeq 
        |> Seq.map fst 
        |> Seq.toList

    let MapValues(map) = 
        map 
        |> Map.toSeq 
        |> Seq.map snd 
        |> Seq.toList

    let MapWithoutValue (values)(map) =
        map
        |> Map.filter (fun _ x -> not (List.contains x values))

    let MapWithout (keys)(map) =
        map
        |> Map.filter (fun x _ -> not (List.contains x keys))

    let MapWith (key)(value)(map) =
        map
        |> Map.add key value

    let MapWithOption key value map =
        match map |> Map.tryFind key with
        | None -> map |> MapWith key value
        | _ -> map

    let MapAppend (mOld: Map<'a, 'b>) (mNew: Map<'a, 'b>) =
        mNew |> Seq.fold (fun m (KeyValue(k, v)) -> Map.add k v m) mOld

    let MapMerge(mOld)(mNew) =
        mOld
        |> MapWithout (mNew |> MapKeys)
        |> MapAppend <| mNew

    let MapRemove (key) = MapWithout [key]

    let MapReplace (key)(value)(map) =
        map
        |> MapWithout [key]
        |> MapWith key value

    let MapRename (oldKey)(newKey)(map) =
        match map |> Map.tryFind oldKey with
        | Some value ->
            map
            |> MapWith newKey value
            |> MapRemove oldKey
        | _ -> map


    let isEmptyString x = System.String.IsNullOrWhiteSpace x
    let isNotEmptyString x = not (isEmptyString x)
    let right l x =
        if isEmptyString x 
        then String.Empty
        else
            if x.Length > l
            then x.Substring (x.Length - l, l)
            else x

    let DefaultTo(defVal: string)(value: string) =
        match value |> isNotEmptyString with
        | true -> value
        | _ -> defVal

    let nonEmpties (map: Map<string,string>): Map<string,string>  =
        map
        |> Map.filter (fun _ v -> isNotEmptyString v)

    let nonEmptiesSeq (map: Map<string,string[]>):Map<string,string[]>  =
        map
        |> Map.filter (fun _ v -> (v.Length > 0) && (isNotEmptyString v.[0]))

    let extractKnownTerms (terms:Map<string, 'T>) =
        terms |> subset <| ["PhoneNumber"; "Email"; "Name"]

    let extractNotificationValues (terms:Map<string, 'T>) =
        terms |> subset <| ["cli"; "conversationid"; "direction"]

    let extractUnKnownTerms (m1:Map<string, 'T>) (keyList: seq<string>) =
         List.fold (fun mapPrev key -> Map.remove key mapPrev) m1 (Seq.toList keyList)

    let getNewSearchTerms ((search, result): Search * SearchResult) =
        let searchTerms = search.Terms |> extractKnownTerms
        let searchKeys = searchTerms |> Map.toSeq |> Seq.map fst
        result.Meta
        |> extractKnownTerms
        |> nonEmptiesSeq
        |> extractUnKnownTerms <| searchKeys

    let appendToCache (m1: Map<string,string>) (m2: Map<string,string>) =
        m1 |> Seq.fold (fun m (KeyValue(k, v)) -> Map.add k v m) m2

    let appendToCacheSeq (m1: Map<string,string[]>) (m2: Map<string,string[]>) =
        m1 |> Seq.fold (fun m (KeyValue(k, v)) -> Map.add k v m) m2

    let takeFirst  (m1: Map<string, string[]>): Map<string, string> =
        m1 |> Seq.fold (fun m (KeyValue(k, v)) -> Map.add k v.[0] m)  Map.empty

    let SetOutput (outputMessage) (output: byref<string>) (logInfo: string -> unit) =
        match outputMessage with
        | Some x -> 
            output <- x |> JsonConvert.SerializeObject
            logInfo(sprintf "Message Sent: %s" output)
        | _ -> 
            logInfo("No Message Sent")

    let When (pred: ('T -> bool))(backup: 'T)(newVal: 'T): 'T = if newVal |> pred then newVal else backup 

    let UnlessEmpty (backup: string) = When isNotEmptyString backup

    let UnlessNull (backup: Nullable<'T>) = When (fun (x:Nullable<'T>) -> x.HasValue) backup

    let UnlessMinDate (backup: DateTime) = When (fun (x:DateTime) -> x <> DateTime.MinValue) backup
