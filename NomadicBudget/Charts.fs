namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open Domain

[<JavaScript>]
module Charts =

    // ── Doughnut ────────────────────────────────────────────────────────────
    // Clears #nb-donut and draws arc-path segments + centre label.
    // Safe to call before the element exists (guard returns early).

    [<Inline """
    (function(data, colors) {
        var el = document.getElementById('nb-donut');
        if (!el) return;
        while (el.firstChild) { el.removeChild(el.firstChild); }

        var total = 0;
        for (var k = 0; k < data.length; k++) { total += data[k]; }
        if (total <= 0) return;

        var NS  = 'http://www.w3.org/2000/svg';
        var cx  = 100, cy = 100, R = 85, r = 52;
        var gap = 0.012; // small gap between slices (radians)
        var angle = 0;

        for (var i = 0; i < data.length; i++) {
            if (data[i] <= 0) { continue; }
            var frac = data[i] / total;
            var end  = angle + frac * 2 * Math.PI - gap;
            var a1   = angle - Math.PI / 2;
            var a2   = end   - Math.PI / 2;

            var x1  = cx + R * Math.cos(a1), y1  = cy + R * Math.sin(a1);
            var x2  = cx + R * Math.cos(a2), y2  = cy + R * Math.sin(a2);
            var ix1 = cx + r * Math.cos(a1), iy1 = cy + r * Math.sin(a1);
            var ix2 = cx + r * Math.cos(a2), iy2 = cy + r * Math.sin(a2);
            var lg  = frac > 0.5 ? 1 : 0;

            var p = document.createElementNS(NS, 'path');
            p.setAttribute('d',
                'M '  + x1.toFixed(2) + ' ' + y1.toFixed(2) +
                ' A ' + R + ' ' + R + ' 0 ' + lg + ' 1 ' + x2.toFixed(2) + ' ' + y2.toFixed(2) +
                ' L ' + ix2.toFixed(2) + ' ' + iy2.toFixed(2) +
                ' A ' + r + ' ' + r + ' 0 ' + lg + ' 0 ' + ix1.toFixed(2) + ' ' + iy1.toFixed(2) +
                ' Z');
            p.setAttribute('fill', colors[i]);
            p.setAttribute('opacity', '0.93');
            el.appendChild(p);
            angle = end + gap;
        }

        // Centre labels
        var t1 = document.createElementNS(NS, 'text');
        t1.setAttribute('x', '100'); t1.setAttribute('y', '93');
        t1.setAttribute('text-anchor', 'middle');
        t1.setAttribute('fill', '#7A8DAF');
        t1.setAttribute('font-family', 'DM Mono, monospace');
        t1.setAttribute('font-size', '9');
        t1.setAttribute('letter-spacing', '1');
        t1.textContent = 'TOTAL';
        el.appendChild(t1);

        var t2 = document.createElementNS(NS, 'text');
        t2.setAttribute('x', '100'); t2.setAttribute('y', '113');
        t2.setAttribute('text-anchor', 'middle');
        t2.setAttribute('fill', '#EEF3FF');
        t2.setAttribute('font-family', 'Syne, sans-serif');
        t2.setAttribute('font-weight', '800');
        t2.setAttribute('font-size', '18');
        t2.textContent = '$' + Math.round(total).toLocaleString();
        el.appendChild(t2);
    })($data, $colors)
    """>]
    let private renderDonut (data: float[]) (colors: string[]) : unit = ()

    let update (profile: BudgetProfile) : unit =
        let data   = allCategories |> List.map (fun cat -> float (categoryValue profile cat)) |> Array.ofList
        let colors = allCategories |> List.map categoryColor |> Array.ofList
        renderDonut data colors

    // ── Sparkline ───────────────────────────────────────────────────────────
    // Draws a smooth bezier area chart into #nb-sparkline.
    // Uses the current savings rate (0-100) to shape an upward-trending curve.

    [<Inline """
    (function(rate) {
        var el = document.getElementById('nb-sparkline');
        if (!el) return;
        while (el.firstChild) { el.removeChild(el.firstChild); }

        var w = 120, h = 44;
        var r = Math.max(0, Math.min(100, rate));

        // 8 points: smoothstep from near-zero to r/100 of chart height
        var n = 8;
        var pts = [];
        for (var i = 0; i < n; i++) {
            var t    = i / (n - 1);
            var ease = t * t * (3 - 2 * t);          // smoothstep
            var wave = Math.sin(t * Math.PI * 1.5) * 0.08 * (1 - ease);
            var yFrac = ease * (r / 100) + wave;
            pts.push({ x: t * w, y: h - yFrac * h * 0.82 - h * 0.06 });
        }

        // Smooth bezier
        var d = 'M ' + pts[0].x.toFixed(1) + ' ' + pts[0].y.toFixed(1);
        for (var i = 1; i < pts.length; i++) {
            var cp1x = pts[i-1].x + (pts[i].x - pts[i-1].x) * 0.5;
            var cp2x = pts[i].x   - (pts[i].x - pts[i-1].x) * 0.5;
            d += ' C ' + cp1x.toFixed(1) + ' ' + pts[i-1].y.toFixed(1) +
                 ' ' + cp2x.toFixed(1) + ' ' + pts[i].y.toFixed(1) +
                 ' ' + pts[i].x.toFixed(1) + ' ' + pts[i].y.toFixed(1);
        }

        var fill = d + ' L ' + w + ' ' + h + ' L 0 ' + h + ' Z';
        var lineColor = r >= 30 ? '#00D4AA' : r >= 5 ? '#3DD6F5' : '#FF4D6D';

        var NS = 'http://www.w3.org/2000/svg';

        // Gradient fill
        var defs = document.createElementNS(NS, 'defs');
        var grad = document.createElementNS(NS, 'linearGradient');
        grad.setAttribute('id', 'spk-grad');
        grad.setAttribute('x1', '0%'); grad.setAttribute('y1', '0%');
        grad.setAttribute('x2', '0%'); grad.setAttribute('y2', '100%');
        var s1 = document.createElementNS(NS, 'stop');
        s1.setAttribute('offset', '0%');   s1.setAttribute('stop-color', lineColor); s1.setAttribute('stop-opacity', '0.3');
        var s2 = document.createElementNS(NS, 'stop');
        s2.setAttribute('offset', '100%'); s2.setAttribute('stop-color', lineColor); s2.setAttribute('stop-opacity', '0.01');
        grad.appendChild(s1); grad.appendChild(s2);
        defs.appendChild(grad);
        el.appendChild(defs);

        var area = document.createElementNS(NS, 'path');
        area.setAttribute('d', fill);
        area.setAttribute('fill', 'url(#spk-grad)');
        el.appendChild(area);

        var line = document.createElementNS(NS, 'path');
        line.setAttribute('d', d);
        line.setAttribute('fill', 'none');
        line.setAttribute('stroke', lineColor);
        line.setAttribute('stroke-width', '1.8');
        line.setAttribute('stroke-linecap', 'round');
        line.setAttribute('stroke-linejoin', 'round');
        el.appendChild(line);
    })($rate)
    """>]
    let private renderSparkline (rate: float) : unit = ()

    let updateSparkline (profile: BudgetProfile) : unit =
        renderSparkline (max 0.0 (min 100.0 (savingsRate profile)))
