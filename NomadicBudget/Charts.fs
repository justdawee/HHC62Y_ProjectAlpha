namespace NomadicBudget

open WebSharper
open WebSharper.JavaScript
open Domain

[<JavaScript>]
module Charts =

    // ── Doughnut ────────────────────────────────────────────────────────────
    // `host` is a plain <div>. We create a real SVG element inside it via
    // createElementNS (WebSharper's Doc.Element always uses createElement,
    // which puts <svg> in the HTML namespace and breaks rendering).

    [<Inline """
    (function(host, data, colors) {
        if (!host) return;

        var NS = 'http://www.w3.org/2000/svg';
        var size = 200, stroke = 22;
        var r    = (size - stroke) / 2;   // 89
        var cx   = size / 2, cy = size / 2;
        var circ = 2 * Math.PI * r;

        // Find or create the SVG root inside the host div
        var svg = host.querySelector('svg.nb-donut-svg');
        if (!svg) {
            svg = document.createElementNS(NS, 'svg');
            svg.setAttribute('class', 'nb-donut-svg');
            svg.setAttribute('width', '200');
            svg.setAttribute('height', '200');
            svg.setAttribute('viewBox', '0 0 200 200');
            host.appendChild(svg);
        }
        while (svg.firstChild) { svg.removeChild(svg.firstChild); }

        var sum = 0;
        for (var k = 0; k < data.length; k++) sum += data[k];
        if (sum <= 0) sum = 1;

        // Base (track) circle
        var base = document.createElementNS(NS, 'circle');
        base.setAttribute('cx', cx); base.setAttribute('cy', cy); base.setAttribute('r', r);
        base.setAttribute('fill', 'none');
        base.setAttribute('stroke', '#141B2D');
        base.setAttribute('stroke-width', stroke);
        svg.appendChild(base);

        var acc = 0;
        for (var i = 0; i < data.length; i++) {
            var frac   = data[i] / sum;
            var len    = frac * circ;
            var dash   = len + ' ' + (circ - len);
            var offset = (-acc * circ).toFixed(4);
            var seg = document.createElementNS(NS, 'circle');
            seg.setAttribute('cx', cx); seg.setAttribute('cy', cy); seg.setAttribute('r', r);
            seg.setAttribute('fill', 'none');
            seg.setAttribute('stroke', colors[i]);
            seg.setAttribute('stroke-width', stroke);
            seg.setAttribute('stroke-dasharray', dash);
            seg.setAttribute('stroke-dashoffset', offset);
            seg.setAttribute('transform', 'rotate(-90 ' + cx + ' ' + cy + ')');
            seg.style.transition =
                'stroke-dasharray 400ms cubic-bezier(0.22,1,0.36,1),' +
                'stroke-dashoffset 400ms cubic-bezier(0.22,1,0.36,1)';
            svg.appendChild(seg);
            acc += frac;
        }
    })($host, $data, $colors)
    """>]
    let private renderDonutEl (host: Dom.Element) (data: float[]) (colors: string[]) : unit = ()

    let updateEl (host: Dom.Element) (profile: BudgetProfile) : unit =
        let data   = allCategories |> List.map (fun cat -> float (categoryValue profile cat)) |> Array.ofList
        let colors = allCategories |> List.map categoryColor |> Array.ofList
        renderDonutEl host data colors

    // ── Sparkline ───────────────────────────────────────────────────────────
    // Same pattern: `host` is a <div>, we mount an SVG inside it via createElementNS.

    [<Inline """
    (function(host, ratio) {
        if (!host) return;

        var NS = 'http://www.w3.org/2000/svg';

        var svg = host.querySelector('svg.nb-spark-svg');
        if (!svg) {
            svg = document.createElementNS(NS, 'svg');
            svg.setAttribute('class', 'nb-spark-svg');
            svg.setAttribute('viewBox', '0 0 200 80');
            svg.setAttribute('preserveAspectRatio', 'none');
            svg.style.width = '100%';
            svg.style.height = '100%';
            svg.style.display = 'block';
            host.appendChild(svg);
        }
        while (svg.firstChild) { svg.removeChild(svg.firstChild); }

        var r   = Math.max(-0.6, Math.min(0.8, ratio));
        var pts = [];
        for (var i = 0; i <= 20; i++) {
            var x = i / 20;
            var y = 0.55 - r * 0.45 * (0.4 + 0.6 * x) + Math.sin(x * 6 + r * 2) * 0.05;
            pts.push([x * 200, y * 80]);
        }

        var path = pts.map(function(p, i) {
            return (i === 0 ? 'M' : 'L') + p[0].toFixed(1) + ',' + p[1].toFixed(1);
        }).join(' ');

        var fill  = path + ' L200,80 L0,80 Z';
        var color = r >= 0 ? '#3DD6F5' : '#FF4D6D';

        var defs = document.createElementNS(NS, 'defs');
        var grad = document.createElementNS(NS, 'linearGradient');
        grad.setAttribute('id', 'sp-g');
        grad.setAttribute('x1', '0'); grad.setAttribute('y1', '0');
        grad.setAttribute('x2', '0'); grad.setAttribute('y2', '1');
        var s1 = document.createElementNS(NS, 'stop');
        s1.setAttribute('offset', '0%');   s1.setAttribute('stop-color', color); s1.setAttribute('stop-opacity', '0.35');
        var s2 = document.createElementNS(NS, 'stop');
        s2.setAttribute('offset', '100%'); s2.setAttribute('stop-color', color); s2.setAttribute('stop-opacity', '0');
        grad.appendChild(s1); grad.appendChild(s2);
        defs.appendChild(grad);
        svg.appendChild(defs);

        var area = document.createElementNS(NS, 'path');
        area.setAttribute('d', fill);
        area.setAttribute('fill', 'url(#sp-g)');
        svg.appendChild(area);

        var line = document.createElementNS(NS, 'path');
        line.setAttribute('d', path);
        line.setAttribute('fill', 'none');
        line.setAttribute('stroke', color);
        line.setAttribute('stroke-width', '2');
        line.setAttribute('stroke-linecap', 'round');
        line.setAttribute('stroke-linejoin', 'round');
        svg.appendChild(line);
    })($host, $ratio)
    """>]
    let private renderSparklineEl (host: Dom.Element) (ratio: float) : unit = ()

    let updateSparklineEl (host: Dom.Element) (profile: BudgetProfile) : unit =
        let ratio =
            if profile.MonthlyIncome = 0.0<usd> then 0.0
            else float (netSavings profile) / float profile.MonthlyIncome
        renderSparklineEl host ratio
