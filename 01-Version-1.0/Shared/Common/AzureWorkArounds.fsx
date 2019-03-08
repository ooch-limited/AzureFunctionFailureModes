namespace Samples.Debug.Common


module AzureWorkArounds =

    let ExceptionWrapper (logError : exn-> unit ) run args =
        try
            run args
        with
        | ex ->
            logError ex
