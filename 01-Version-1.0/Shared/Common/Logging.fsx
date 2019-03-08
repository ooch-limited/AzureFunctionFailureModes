namespace Samples.Debug.Common

#load "Operators.fsx"
open Samples.Debug.Common.Operators

open System

#r "Microsoft.Azure.WebJobs.Extensions"
open Microsoft.Azure.WebJobs

#r "Microsoft.Azure.WebJobs.Host"
open Microsoft.Azure.WebJobs.Host

#r "Newtonsoft.Json"
open Newtonsoft.Json


module Logging =

    let LogAsObject (logInfo: string -> unit)(document) =
        document 
        |> JsonConvert.SerializeObject
        |> logInfo

    let LogPrefixInformation (logInfo: string -> unit) (prefix) (message) = 
        message
        |> sprintf "%s %s" prefix 
        |> logInfo

    let getEnvSetting key = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process)

    let Slot(logInfo: string -> unit) =
       let functionName = "APPSETTING_WEBSITE_SITE_NAME" |> getEnvSetting 
       let slotName = "APPSETTING_WEBSITE_SLOT_NAME" |> getEnvSetting 
       "FUNCTION: " + functionName + " on SLOT: " + slotName |> logInfo

    let FunctionGuid(logInfo: string -> unit)(context: ExecutionContext) =
        Slot logInfo

        context.InvocationId.ToString()
        |>! logInfo
