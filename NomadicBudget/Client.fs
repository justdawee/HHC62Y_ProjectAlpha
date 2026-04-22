namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open Domain
open Storage
open Charts

[<JavaScript>]
module Client =

    // ── Formatting helpers ──────────────────────────────────────────────

    let private fmt (amount: float<usd>) =
        let n = float amount
        if n >= 0.0 then sprintf "$%.0f" n
        else sprintf "-$%.0f" (abs n)

    // ── Reusable slider field ───────────────────────────────────────────

    let private sliderField
            (lbl    : string)
            (icon   : string)
            (minV   : float)
            (maxV   : float)
            (stepV  : float)
            (get    : BudgetProfile -> float<usd>)
            (setF   : float<usd> -> BudgetProfile -> BudgetProfile)
            (state  : Var<BudgetProfile>) =
        let viewVal = state.View |> View.Map (fun p -> float (get p))
        div [attr.``class`` "field mb-4"] [
            div [attr.``class`` "level is-mobile mb-1"] [
                div [attr.``class`` "level-left"] [
                    label [attr.``class`` "label has-text-light is-small mb-0"] [
                        span [attr.``class`` "icon-text"] [
                            span [attr.``class`` "icon is-small"] [
                                i [attr.``class`` (sprintf "fas %s" icon)] []
                            ]
                            span [] [text lbl]
                        ]
                    ]
                ]
                div [attr.``class`` "level-right"] [
                    span [attr.``class`` "tag is-info"] [
                        textView (viewVal |> View.Map (sprintf "$%.0f"))
                    ]
                ]
            ]
            input [
                attr.``type``  "range"
                attr.``class`` "slider is-fullwidth is-info"
                attr.min       (string minV)
                attr.max       (string maxV)
                attr.step      (string stepV)
                Attr.Dynamic "value" (viewVal |> View.Map string)
                on.input (fun el _ ->
                    let v = float (As<HTMLInputElement>(el)).Value
                    state.Update(fun p -> setF (v * 1.0<usd>) p)
                )
            ] []
        ]

    // ── Income slider (larger, green) ───────────────────────────────────

    let private incomeField (state: Var<BudgetProfile>) =
        let viewVal = state.View |> View.Map (fun p -> float p.MonthlyIncome)
        div [attr.``class`` "field mb-5"] [
            div [attr.``class`` "level is-mobile mb-1"] [
                div [attr.``class`` "level-left"] [
                    label [attr.``class`` "label has-text-light mb-0"] [
                        span [attr.``class`` "icon-text"] [
                            span [attr.``class`` "icon"] [ i [attr.``class`` "fas fa-wallet"] [] ]
                            span [] [text "Monthly Income"]
                        ]
                    ]
                ]
                div [attr.``class`` "level-right"] [
                    span [attr.``class`` "tag is-success is-medium"] [
                        textView (viewVal |> View.Map (sprintf "$%.0f"))
                    ]
                ]
            ]
            input [
                attr.``type``  "range"
                attr.``class`` "slider is-fullwidth is-success is-medium"
                attr.min "500" ; attr.max "20000" ; attr.step "100"
                Attr.Dynamic "value" (viewVal |> View.Map string)
                on.input (fun el _ ->
                    let v = float (As<HTMLInputElement>(el)).Value
                    state.Update(fun p -> { p with MonthlyIncome = v * 1.0<usd> })
                )
            ] []
        ]

    // ── Tax rate slider ─────────────────────────────────────────────────

    let private taxRateField (state: Var<BudgetProfile>) =
        let viewPct = state.View |> View.Map (fun p -> p.TaxRate * 100.0)
        div [attr.``class`` "field mb-4"] [
            div [attr.``class`` "level is-mobile mb-1"] [
                div [attr.``class`` "level-left"] [
                    label [attr.``class`` "label has-text-light is-small mb-0"] [
                        span [attr.``class`` "icon-text"] [
                            span [attr.``class`` "icon is-small"] [ i [attr.``class`` "fas fa-percent"] [] ]
                            span [] [text "Tax Rate"]
                        ]
                    ]
                ]
                div [attr.``class`` "level-right"] [
                    span [attr.``class`` "tag is-warning"] [
                        textView (viewPct |> View.Map (sprintf "%.0f%%"))
                    ]
                ]
            ]
            input [
                attr.``type``  "range"
                attr.``class`` "slider is-fullwidth is-warning"
                attr.min "0" ; attr.max "60" ; attr.step "1"
                Attr.Dynamic "value" (viewPct |> View.Map string)
                on.input (fun el _ ->
                    let v = float (As<HTMLInputElement>(el)).Value / 100.0
                    state.Update(fun p -> { p with TaxRate = v })
                )
            ] []
        ]

    // ── Tax mode selector ───────────────────────────────────────────────

    let private taxModeSelect (state: Var<BudgetProfile>) =
        let modeView = state.View |> View.Map (fun p ->
            match p.TaxMode with
            | FixedRate      -> "0"
            | Progressive    -> "1"
            | SocialSecurity -> "2"
        )
        div [attr.``class`` "field mb-3"] [
            label [attr.``class`` "label has-text-light is-small"] [
                span [attr.``class`` "icon-text"] [
                    span [attr.``class`` "icon is-small"] [ i [attr.``class`` "fas fa-building-columns"] [] ]
                    span [] [text "Tax Mode"]
                ]
            ]
            div [attr.``class`` "control"] [
                div [attr.``class`` "select is-dark is-fullwidth is-small"] [
                    select [
                        Attr.Dynamic "value" modeView
                        on.change (fun el _ ->
                            // Cast to HTMLInputElement — both share the .value JS property
                            let mode =
                                match (As<HTMLInputElement>(el)).Value with
                                | "1" -> Progressive
                                | "2" -> SocialSecurity
                                | _   -> FixedRate
                            state.Update(fun p -> { p with TaxMode = mode })
                        )
                    ] [
                        option [attr.value "0"] [text "Fixed Rate"]
                        option [attr.value "1"] [text "Progressive (brackets)"]
                        option [attr.value "2"] [text "Self-Employment / Social Security (15.3%)"]
                    ]
                ]
            ]
        ]

    // ── Stat card ───────────────────────────────────────────────────────

    let private statCard (lbl: string) (icon: string) (tagCls: string) (valueView: View<string>) =
        div [attr.``class`` "box mb-3"] [
            div [attr.``class`` "level is-mobile"] [
                div [attr.``class`` "level-left"] [
                    div [] [
                        p [attr.``class`` "heading has-text-grey-light"] [text lbl]
                        p [attr.``class`` (sprintf "title is-4 %s" tagCls)] [ textView valueView ]
                    ]
                ]
                div [attr.``class`` "level-right"] [
                    span [attr.``class`` (sprintf "icon is-large %s" tagCls)] [
                        i [attr.``class`` (sprintf "fas fa-2x %s" icon)] []
                    ]
                ]
            ]
        ]

    // ── Savings rate progress bar ───────────────────────────────────────

    let private savingsBar (state: Var<BudgetProfile>) =
        let rateView  = state.View |> View.Map savingsRate
        let pctView   = rateView |> View.Map (fun r -> sprintf "%.1f%%" (max 0.0 r))
        let colorView = rateView |> View.Map (fun r ->
            if r >= 30.0 then "is-success"
            elif r >= 5.0 then "is-warning"
            else "is-danger"
        )
        let valView = rateView |> View.Map (fun r -> string (int (max 0.0 (min 100.0 r))))
        div [attr.``class`` "box mb-3"] [
            div [attr.``class`` "level is-mobile mb-2"] [
                div [attr.``class`` "level-left"] [
                    p [attr.``class`` "heading has-text-grey-light"] [text "Savings Rate"]
                ]
                div [attr.``class`` "level-right"] [
                    span [attr.``class`` "tag is-dark"] [ textView pctView ]
                ]
            ]
            progress [
                Attr.Dynamic "class" (colorView |> View.Map (sprintf "progress %s"))
                Attr.Dynamic "value" valView
                attr.max "100"
            ] []
        ]

    // ── Entry point ─────────────────────────────────────────────────────

    [<SPAEntryPoint>]
    let Main () =
        let state = Var.Create (Storage.load())

        // Persist + update chart on every state change
        state.View |> View.Sink (fun p ->
            Storage.save p
            Charts.update p
        )

        // Derived views for right panel
        let netView  = state.View |> View.Map (netSavings >> fmt)
        let burnView = state.View |> View.Map (burnRate   >> fmt)
        let taxView  = state.View |> View.Map (fun p ->
            fmt (calculateTax p.MonthlyIncome p.TaxRate p.TaxMode)
        )
        let netColorView = state.View |> View.Map (fun p ->
            if float (netSavings p) >= 0.0 then "has-text-success" else "has-text-danger"
        )

        let ui =
            div [attr.``class`` "container is-fluid mt-4 px-4"] [

                // ── Header hero ──────────────────────────────────────
                div [attr.``class`` "hero is-dark is-small mb-5"] [
                    div [attr.``class`` "hero-body py-4"] [
                        p [attr.``class`` "title"] [
                            span [attr.``class`` "icon-text"] [
                                span [attr.``class`` "icon"] [ i [attr.``class`` "fas fa-earth-americas"] [] ]
                                span [] [text " NomadicBudget"]
                            ]
                        ]
                        p [attr.``class`` "subtitle is-6 has-text-grey-light"] [
                            text "Digital Nomad \u00b7 Cost of Living Calculator"
                        ]
                    ]
                ]

                // ── Two-column layout ────────────────────────────────
                div [attr.``class`` "columns is-variable is-4"] [

                    // Left: inputs
                    div [attr.``class`` "column is-5"] [
                        div [attr.``class`` "box"] [
                            p [attr.``class`` "title is-5 has-text-light mb-4"] [
                                span [attr.``class`` "icon-text"] [
                                    span [attr.``class`` "icon"] [ i [attr.``class`` "fas fa-sliders"] [] ]
                                    span [] [text " Budget Inputs"]
                                ]
                            ]
                            incomeField state
                            hr [] []
                            sliderField "Housing"    "fa-house"       0.0 5000.0 50.0 (fun p -> p.Housing)    (fun v p -> { p with Housing    = v }) state
                            sliderField "Food"       "fa-utensils"    0.0 2000.0 25.0 (fun p -> p.Food)       (fun v p -> { p with Food       = v }) state
                            sliderField "Transport"  "fa-car"         0.0 1000.0 25.0 (fun p -> p.Transport)  (fun v p -> { p with Transport  = v }) state
                            sliderField "Healthcare" "fa-heart-pulse" 0.0 1000.0 25.0 (fun p -> p.Healthcare) (fun v p -> { p with Healthcare = v }) state
                            sliderField "Fun"        "fa-gamepad"     0.0 2000.0 25.0 (fun p -> p.Fun)        (fun v p -> { p with Fun        = v }) state
                            sliderField "Other"      "fa-ellipsis"    0.0 1000.0 25.0 (fun p -> p.Other)      (fun v p -> { p with Other      = v }) state
                            hr [] []
                            taxRateField state
                            taxModeSelect state
                        ]
                    ]

                    // Right: stats + chart
                    div [attr.``class`` "column"] [
                        // Net savings – color flips when negative
                        div [attr.``class`` "box mb-3"] [
                            div [attr.``class`` "level is-mobile"] [
                                div [attr.``class`` "level-left"] [
                                    div [] [
                                        p [attr.``class`` "heading has-text-grey-light"] [text "Net Monthly Savings"]
                                        p [Attr.Dynamic "class" (netColorView |> View.Map (sprintf "title is-3 %s"))] [
                                            textView netView
                                        ]
                                    ]
                                ]
                                div [attr.``class`` "level-right"] [
                                    span [attr.``class`` "icon is-large has-text-success"] [
                                        i [attr.``class`` "fas fa-2x fa-piggy-bank"] []
                                    ]
                                ]
                            ]
                        ]

                        statCard "Monthly Burn Rate" "fa-fire"             "has-text-danger"  burnView
                        statCard "Monthly Tax"        "fa-building-columns" "has-text-warning" taxView
                        savingsBar state

                        // Doughnut chart
                        div [attr.``class`` "box"] [
                            p [attr.``class`` "title is-6 has-text-light mb-3"] [text "Expense Breakdown"]
                            div [attr.style "height:280px; position:relative;"] [
                                canvas [attr.id "expense-chart"] []
                            ]
                        ]
                    ]
                ]
            ]

        Doc.RunById "main" ui

        // Init Chart.js after DOM is committed
        async {
            do! Async.Sleep 80
            Charts.init "expense-chart" state.Value
        }
        |> Async.StartImmediate
