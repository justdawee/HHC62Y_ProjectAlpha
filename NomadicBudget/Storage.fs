namespace NomadicBudget

open WebSharper
open Domain

[<JavaScript>]
module Storage =
    let save (_: BudgetProfile) : unit = ()
    let load () : BudgetProfile = defaultProfile
