using System.Globalization;
using System.Text;

namespace MediaCentreDesigner.Services;

public record DrawBox(Box Box, string Fill, double Opacity = 1, bool Highlight = false, string? Label = null);

public record Arrow(double FromX, double FromY, double FromZ, double ToX, double ToY, double ToZ);

/// <summary>Oblique "cabinet projection" renderer producing IKEA-style line illustrations as SVG.</summary>
public static class Svg
{
    private const double Kx = 0.42, Ky = 0.26;

    private static (double x, double y) P(double x, double y, double z) => (x + y * Kx, -z - y * Ky);

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Esc(string s) => System.Net.WebUtility.HtmlEncode(s);

    public static string Shade(string hex, double f)
    {
        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);
        int C(int v) => (int)Math.Clamp(v * f, 0, 255);
        return $"#{C(r):x2}{C(g):x2}{C(b):x2}";
    }

    public static string Iso(IEnumerable<DrawBox> boxes, Arrow? arrow = null, string? callout = null, double width = 640)
    {
        var list = Order(boxes.ToList());
        var pts = new List<(double x, double y)>();
        foreach (var b in list)
            foreach (var x in new[] { b.Box.X, b.Box.X1 })
                foreach (var y in new[] { b.Box.Y, b.Box.Y1 })
                    foreach (var z in new[] { b.Box.Z, b.Box.Z1 })
                        pts.Add(P(x, y, z));
        if (arrow != null) { pts.Add(P(arrow.FromX, arrow.FromY, arrow.FromZ)); pts.Add(P(arrow.ToX, arrow.ToY, arrow.ToZ)); }

        var minX = pts.Min(p => p.x); var maxX = pts.Max(p => p.x);
        var minY = pts.Min(p => p.y); var maxY = pts.Max(p => p.y);
        var pad = Math.Max(maxX - minX, maxY - minY) * 0.06 + 2;
        var vb = $"{F(minX - pad)} {F(minY - pad)} {F(maxX - minX + 2 * pad)} {F(maxY - minY + 2 * pad)}";
        var sw = Math.Max(maxX - minX, maxY - minY) / 400;

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{vb}\" class=\"illustration\" role=\"img\">");
        sb.Append($"<g stroke=\"#222\" stroke-width=\"{F(sw)}\" stroke-linejoin=\"round\">");
        foreach (var b in list) Face(sb, b, sw);
        sb.Append("</g>");

        if (arrow != null)
        {
            var (fx, fy) = P(arrow.FromX, arrow.FromY, arrow.FromZ);
            var (tx, ty) = P(arrow.ToX, arrow.ToY, arrow.ToZ);
            var ang = Math.Atan2(ty - fy, tx - fx);
            var hs = sw * 9;
            var a1 = (tx - hs * Math.Cos(ang - 0.45), ty - hs * Math.Sin(ang - 0.45));
            var a2 = (tx - hs * Math.Cos(ang + 0.45), ty - hs * Math.Sin(ang + 0.45));
            sb.Append($"<g stroke=\"#d9480f\" fill=\"#d9480f\" stroke-width=\"{F(sw * 1.6)}\">");
            sb.Append($"<line x1=\"{F(fx)}\" y1=\"{F(fy)}\" x2=\"{F(tx)}\" y2=\"{F(ty)}\" stroke-dasharray=\"{F(sw * 5)} {F(sw * 3)}\"/>");
            sb.Append($"<polygon points=\"{F(tx)},{F(ty)} {F(a1.Item1)},{F(a1.Item2)} {F(a2.Item1)},{F(a2.Item2)}\"/></g>");
        }

        if (callout != null)
        {
            var hb = list.FirstOrDefault(b => b.Highlight);
            if (hb != null)
            {
                var c = hb.Box;
                var (cx, cy) = P(c.X1, c.Y, c.Z1);
                var r = Math.Max(maxX - minX, maxY - minY) * 0.035;
                sb.Append($"<g><circle cx=\"{F(cx)}\" cy=\"{F(cy - r * 1.4)}\" r=\"{F(r)}\" fill=\"#fff\" stroke=\"#d9480f\" stroke-width=\"{F(sw * 2)}\"/>");
                sb.Append($"<text x=\"{F(cx)}\" y=\"{F(cy - r * 1.4 + r * 0.38)}\" text-anchor=\"middle\" font-size=\"{F(r * 1.15)}\" font-weight=\"700\" fill=\"#d9480f\" font-family=\"sans-serif\">{Esc(callout)}</text></g>");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void Face(StringBuilder sb, DrawBox d, double sw)
    {
        var b = d.Box;
        var fill = d.Highlight ? "#ffd8a8" : d.Fill;
        var op = F(d.Opacity);
        string Poly(IEnumerable<(double, double)> pts, string color) =>
            $"<polygon points=\"{string.Join(' ', pts.Select(p => $"{F(p.Item1)},{F(p.Item2)}"))}\" fill=\"{color}\" fill-opacity=\"{op}\" stroke-opacity=\"{F(Math.Max(d.Opacity, 0.35))}\"/>";

        // Front (y = min), right (x = max) and top (z = max) faces are visible.
        sb.Append(Poly([P(b.X, b.Y, b.Z), P(b.X1, b.Y, b.Z), P(b.X1, b.Y, b.Z1), P(b.X, b.Y, b.Z1)], fill));
        sb.Append(Poly([P(b.X1, b.Y, b.Z), P(b.X1, b.Y1, b.Z), P(b.X1, b.Y1, b.Z1), P(b.X1, b.Y, b.Z1)], Shade(fill, 0.8)));
        sb.Append(Poly([P(b.X, b.Y, b.Z1), P(b.X1, b.Y, b.Z1), P(b.X1, b.Y1, b.Z1), P(b.X, b.Y1, b.Z1)], Shade(fill, 1.08)));

        if (d.Label != null)
        {
            var (cx, cy) = P(b.X + b.W / 2, b.Y, b.Z + b.H / 2);
            var size = Math.Clamp(Math.Min(b.W * 0.9 / Math.Max(d.Label.Length * 0.55, 1), b.H * 0.6), 0.1, 3);
            sb.Append($"<text x=\"{F(cx)}\" y=\"{F(cy + size * 0.35)}\" text-anchor=\"middle\" font-size=\"{F(size)}\" fill=\"#222\" stroke=\"none\" font-family=\"sans-serif\">{Esc(d.Label)}</text>");
        }
    }

    /// <summary>Back-to-front painter's ordering for a viewer at front-right-above.</summary>
    private static List<DrawBox> Order(List<DrawBox> boxes)
    {
        const double e = 1e-6;
        var n = boxes.Count;
        var behind = new List<int>[n];
        var indeg = new int[n];
        for (var i = 0; i < n; i++) behind[i] = [];

        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
            {
                var a = boxes[i].Box; var b = boxes[j].Box;
                int aBehind = 0, bBehind = 0;
                if (a.X1 <= b.X + e) aBehind++; else if (b.X1 <= a.X + e) bBehind++;
                if (a.Y >= b.Y1 - e) aBehind++; else if (b.Y >= a.Y1 - e) bBehind++;
                if (a.Z1 <= b.Z + e) aBehind++; else if (b.Z1 <= a.Z + e) bBehind++;
                if (aBehind > 0 && bBehind == 0) { behind[i].Add(j); indeg[j]++; }
                else if (bBehind > 0 && aBehind == 0) { behind[j].Add(i); indeg[i]++; }
            }

        double Key(DrawBox d) => (d.Box.X + d.Box.X1) / 2 + (d.Box.Z + d.Box.Z1) / 2 - (d.Box.Y + d.Box.Y1) / 2;
        var result = new List<DrawBox>();
        var done = new bool[n];
        while (result.Count < n)
        {
            var next = Enumerable.Range(0, n).Where(i => !done[i] && indeg[i] == 0).OrderBy(i => Key(boxes[i])).Cast<int?>().FirstOrDefault()
                       ?? Enumerable.Range(0, n).Where(i => !done[i]).OrderBy(i => Key(boxes[i])).First();
            done[next] = true;
            result.Add(boxes[next]);
            foreach (var j in behind[next]) indeg[j]--;
        }
        return result;
    }

    public static string SheetLayoutSvg(SheetLayout s, string title)
    {
        const double scale = 8;
        var w = s.Length * scale; var h = s.Width * scale;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"-4 -26 {F(w + 8)} {F(h + 34)}\" class=\"sheet\" role=\"img\">");
        sb.Append($"<text x=\"0\" y=\"-10\" font-size=\"14\" font-weight=\"700\" font-family=\"sans-serif\">{Esc(title)}</text>");
        sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"#f4e7d3\" stroke=\"#222\" stroke-width=\"2\"/>");
        foreach (var p in s.Pieces)
        {
            var px = p.X * scale; var py = p.Y * scale; var pw = p.Length * scale; var ph = p.Width * scale;
            sb.Append($"<rect x=\"{F(px)}\" y=\"{F(py)}\" width=\"{F(pw)}\" height=\"{F(ph)}\" fill=\"#ffd8a8\" stroke=\"#c2410c\" stroke-width=\"1.5\"/>");
            var fs = Math.Clamp(Math.Min(ph * 0.35, pw / 10), 7, 16);
            sb.Append($"<text x=\"{F(px + pw / 2)}\" y=\"{F(py + ph / 2 - 1)}\" text-anchor=\"middle\" font-size=\"{F(fs * 1.3)}\" font-weight=\"700\" font-family=\"sans-serif\">{Esc(p.Label)}</text>");
            sb.Append($"<text x=\"{F(px + pw / 2)}\" y=\"{F(py + ph / 2 + fs * 1.1)}\" text-anchor=\"middle\" font-size=\"{F(fs)}\" font-family=\"sans-serif\">{Esc(Units.Fmt(p.Length))} × {Esc(Units.Fmt(p.Width))}</text>");
        }
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Flat, labelled drawing of each unique part (used in the "parts" step).</summary>
    public static string PartsSvg(List<PartGroup> groups)
    {
        const double maxLen = 300;
        var longest = groups.Max(g => g.Length);
        var scale = maxLen / longest;
        var y = 10.0;
        var sb = new StringBuilder();
        var body = new StringBuilder();
        foreach (var g in groups)
        {
            var w = g.Length * scale; var h = Math.Max(g.Width * scale, 8);
            body.Append($"<rect x=\"60\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"#ffd8a8\" stroke=\"#c2410c\" stroke-width=\"1.5\"/>");
            body.Append($"<text x=\"30\" y=\"{F(y + h / 2 + 8)}\" text-anchor=\"middle\" font-size=\"24\" font-weight=\"700\" font-family=\"sans-serif\">{Esc(g.Letter)}</text>");
            body.Append($"<text x=\"{F(66 + w)}\" y=\"{F(y + h / 2 - 2)}\" font-size=\"12\" font-family=\"sans-serif\">{g.Qty}× {Esc(g.Name)}</text>");
            body.Append($"<text x=\"{F(66 + w)}\" y=\"{F(y + h / 2 + 12)}\" font-size=\"11\" fill=\"#555\" font-family=\"sans-serif\">{Esc(Units.Fmt(g.Length))} × {Esc(Units.Fmt(g.Width))} × {Esc(Units.Fmt(g.Thickness))}</text>");
            y += h + 22;
        }
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {F(60 + maxLen + 180)} {F(y)}\" class=\"illustration\" role=\"img\">{body}</svg>");
        return sb.ToString();
    }
}
