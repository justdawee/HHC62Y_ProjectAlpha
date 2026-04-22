namespace NomadicBudget

open WebSharper

[<Measure>] type usd

[<JavaScript>]
module Domain =

    type ExpenseCategory =
        | Housing
        | Food
        | Transport
        | Healthcare
        | Fun
        | Other

    type TaxMode =
        | FixedRate
        | Progressive
        | SocialSecurity

    type BudgetProfile = {
        MonthlyIncome : float<usd>
        Housing       : float<usd>
        Food          : float<usd>
        Transport     : float<usd>
        Healthcare    : float<usd>
        Fun           : float<usd>
        Other         : float<usd>
        TaxRate       : float          // 0.0 – 1.0
        TaxMode       : TaxMode
        Currency      : string
    }

    let defaultProfile : BudgetProfile = {
        MonthlyIncome = 3000.0<usd>
        Housing       = 800.0<usd>
        Food          = 400.0<usd>
        Transport     = 150.0<usd>
        Healthcare    = 100.0<usd>
        Fun           = 200.0<usd>
        Other         = 100.0<usd>
        TaxRate       = 0.25
        TaxMode       = FixedRate
        Currency      = "USD"
    }

    let calculateTax (income: float<usd>) (rate: float) (mode: TaxMode) : float<usd> =
        match mode with
        | FixedRate ->
            income * rate
        | Progressive ->
            // Brackets: 10% ≤$1 000, 20% $1 000–$3 000, 30% >$3 000 (monthly)
            let i  = float income
            let b1 = min i 1000.0 * 0.10
            let b2 = (min i 3000.0 - min i 1000.0) * 0.20
            let b3 = max 0.0 (i - 3000.0) * 0.30
            (b1 + b2 + b3) * 1.0<usd>
        | SocialSecurity ->
            income * 0.153         // US self-employment tax

    let totalExpenses (p: BudgetProfile) : float<usd> =
        p.Housing + p.Food + p.Transport + p.Healthcare + p.Fun + p.Other

    let netSavings (p: BudgetProfile) : float<usd> =
        p.MonthlyIncome
        - calculateTax p.MonthlyIncome p.TaxRate p.TaxMode
        - totalExpenses p

    let savingsRate (p: BudgetProfile) : float =
        if p.MonthlyIncome = 0.0<usd> then 0.0
        else float (netSavings p) / float p.MonthlyIncome * 100.0

    let burnRate (p: BudgetProfile) : float<usd> =
        calculateTax p.MonthlyIncome p.TaxRate p.TaxMode + totalExpenses p

    let categoryName (cat: ExpenseCategory) =
        match cat with
        | Housing    -> "Housing"
        | Food       -> "Food"
        | Transport  -> "Transport"
        | Healthcare -> "Healthcare"
        | Fun        -> "Fun"
        | Other      -> "Other"

    let categoryIcon (cat: ExpenseCategory) =
        match cat with
        | Housing    -> "fa-house"
        | Food       -> "fa-utensils"
        | Transport  -> "fa-car"
        | Healthcare -> "fa-heart-pulse"
        | Fun        -> "fa-gamepad"
        | Other      -> "fa-ellipsis"

    let categoryColor (cat: ExpenseCategory) =
        match cat with
        | Housing    -> "#FF6384"
        | Food       -> "#36A2EB"
        | Transport  -> "#FFCE56"
        | Healthcare -> "#4BC0C0"
        | Fun        -> "#9966FF"
        | Other      -> "#FF9F40"

    let allCategories = [ Housing; Food; Transport; Healthcare; Fun; Other ]

    let categoryValue (p: BudgetProfile) (cat: ExpenseCategory) : float<usd> =
        match cat with
        | Housing    -> p.Housing
        | Food       -> p.Food
        | Transport  -> p.Transport
        | Healthcare -> p.Healthcare
        | Fun        -> p.Fun
        | Other      -> p.Other
