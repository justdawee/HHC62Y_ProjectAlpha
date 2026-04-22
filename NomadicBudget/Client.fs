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

    // ── Expense slider field ────────────────────────────────────────────────
    // Matches NBSliderField.jsx: nb-field[__head, __label, __value] + nb-range

    let private sliderField
            (lbl   : string) (icon  : string)
            (minV  : float)  (maxV  : float) (stepV : float)
            (get   : BudgetProfile -> float<usd>)
            (setF  : float<usd> -> BudgetProfile -> BudgetProfile)
            (state : Var<BudgetProfile>) =
        let viewVal = state.View |> View.Map (fun p -> float (get p))
        div [attr.``class`` "nb-field"] [
            div [attr.``class`` "nb-field__head"] [
                span [attr.``class`` "nb-field__label"] [
                    i [attr.``class`` (sprintf "fa-solid %s" icon)] []
                    text lbl
                ]
                span [attr.``class`` "nb-field__value"] [
                    textView (viewVal |> View.Map (fun v -> sprintf "$%.0f" v))
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
                span [attr.``class`` "nb-field__label"] [
                    i [attr.``class`` "fa-solid fa-wallet"] []
                    text "Monthly Income"
                ]
                span [attr.``class`` "nb-field__value"] [
                    textView (viewVal |> View.Map (fun v -> sprintf "$%.0f" v))
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
                span [attr.``class`` "nb-field__label"] [
                    i [attr.``class`` "fa-solid fa-percent"] []
                    text "Tax Rate"
                ]
                span [attr.``class`` "nb-field__value"] [
                    textView (viewPct |> View.Map (fun v -> sprintf "%.0f%%" v))
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

    // ── Tax mode select ─────────────────────────────────────────────────────

    let private taxModeSelect (state: Var<BudgetProfile>) =
        let modeView = state.View |> View.Map (fun p ->
            match p.TaxMode with
            | FixedRate      -> "fixed"
            | Progressive    -> "progressive"
            | SocialSecurity -> "social"
        )
        div [attr.``class`` "nb-field"] [
            div [attr.``class`` "nb-field__head"] [
                span [attr.``class`` "nb-field__label"] [
                    i [attr.``class`` "fa-solid fa-building-columns"] []
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
                            | "progressive" -> Progressive
                            | "social"      -> SocialSecurity
                            | _             -> FixedRate
                        state.Update(fun p -> { p with TaxMode = mode })
                    )
                ] [
                    option [attr.value "fixed"]       [text "Fixed Rate"]
                    option [attr.value "progressive"] [text "Progressive"]
                    option [attr.value "social"]      [text "Social Security (15.3%)"]
                ]
            ]
        ]

    // ── Savings rate bar ────────────────────────────────────────────────────
    // Matches NBSavingsBar.jsx: nb-card nb-savings, head/track/fill/zones
    // negative = <5%, stable = 5-30%, thriving (default, cyan) = >=30%

    let private savingsBar (state: Var<BudgetProfile>) =
        let rateView    = state.View |> View.Map savingsRate
        let pctText     = rateView |> View.Map (fun r -> sprintf "%.1f%%" r)
        let pctCls      = rateView |> View.Map (fun r ->
            if r < 5.0   then "nb-savings__pct negative"
            elif r < 30.0 then "nb-savings__pct stable"
            else "nb-savings__pct"
        )
        let fillWidth   = rateView |> View.Map (fun r -> sprintf "width:%.1f%%" (max 0.0 (min 100.0 r)))
        let dangerCls   = rateView |> View.Map (fun r -> if r < 5.0 then "danger active" else "danger")
        let stableCls   = rateView |> View.Map (fun r -> if r >= 5.0 && r < 30.0 then "stable active" else "stable")
        let thrivingCls = rateView |> View.Map (fun r -> if r >= 30.0 then "thriving active" else "thriving")

        div [attr.``class`` "nb-card nb-savings"] [
            div [attr.``class`` "nb-savings__head"] [
                span [attr.``class`` "nb-savings__label"] [text "Savings Rate"]
                span [Attr.Dynamic "class" pctCls] [textView pctText]
            ]
            div [attr.``class`` "nb-savings__track"] [
                div [
                    attr.``class`` "nb-savings__fill"
                    Attr.Dynamic "style" fillWidth
                ] []
            ]
            div [attr.``class`` "nb-savings__zones"] [
                span [Attr.Dynamic "class" dangerCls]   [text "Danger <5%"]
                span [Attr.Dynamic "class" stableCls]   [text "Stable >5%"]
                span [Attr.Dynamic "class" thrivingCls] [text "Thriving >30%"]
            ]
        ]

    // ── Entry point ─────────────────────────────────────────────────────────

    [<SPAEntryPoint>]
    let Main () =
        let state = Var.Create (Storage.load())

        // Persist on every state change (charts update via Doc.BindView below)
        state.View |> View.Sink (fun p ->
            Storage.save p
        )

        // Derived views
        let netView      = state.View |> View.Map (netSavings >> fmt)
        let burnView     = state.View |> View.Map (burnRate   >> fmt)
        let taxView      = state.View |> View.Map (fun p ->
            fmt (calculateTax p.MonthlyIncome p.TaxRate p.TaxMode)
        )
        let totalView    = state.View |> View.Map (fun p ->
            sprintf "$%.0f" (float (totalExpenses p))
        )
        // positive/negative — matches .nb-kpi-hero__value.positive / .negative in CSS
        let netClassView = state.View |> View.Map (fun p ->
            if float (netSavings p) >= 0.0
            then "nb-kpi-hero__value positive"
            else "nb-kpi-hero__value negative"
        )

        // Reactive legend: matches NBChartCard legend rows
        let legendDoc =
            Doc.BindView (fun profile ->
                div [attr.``class`` "nb-chart-legend"] (
                    allCategories |> List.map (fun cat ->
                        div [attr.``class`` "nb-leg-row"] [
                            span [attr.``class`` "name"] [
                                span [
                                    attr.``class`` "dot"
                                    attr.style (sprintf "background:%s" (categoryColor cat))
                                ] []
                                text (categoryName cat)
                            ]
                            span [attr.``class`` "v"] [text (fmt (categoryValue profile cat))]
                        ]
                    )
                )
            ) state.View

        let ui =
            div [attr.``class`` "nb-shell nb"] [
                div [attr.``class`` "nb-grain"; Attr.Create "aria-hidden" "true"] []
                div [attr.``class`` "nb-container"] [

                    // ── Top bar ──────────────────────────────────────────
                    // Matches NBTopBar.jsx exactly
                    div [attr.``class`` "nb-topbar"] [
                        div [attr.``class`` "nb-brand"] [
                            i [attr.``class`` "fa-solid fa-earth-americas"] []
                            span [attr.``class`` "name"] [text "NomadicBudget"]
                        ]
                        div [attr.``class`` "nb-tag"] [text "Digital Nomad Calculator"]
                    ]

                    // ── Main grid ────────────────────────────────────────
                    div [attr.``class`` "nb-grid nb-stagger"] [

                        // ── Left: NBInputsPanel ──────────────────────────
                        div [attr.``class`` "nb-card nb-card--accent"] [
                            div [attr.``class`` "nb-section-head"] [
                                i [attr.``class`` "fa-solid fa-sliders"] []
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

                        // ── Right: NBHeroCard + KPIs + Savings + Chart ───
                        div [attr.style "display:flex;flex-direction:column;gap:14px"] [

                            // NBHeroCard: net savings + sparkline
                            div [attr.``class`` "nb-card nb-card--hoverable"] [
                                div [attr.``class`` "nb-kpi-hero"] [
                                    div [] [
                                        div [attr.``class`` "nb-kpi-hero__label"] [
                                            text "Net Monthly Savings"
                                        ]
                                        div [Attr.Dynamic "class" netClassView] [
                                            textView netView
                                        ]
                                    ]
                                    Doc.Element "svg" [
                                        attr.id "nb-sparkline"
                                        attr.``class`` "nb-kpi-hero__spark"
                                        Attr.Create "viewBox" "0 0 200 80"
                                        Attr.Create "xmlns"   "http://www.w3.org/2000/svg"
                                    ] []
                                    Doc.BindView (fun p ->
                                        Charts.updateSparkline p
                                        Doc.Empty
                                    ) state.View
                                ]
                            ]

                            // KPI row: burn + tax
                            div [attr.``class`` "nb-kpi-row"] [
                                div [attr.``class`` "nb-card nb-card--hoverable nb-kpi nb-kpi--coral"] [
                                    div [] [
                                        div [attr.``class`` "nb-kpi__label"] [text "Burn Rate"]
                                        div [attr.``class`` "nb-kpi__value"] [textView burnView]
                                    ]
                                    i [attr.``class`` "fa-solid fa-fire nb-kpi__icon"] []
                                ]
                                div [attr.``class`` "nb-card nb-card--hoverable nb-kpi nb-kpi--amber"] [
                                    div [] [
                                        div [attr.``class`` "nb-kpi__label"] [text "Monthly Tax"]
                                        div [attr.``class`` "nb-kpi__value"] [textView taxView]
                                    ]
                                    i [attr.``class`` "fa-solid fa-building-columns nb-kpi__icon"] []
                                ]
                            ]

                            // NBSavingsBar (inside its own nb-card)
                            savingsBar state

                            // NBChartCard: doughnut + legend
                            div [attr.``class`` "nb-card nb-chart-card"] [
                                div [attr.``class`` "nb-section-head"] [
                                    i [attr.``class`` "fa-solid fa-chart-pie"] []
                                    span [attr.``class`` "t"] [text "Expense Breakdown"]
                                ]
                                div [attr.``class`` "nb-chart-wrap"] [
                                    // Canvas: SVG + centre overlay div (matches NBDoughnut.jsx)
                                    div [attr.``class`` "nb-chart-canvas"] [
                                        Doc.Element "svg" [
                                            attr.id "nb-donut"
                                            Attr.Create "width"   "200"
                                            Attr.Create "height"  "200"
                                            Attr.Create "viewBox" "0 0 200 200"
                                            Attr.Create "xmlns"   "http://www.w3.org/2000/svg"
                                        ] []
                                        // Reactive centre text overlay
                                        div [attr.``class`` "nb-chart-center"] [
                                            div [attr.``class`` "lbl"] [text "Total"]
                                            div [attr.``class`` "val"] [textView totalView]
                                        ]
                                        // Render donut after SVG is in DOM
                                        Doc.BindView (fun p ->
                                            Charts.update p
                                            Doc.Empty
                                        ) state.View
                                    ]
                                    legendDoc
                                ]
                            ]
                        ]
                    ]
                ]
            ]

        Doc.RunById "main" ui
