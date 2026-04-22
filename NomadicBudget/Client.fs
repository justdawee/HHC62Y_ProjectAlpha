namespace NomadicBudget

open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open Domain
open Storage
open Charts

[<JavaScript>]
module Client =

    [<SPAEntryPoint>]
    let Main () =
        let state = Var.Create (Storage.load())
        div [] [text "Loading..."]
        |> Doc.RunById "main"
