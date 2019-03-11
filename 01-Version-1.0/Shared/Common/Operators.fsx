namespace Samples.Debug.Common

module Operators =

    let inline (~~) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit: ^a -> ^b) x)
    let inline (|>!) x f = x |> f |> ignore; x
    let inline (|>+) y x = (x, y)
    let inline (|+>) x y  = (x, y)

    // The definition of Result in FSharp.Core
    [<StructuralEquality; StructuralComparison>]
    [<CompiledName("FSharpResult`2")>]
    [<Struct>]
    type Result<'T,'TError> = 
        | Ok of ResultValue:'T 
        | Error of ErrorValue:'TError

    let bind binder result = match result with Error e -> Error e | Ok x -> binder x

    let inline (>>=) input func = bind func input

