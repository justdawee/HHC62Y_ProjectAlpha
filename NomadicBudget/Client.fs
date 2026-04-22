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

    // ── Formatting ──────────────────────────────────────────────────────────

    let private fmt (amount: float<usd>) =
        let n = float amount
        if n >= 0.0 then sprintf "$%.0f" n
        else sprintf "-$%.0f" (abs n)

    // ── Expense slider ──────────────────────────────────────────────────────

    let private sliderField
            (lbl   : string) (icon  : string)
            (minV  : float)  (maxV  : float) (stepV : float)
            (get   : BudgetProfile -> float<usd>)
            (setF  : float<usd> -> BudgetProfile -> BudgetProfile)
            (state : Var<BudgetProfile>) =
        let viewVal = state.View |> View.Map (fun p -> float (get p))
        div [attr.``class`` "nb-field"] [
            div [attr.``class`` "nb-field__head"] [
                label [attr.``class`` "nb-field__label"] [
                    span [attr.``class`` "nb-field__icon"] [
                        i [attr.``class`` (sprintf "fas %s" icon)] []
                    ]
                    text lbl
                ]
                span [attr.``class`` "nb-field__value"] [
                    textView (viewVal |> View.Map (sprintf "$%.0f"))
                ]
            ]
            input [
                attr.``type``  "range"
                attr.``class`` "nb-range"
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

    // ── Income slider ───────────────────────────────────────────────────────

    let private incomeField (state: Var<BudgetProfile>) =
        let viewVal = state.View |> View.Map (fun p -> float p.MonthlyIncome)
        div [attr.``class`` "nb-field nb-field--income"] [
            div [attr.``class`` "nb-field__head"] [
                label [attr.``class`` "nb-field__label"] [
                    span [attr.``class`` "nb-field__icon"] [
                        i [attr.``class`` "fas fa-wallet"] []
                    ]
                    text "Monthly Income"
                ]
                span [attr.``class`` "nb-field__value"] [
                    textView (viewVal |> View.Map (sprintf "$%.0f"))
                ]
            ]
            input [
                attr.``type``  "range"
                attr.``class`` "nb-range nb-range--income"
                attr.min "500" ; attr.max "20000" ; attr.step "100"
                Attr.Dynamic "value" (viewVal |> View.Map string)
                on.input (fun el _ ->
                    let v = float (As<HTMLInputElement>(el)).Value
                    state.Update(fun p -> { p with MonthlyIncome = v * 1.0<usd> })
                )
            ] []
        ]

    // ── Tax rate slider ─────────────────────────────────────────────────────

    let private taxRateField (state: Var<BudgetProfile>) =
        let viewPct = state.View |> View.Map (fun p -> p.TaxRate * 100.0)
        div [attr.``class`` "nb-field nb-field--tax"] [
            div [attr.``class`` "nb-field__head"] [
                label [attr.``class`` "nb-field__label"] [
                    span [attr.``class`` "nb-field__icon"] [
                        i [attr.``class`` "fas fa-percent"] []
                    ]
                    text "Tax Rate"
                ]
                span [attr.``class`` "nb-field__value"] [
                    textView (viewPct |> View.Map (sprintf "%.0f%%"))
                ]
            ]
            input [
                attr.``type``  "range"
                attr.``class`` "nb-range nb-range--tax"
                attr.min "0" ; attr.max "60" ; attr.step "1"
                Attr.Dynamic "value" (viewPct |> View.Map string)
                on.input (fun el _ ->
                    let v = float (As<HTMLInputElement>(el)).Value / 100.0
                    state.Update(fun p -> { p with TaxRate = v })
                )
            ] []
        ]

    // ── Tax mode ────────────────────────────────────────────────────────────

    let private taxModeSelect (state: Var<BudgetProfile>) =
        let modeView = state.View |> View.Map (fun p ->
            match p.TaxMode with
            | FixedRate      -> "0"
            | Progressive    -> "1"
            | SocialSecurity -> "2"
        )
        div [attr.``class`` "nb-field"] [
            div [attr.``class`` "nb-field__head"] [
                label [attr.``class`` "nb-field__label"] [
                    span [attr.``class`` "nb-field__icon"] [
                        i [attr.``class`` "fas fa-building-columns"] []
                    ]
                    text "Tax Mode"
                ]
            ]
            div [attr.``class`` "nb-select-wrap"] [
                select [
                    attr.``class`` "nb-select"
                    Attr.Dynamic "value" modeView
                    on.change (fun el _ ->
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
                    option [attr.value "2"] [text "Self-Employment / SS (15.3%)"]
                ]
            ]
        ]

    // ── Savings rate bar ─────────────────────────────────────────────────────
    // Track shows 3-zone background (danger / stable / thriving).
    // A gradient fill expands from left to savings %.

    let private savingsBar (state: Var<BudgetProfile>) =
        let rateView     = state.View |> View.Map savingsRate
        let pctText      = rateView |> View.Map (fun r -> sprintf "%.1f%%" (max 0.0 r))
        let fillStyle    = rateView |> View.Map (fun r -> sprintf "width:%.1f%%" (max 0.0 (min 100.0 r)))
        let pctCls       = rateView |> View.Map (fun r ->
            if r >= 30.0 then "nb-savings__pct nb-savings__pct--thriving"
            elif r >= 5.0 then "nb-savings__pct nb-savings__pct--stable"
            else "nb-savings__pct nb-savings__pct--danger"
        )
        div [attr.``class`` "nb-savings"] [
            div [attr.``class`` "nb-savings__header"] [
                span [attr.``class`` "nb-savings__label"] [text "Savings Rate"]
                span [Attr.Dynamic "class" pctCls] [textView pctText]
            ]
            div [attr.``class`` "nb-savings__track"] [
                div [
                    attr.``class`` "nb-savings__fill"
                    Attr.Dynamic "style" fillStyle
                ] []
            ]
            div [attr.``class`` "nb-savings__zones"] [
                span [attr.``class`` "nb-savings__zone"] [text "Danger <5%"]
                span [attr.``class`` "nb-savings__zone"] [text "Stable >5%"]
                span [attr.``class`` "nb-savings__zone"] [text "Thriving >30%"]
            ]
        ]

    // ── Entry point ─────────────────────────────────────────────────────────

    [<SPAEntryPoint>]
    let Main () =
        let state = Var.Create (Storage.load())

        // Persist + redraw both charts on every state change
        state.View |> View.Sink (fun p ->
            Storage.save p
            Charts.update p
            Charts.updateSparkline p
        )

        // Derived views
        let netView      = state.View |> View.Map (netSavings >> fmt)
        let burnView     = state.View |> View.Map (burnRate   >> fmt)
        let taxView      = state.View |> View.Map (fun p ->
            fmt (calculateTax p.MonthlyIncome p.TaxRate p.TaxMode)
        )
        let netClassView = state.View |> View.Map (fun p ->
            if float (netSavings p) >= 0.0
            then "nb-kpi-hero__value"
            else "nb-kpi-hero__value nb-kpi-hero__value--neg"
        )

        // Reactive legend: 2-col grid, each row: name-left / value-right
        let legendDoc =
            Doc.BindView (fun profile ->
                div [attr.``class`` "nb-chart-legend"] (
                    allCategories |> List.map (fun cat ->
                        div [attr.``class`` "nb-leg-row"] [
                            div [attr.``class`` "name"] [
                                div [
                                    attr.``class`` "nb-leg-dot"
                                    attr.style (sprintf "background:%s" (categoryColor cat))
                                ] []
                                span [] [text (categoryName cat)]
                            ]
                            span [attr.``class`` "v"] [text (fmt (categoryValue profile cat))]
                        ]
                    )
                )
            ) state.View

        let ui =
            div [attr.``class`` "nb-shell nb"] [
                div [attr.``class`` "nb-grain"] []
                div [attr.``class`` "nb-container"] [

                    // ── Top bar ──────────────────────────────────────────
                    div [attr.``class`` "nb-topbar"] [
                        div [attr.``class`` "nb-brand"] [
                            // Logo mark inline SVG (amber gradient globe)
                            Doc.Element "svg" [
                                attr.``class`` "nb-brand-icon"
                                Attr.Create "viewBox" "0 0 32 32"
                                Attr.Create "xmlns"   "http://www.w3.org/2000/svg"
                                Attr.Create "fill"    "none"
                            ] [
                                Doc.Element "defs" [] [
                                    Doc.Element "linearGradient" [
                                        Attr.Create "id"  "lg"
                                        Attr.Create "x1"  "0%"
                                        Attr.Create "y1"  "0%"
                                        Attr.Create "x2"  "100%"
                                        Attr.Create "y2"  "100%"
                                    ] [
                                        Doc.Element "stop" [Attr.Create "offset" "0%";   Attr.Create "stop-color" "#F5A623"] []
                                        Doc.Element "stop" [Attr.Create "offset" "100%"; Attr.Create "stop-color" "#FF7A38"] []
                                    ]
                                ]
                                Doc.Element "circle" [
                                    Attr.Create "cx"           "16"
                                    Attr.Create "cy"           "16"
                                    Attr.Create "r"            "14"
                                    Attr.Create "stroke"       "url(#lg)"
                                    Attr.Create "stroke-width" "2"
                                ] []
                                Doc.Element "ellipse" [
                                    Attr.Create "cx"             "16"
                                    Attr.Create "cy"             "16"
                                    Attr.Create "rx"             "6"
                                    Attr.Create "ry"             "14"
                                    Attr.Create "stroke"         "url(#lg)"
                                    Attr.Create "stroke-width"   "1.5"
                                    Attr.Create "stroke-dasharray" "3 4"
                                ] []
                                Doc.Element "line" [
                                    Attr.Create "x1"           "2"
                                    Attr.Create "y1"           "16"
                                    Attr.Create "x2"           "30"
                                    Attr.Create "y2"           "16"
                                    Attr.Create "stroke"       "url(#lg)"
                                    Attr.Create "stroke-width" "1.5"
                                    Attr.Create "stroke-dasharray" "3 4"
                                ] []
                            ]
                            span [attr.``class`` "name"] [text "NomadicBudget"]
                        ]
                        span [attr.``class`` "nb-tag"] [text "Digital Nomad Calculator"]
                    ]

                    // ── Main grid ────────────────────────────────────────
                    div [attr.``class`` "nb-grid nb-stagger"] [

                        // ── Left: Budget Inputs ──────────────────────────
                        div [attr.``class`` "nb-card nb-card--accent"] [
                            div [attr.``class`` "nb-section-head"] [
                                i [attr.``class`` "fas fa-sliders"] []
                                span [attr.``class`` "t"] [text "Budget Inputs"]
                            ]

                            incomeField state
                            hr [attr.``class`` "nb-divider"] []

                            sliderField "Housing"    "fa-house"       0.0 5000.0 50.0
                                (fun p -> p.Housing)    (fun v p -> { p with Housing    = v }) state
                            sliderField "Food"       "fa-utensils"    0.0 2000.0 25.0
                                (fun p -> p.Food)       (fun v p -> { p with Food       = v }) state
                            sliderField "Transport"  "fa-car"         0.0 1000.0 25.0
                                (fun p -> p.Transport)  (fun v p -> { p with Transport  = v }) state
                            sliderField "Healthcare" "fa-heart-pulse" 0.0 1000.0 25.0
                                (fun p -> p.Healthcare) (fun v p -> { p with Healthcare = v }) state
                            sliderField "Fun"        "fa-gamepad"     0.0 2000.0 25.0
                                (fun p -> p.Fun)        (fun v p -> { p with Fun        = v }) state
                            sliderField "Other"      "fa-ellipsis"    0.0 1000.0 25.0
                                (fun p -> p.Other)      (fun v p -> { p with Other      = v }) state

                            hr [attr.``class`` "nb-divider"] []

                            taxRateField state
                            taxModeSelect state
                        ]

                        // ── Right: Stats + Charts ────────────────────────
                        div [attr.style "display:flex;flex-direction:column;gap:14px"] [

                            // Hero KPI — Net Monthly Savings + sparkline
                            div [attr.``class`` "nb-card"] [
                                div [attr.``class`` "nb-kpi-hero"] [
                                    div [] [
                                        p [attr.``class`` "nb-kpi-hero__eyebrow"] [
                                            text "Net Monthly Savings"
                                        ]
                                        p [Attr.Dynamic "class" netClassView] [
                                            textView netView
                                        ]
                                    ]
                                    // Sparkline SVG — rendered by Charts.updateSparkline
                                    Doc.Element "svg" [
                                        attr.id "nb-sparkline"
                                        attr.``class`` "nb-kpi-hero__spark"
                                        Attr.Create "viewBox" "0 0 200 80"
                                        Attr.Create "xmlns"   "http://www.w3.org/2000/svg"
                                    ] []
                                ]
                            ]

                            // KPI row — Burn Rate + Monthly Tax
                            div [attr.``class`` "nb-kpi-row"] [
                                div [attr.``class`` "nb-card nb-card--hoverable nb-kpi nb-kpi--coral"] [
                                    div [] [
                                        p [attr.``class`` "nb-kpi__label"] [text "Burn Rate"]
                                        p [attr.``class`` "nb-kpi__value"] [textView burnView]
                                    ]
                                    i [attr.``class`` "fas fa-fire nb-kpi__icon"] []
                                ]
                                div [attr.``class`` "nb-card nb-card--hoverable nb-kpi nb-kpi--amber"] [
                                    div [] [
                                        p [attr.``class`` "nb-kpi__label"] [text "Monthly Tax"]
                                        p [attr.``class`` "nb-kpi__value"] [textView taxView]
                                    ]
                                    i [attr.``class`` "fas fa-building-columns nb-kpi__icon"] []
                                ]
                            ]

                            // Savings Rate bar
                            savingsBar state

                            // Expense Breakdown — doughnut + legend
                            div [attr.``class`` "nb-card"] [
                                div [attr.``class`` "nb-section-head"] [
                                    i [attr.``class`` "fas fa-chart-pie"] []
                                    span [attr.``class`` "t"] [text "Expense Breakdown"]
                                ]
                                div [attr.``class`` "nb-chart-wrap"] [
                                    div [attr.``class`` "nb-chart-canvas"] [
                                        Doc.Element "svg" [
                                            attr.id "nb-donut"
                                            attr.``class`` "nb-chart-svg"
                                            Attr.Create "viewBox" "0 0 200 200"
                                            Attr.Create "xmlns"   "http://www.w3.org/2000/svg"
                                        ] []
                                    ]
                                    legendDoc
                                ]
                            ]
                        ]
                    ]
                ]
            ]

        Doc.RunById "main" ui

        // Both charts need a short delay for the DOM to commit
        async {
            do! Async.Sleep 80
            Charts.update state.Value
            Charts.updateSparkline state.Value
        }
        |> Async.StartImmediate
