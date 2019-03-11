namespace Samples.Debug.Common


module AzureWorkArounds =

    let ExceptionWrapper (logError : exn * string -> unit ) run args =
        try
            run args
        with
        | ex ->
            logError (ex, "Caught by Exception Wrapper")
