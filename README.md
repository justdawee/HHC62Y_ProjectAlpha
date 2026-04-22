# NomadicBudget – Digital Nomad Cost of Living Calculator

A reactive, **client-only SPA** built with **F# + WebSharper UI**. Helps digital nomads visualize their net monthly savings after taxes and local expenses, with live slider updates, a doughnut chart, and LocalStorage persistence.

## Try Live

**[https://justdawee.github.io/HHC62Y_ProjectAlpha/](https://justdawee.github.io/HHC62Y_ProjectAlpha/)**

## Screenshots

<!-- Add screenshots here after first deploy -->

## Motivation

Choosing where to live as a digital nomad requires balancing cost of living, local taxes, and personal spending habits. NomadicBudget makes this tangible: adjust sliders for housing, food, transport, healthcare, and fun, pick a tax mode, and instantly see your net savings—all in the browser, no server needed.

## Features

- **Reactive sliders** for 6 expense categories (Housing, Food, Transport, Healthcare, Fun, Other)
- **Three tax modes**: Fixed Rate, Progressive tax brackets, US Self-Employment / Social Security (15.3%)
- **Savings rate** progress bar (color-coded: green ≥30%, yellow ≥5%, red <5%)
- **Doughnut chart** showing expense breakdown (Chart.js 4)
- **LocalStorage persistence** – data survives page refresh
- **F# Units of Measure** (`float<usd>`) for type-safe monetary arithmetic
- **Responsive layout** with Bulma CSS + FontAwesome icons

## Architecture

| File | Responsibility |
|------|---------------|
| `Domain.fs` | `[<Measure>] type usd`, DUs, records, pure calculation logic |
| `Storage.fs` | localStorage JSON round-trip via `[<Inline>]` JS interop |
| `Charts.fs`  | Chart.js interop: `new Chart(...)` + `chart.update()` |
| `Client.fs`  | `Var<BudgetProfile>` reactive state, slider UI, entry point |

## Build & Run

### Prerequisites

- [.NET SDK 10.x](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org) (needed by esbuild bundler)

### Development (hot-reload)

```bash
cd NomadicBudget/NomadicBudget
dotnet run
```

Open `https://localhost:5001`.

### Release / Static Build

```bash
cd NomadicBudget/NomadicBudget
dotnet build --configuration Release
```

Deployable static files will be in `NomadicBudget/NomadicBudget/wwwroot/`.
