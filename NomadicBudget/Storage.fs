namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open Domain

[<JavaScript>]
module Storage =

    let private StorageKey = "nomadic-budget-v1"

    [<Inline "window.localStorage.getItem($key)">]
    let private getItem (key: string) : string = X<string>

    [<Inline "window.localStorage.setItem($key, $value)">]
    let private setItem (key: string) (value: string) : unit = ()

    [<Inline "JSON.stringify($value)">]
    let private toJson (value: obj) : string = X<string>

    [<Inline "JSON.parse($json)">]
    let private fromJson<'T> (json: string) : 'T = X<'T>

    // Plain JS-compatible type: no DUs, no Units of Measure
    type private Persisted = {
        income    : float
        housing   : float
        food      : float
        transport : float
        healthcare: float
        funAmt    : float
        other     : float
        taxRate   : float
        taxMode   : int    // 0=FixedRate  1=Progressive  2=SocialSecurity
        currency  : string
    }

    let private toPersisted (p: BudgetProfile) : Persisted = {
        income     = float p.MonthlyIncome
        housing    = float p.Housing
        food       = float p.Food
        transport  = float p.Transport
        healthcare = float p.Healthcare
        funAmt     = float p.Fun
        other      = float p.Other
        taxRate    = p.TaxRate
        taxMode    =
            match p.TaxMode with
            | FixedRate      -> 0
            | Progressive    -> 1
            | SocialSecurity -> 2
        currency   = p.Currency
    }

    let private fromPersisted (s: Persisted) : BudgetProfile = {
        MonthlyIncome = s.income     * 1.0<usd>
        Housing       = s.housing    * 1.0<usd>
        Food          = s.food       * 1.0<usd>
        Transport     = s.transport  * 1.0<usd>
        Healthcare    = s.healthcare * 1.0<usd>
        Fun           = s.funAmt     * 1.0<usd>
        Other         = s.other      * 1.0<usd>
        TaxRate       = s.taxRate
        TaxMode       =
            match s.taxMode with
            | 1 -> Progressive
            | 2 -> SocialSecurity
            | _ -> FixedRate
        Currency = s.currency
    }

    let save (p: BudgetProfile) : unit =
        p |> toPersisted |> As<obj> |> toJson |> setItem StorageKey

    let load () : BudgetProfile =
        let json = getItem StorageKey
        if isNull json || json = "" then defaultProfile
        else
            try fromJson<Persisted> json |> fromPersisted
            with _ -> defaultProfile
