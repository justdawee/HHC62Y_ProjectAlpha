namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open Domain

[<JavaScript>]
module Charts =

    // Render an inline SVG doughnut into #nb-donut.
    // Uses arc-path geometry; safe to call before the element exists (no-ops then).
    [<Inline """
    (function(data, colors) {
        var el = document.getElementById('nb-donut');
        if (!el) return;

        // Clear previous render
        while (el.firstChild) { el.removeChild(el.firstChild); }

        var total = 0;
        for (var k = 0; k < data.length; k++) { total += data[k]; }
        if (total <= 0) return;

        var NS = 'http://www.w3.org/2000/svg';
        var cx = 100, cy = 100, R = 85, r = 52;
        var angle = 0;

        for (var i = 0; i < data.length; i++) {
            if (data[i] <= 0) { continue; }
            var frac = data[i] / total;
            var end  = angle + frac * 2 * Math.PI;
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
            p.setAttribute('opacity', '0.92');
            el.appendChild(p);
            angle = end;
        }

        // Centre label: total spend
        var t1 = document.createElementNS(NS, 'text');
        t1.setAttribute('x', '100');
        t1.setAttribute('y', '97');
        t1.setAttribute('text-anchor', 'middle');
        t1.setAttribute('dominant-baseline', 'middle');
        t1.setAttribute('fill', '#EEF3FF');
        t1.setAttribute('font-family', 'Syne, sans-serif');
        t1.setAttribute('font-weight', '800');
        t1.setAttribute('font-size', '17');
        t1.textContent = '$' + Math.round(total);
        el.appendChild(t1);

        var t2 = document.createElementNS(NS, 'text');
        t2.setAttribute('x', '100');
        t2.setAttribute('y', '114');
        t2.setAttribute('text-anchor', 'middle');
        t2.setAttribute('fill', '#7A8DAF');
        t2.setAttribute('font-family', 'DM Mono, monospace');
        t2.setAttribute('font-size', '9');
        t2.textContent = 'total spend';
        el.appendChild(t2);
    })($data, $colors)
    """>]
    let private renderDonut (data: float[]) (colors: string[]) : unit = ()

    let update (profile: BudgetProfile) : unit =
        let data   = allCategories |> List.map (fun cat -> float (categoryValue profile cat)) |> Array.ofList
        let colors = allCategories |> List.map categoryColor |> Array.ofList
        renderDonut data colors
