namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open Domain

[<JavaScript>]
module Charts =

    let mutable private chart : obj option = None

    [<Inline "new Chart(document.getElementById($id), $config)">]
    let private createChart (id: string) (config: obj) : obj = X<obj>

    [<Inline "$c.data.datasets[0].data = $data; $c.data.labels = $labels; $c.update('none')">]
    let private applyUpdate (c: obj) (labels: string[]) (data: float[]) : unit = ()

    let private makeConfig (profile: BudgetProfile) : obj =
        let labels = allCategories |> List.map categoryName |> Array.ofList
        let data   = allCategories |> List.map (fun c -> float (categoryValue profile c)) |> Array.ofList
        let colors = allCategories |> List.map categoryColor |> Array.ofList
        New [
            "type", box "doughnut"
            "data", box (New [
                "labels", box labels
                "datasets", box [|
                    New [
                        "data",            box data
                        "backgroundColor", box colors
                        "borderWidth",     box 2
                        "borderColor",     box "#0a0a1a"
                    ]
                |]
            ])
            "options", box (New [
                "responsive",          box true
                "maintainAspectRatio", box false
                "plugins", box (New [
                    "legend", box (New [
                        "position", box "bottom"
                        "labels",   box (New [
                            "color",   box "#e0e0e0"
                            "padding", box 15
                        ])
                    ])
                ])
            ])
        ]

    let init (canvasId: string) (profile: BudgetProfile) : unit =
        let c = createChart canvasId (makeConfig profile)
        chart <- Some c

    let update (profile: BudgetProfile) : unit =
        match chart with
        | None -> ()
        | Some c ->
            let labels = allCategories |> List.map categoryName |> Array.ofList
            let data   = allCategories |> List.map (fun cat -> float (categoryValue profile cat)) |> Array.ofList
            applyUpdate c labels data
