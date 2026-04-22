# NomadicBudget – Digital Nomad Cost of Living Calculator

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen)

A reactive, client-side SPA designed to solve the real-world problem of budgeting across different tax regimes and cost-of-living environments. Built with **F# + WebSharper UI** for the University Project Alpha course.

## 🚀 Try it Live
**[https://justdawee.github.io/HHC62Y_ProjectAlpha/](https://justdawee.github.io/HHC62Y_ProjectAlpha/)**

---

## 📸 Screenshots

|                    Desktop Dashboard                    |                      Mobile View                       |
|:-------------------------------------------------------:|:------------------------------------------------------:|
| ![Desktop Placeholder](https://i.imgur.com/HsWGJZH.png) | ![Mobile Placeholder](https://i.imgur.com/LbEDmG9.png) |
|    *Reactive sliders and real-time Chart.js updates*    |            *Responsive Bulma-based layout*             |

---

## 💡 Motivation & Problem Statement
Choosing where to live as a digital nomad requires balancing gross income against varying local taxes and personal spending habits.

**The Problem:** Most budget calculators use static values or ignore specific tax nuances like US Self-Employment tax vs. European Progressive brackets.
**The Solution:** NomadicBudget provides a reactive playground where users can instantly see their net savings after taxes and expenses. Adjust a slider, and the doughnut chart and savings bar update in real-time—all in the browser, with no server-side latency.

---

## 🛠 Technical Implementation (F# Showcase)
This project serves as a demonstration of idiomatic F# and the WebSharper ecosystem. It contains **~660 lines of F# code** (excluding boilerplate).

### Key F# Features Used:
- **Type-Safe Arithmetic with Units of Measure:** We use `[<Measure>] type usd` to ensure that monetary calculations are never mixed with raw floats, preventing logic errors in tax calculations.
- **Domain Modeling with Discriminated Unions:** The `TaxMode` is modeled as a DU (Fixed, Progressive, US Self-Employment), allowing for exhaustive pattern matching in the calculation engine.
- **Reactive Programming (WebSharper UI):** The entire application state is held in a `Var<BudgetProfile>`. UI components are linked via `View.Map`, ensuring the DOM and Chart.js updates are always in sync with the model.
- **JSON Serialization & LocalStorage:** Custom `[<Inline>]` JavaScript interop in `Storage.fs` allows for seamless F# Record persistence to the browser's `localStorage`.

---

## 📂 Architecture Map

| File         | Responsibility                                                               |
|:-------------|:-----------------------------------------------------------------------------|
| `Domain.fs`  | **Core Logic:** Units of Measure, Tax DUs, Budget Records, and pure math.    |
| `Storage.fs` | **Persistence:** Serialization logic for saving/loading from `localStorage`. |
| `Charts.fs`  | **JS Interop:** Integration with Chart.js 4 for the expense breakdown.       |
| `Client.fs`  | **UI & State:** Reactive variables, slider components, and SPA entry point.  |
| `Startup.fs` | **ASP.NET Core:** Standard WebSharper host configuration.                    |

---

## 🔨 Build & Run Instructions

### Prerequisites
- [.NET SDK 10.x](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org) (required for the `esbuild` bundling stage)

### Development (Hot-Reload)
```bash
cd NomadicBudget/NomadicBudget
dotnet run
```
Open `https://localhost:5001`.

### Release / Static Build
The project is configured to bundle assets automatically during a Release build:
```bash
cd NomadicBudget/NomadicBudget
dotnet build --configuration Release
```
Deployable static files (HTML, CSS, JS) are generated in the `wwwroot/` folder.

---

## 📄 License
This project is licensed under the **MIT License**. See the root `LICENSE` file for details (if provided) or consider it open-source under MIT terms.
