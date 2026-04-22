namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open Domain

[<JavaScript>]
module Charts =

    // ── Doughnut ────────────────────────────────────────────────────────────
    // Stroke-dasharray technique on <circle> elements.
    // r = (200 - strokeWidth) / 2 = (200 - 22) / 2 = 89
    // circ = 2 * PI * 89 ≈ 559.20
    // Each segment: strokeDasharray = "segLen rest", strokeDashoffset = -acc*circ
    // rotate(-90 100 100) starts drawing from the top.

    [<Inline """
    (function(data, colors, total) {
        var el = document.getElementById('nb-donut');
        if (!el) return;
        while (el.firstChild) { el.removeChild(el.firstChild); }

        var NS  = 'http://www.w3.org/2000/svg';
        var cx  = 100, cy = 100, r = 89, sw = 22;
        var circ = 2 * Math.PI * r;

        // Base (track) circle
        var base = document.createElementNS(NS, 'circle');
        base.setAttribute('cx', cx);
        base.setAttribute('cy', cy);
        base.setAttribute('r', r);
        base.setAttribute('fill', 'none');
        base.setAttribute('stroke', '#141B2D');
        base.setAttribute('stroke-width', sw);
        el.appendChild(base);

        if (total <= 0) return;

        var acc = 0;
        for (var i = 0; i < data.length; i++) {
            if (data[i] <= 0) { continue; }
            var frac = data[i] / total;
            var len  = frac * circ;
            var seg  = document.createElementNS(NS, 'circle');
            seg.setAttribute('cx', cx);
            seg.setAttribute('cy', cy);
            seg.setAttribute('r', r);
            seg.setAttribute('fill', 'none');
            seg.setAttribute('stroke', colors[i]);
            seg.setAttribute('stroke-width', sw);
            seg.setAttribute('stroke-dasharray', len.toFixed(3) + ' ' + (circ - len).toFixed(3));
            seg.setAttribute('stroke-dashoffset', (-acc * circ).toFixed(3));
            seg.setAttribute('transform', 'rotate(-90 ' + cx + ' ' + cy + ')');
            el.appendChild(seg);
            acc += frac;
        }

        // Centre: "TOTAL" label
        var t1 = document.createElementNS(NS, 'text');
        t1.setAttribute('x', '100'); t1.setAttribute('y', '94');
        t1.setAttribute('text-anchor', 'middle');
        t1.setAttribute('fill', '#AEB6C6');
        t1.setAttribute('font-family', 'DM Mono, monospace');
        t1.setAttribute('font-size', '9');
        t1.setAttribute('letter-spacing', '2');
        t1.textContent = 'TOTAL';
        el.appendChild(t1);

        // Centre: dollar amount
        var t2 = document.createElementNS(NS, 'text');
        t2.setAttribute('x', '100'); t2.setAttribute('y', '116');
        t2.setAttribute('text-anchor', 'middle');
        t2.setAttribute('fill', '#E8ECF4');
        t2.setAttribute('font-family', 'Syne, sans-serif');
        t2.setAttribute('font-weight', '700');
        t2.setAttribute('font-size', '20');
        t2.textContent = '$' + Math.round(total).toLocaleString();
        el.appendChild(t2);
    })($data, $colors, $total)
    """>]
    let private renderDonut (data: float[]) (colors: string[]) (total: float) : unit = ()

    let update (profile: BudgetProfile) : unit =
        let data   = allCategories |> List.map (fun cat -> float (categoryValue profile cat)) |> Array.ofList
        let colors = allCategories |> List.map categoryColor |> Array.ofList
        let total  = float (totalExpenses profile)
        renderDonut data colors total

    // ── Sparkline ───────────────────────────────────────────────────────────
    // Formula from Dashboard.jsx:
    //   ratio = net / income  (clamped to -0.6 … 0.8)
    //   21 points, x ∈ [0,1], y = 0.55 - r*0.45*(0.4+0.6*x) + sin(x*6+r*2)*0.05
    //   SVG viewBox "0 0 200 80"
    //   color: cyan when ratio ≥ 0, coral otherwise

    [<Inline """
    (function(ratio) {
        var el = document.getElementById('nb-sparkline');
        if (!el) return;
        while (el.firstChild) { el.removeChild(el.firstChild); }

        var r = Math.max(-0.6, Math.min(0.8, ratio));
        var pts = [];
        for (var i = 0; i <= 20; i++) {
            var x    = i / 20;
            var y    = 0.55 - r * 0.45 * (0.4 + 0.6 * x) + Math.sin(x * 6 + r * 2) * 0.05;
            pts.push([x * 200, y * 80]);
        }

        var lineColor = r >= 0 ? '#3DD6F5' : '#FF4D6D';

        // Smooth cubic bezier path
        var d = 'M ' + pts[0][0].toFixed(2) + ',' + pts[0][1].toFixed(2);
        for (var i = 1; i < pts.length; i++) {
            var cpx = (pts[i-1][0] + pts[i][0]) / 2;
            d += ' C ' + cpx.toFixed(2) + ',' + pts[i-1][1].toFixed(2) +
                 ' '  + cpx.toFixed(2) + ',' + pts[i][1].toFixed(2) +
                 ' '  + pts[i][0].toFixed(2) + ',' + pts[i][1].toFixed(2);
        }

        var NS   = 'http://www.w3.org/2000/svg';
        var defs = document.createElementNS(NS, 'defs');
        var grad = document.createElementNS(NS, 'linearGradient');
        grad.setAttribute('id', 'spk-g');
        grad.setAttribute('x1', '0'); grad.setAttribute('y1', '0');
        grad.setAttribute('x2', '0'); grad.setAttribute('y2', '1');
        var s1 = document.createElementNS(NS, 'stop');
        s1.setAttribute('offset', '0%');   s1.setAttribute('stop-color', lineColor); s1.setAttribute('stop-opacity', '0.35');
        var s2 = document.createElementNS(NS, 'stop');
        s2.setAttribute('offset', '100%'); s2.setAttribute('stop-color', lineColor); s2.setAttribute('stop-opacity', '0.02');
        grad.appendChild(s1); grad.appendChild(s2);
        defs.appendChild(grad);
        el.appendChild(defs);

        // Filled area
        var area = document.createElementNS(NS, 'path');
        area.setAttribute('d', d + ' L200,80 L0,80 Z');
        area.setAttribute('fill', 'url(#spk-g)');
        el.appendChild(area);

        // Stroke line
        var line = document.createElementNS(NS, 'path');
        line.setAttribute('d', d);
        line.setAttribute('fill', 'none');
        line.setAttribute('stroke', lineColor);
        line.setAttribute('stroke-width', '2');
        line.setAttribute('stroke-linecap', 'round');
        line.setAttribute('stroke-linejoin', 'round');
        el.appendChild(line);
    })($ratio)
    """>]
    let private renderSparkline (ratio: float) : unit = ()

    let updateSparkline (profile: BudgetProfile) : unit =
        let ratio =
            if profile.MonthlyIncome = 0.0<usd> then 0.0
            else float (netSavings profile) / float profile.MonthlyIncome
        renderSparkline ratio
