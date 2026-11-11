namespace MediaCentreDesigner.Services;

public static class CutList
{
    public static List<PartGroup> Groups(Cabinet c)
    {
        return c.Parts
            .Select(p =>
            {
                var t = p.Material == CabinetLayout.BackMaterial ? c.Materials.BackThickness : c.Materials.CarcassThickness;
                var dims = new[] { p.Box.W, p.Box.D, p.Box.H }.OrderBy(v => v).ToArray();
                // Thickness is the smallest dimension; length/width are the other two (grain runs along length).
                return (p, t, len: dims[2], wid: dims[1]);
            })
            .GroupBy(x => (x.p.Letter, x.p.Name == "Right side" || x.p.Name == "Left side" ? "Side" : x.p.Name,
                Len: Math.Round(x.len, 4), Wid: Math.Round(x.wid, 4), x.p.Material))
            .Select(g => new PartGroup(g.Key.Letter, g.Key.Item2, g.Count(), g.First().len, g.First().wid, g.First().t, g.Key.Material))
            .OrderBy(g => g.Letter)
            .ToList();
    }

    public static List<string> OversizeWarnings(List<PartGroup> groups, MaterialInput m) =>
        groups
            .Where(g => g.Length > m.SheetLength + 1e-9 || g.Width > m.SheetWidth + 1e-9)
            .Select(g => $"{g.Name} ({Units.Fmt(g.Length)} × {Units.Fmt(g.Width)}) does not fit on a " +
                         $"{Units.Fmt(m.SheetWidth)} × {Units.Fmt(m.SheetLength)} sheet. Reduce the maximum width/height or splice the panel.")
            .ToList();

    public static List<SheetLayout> Pack(List<PartGroup> groups, MaterialInput m)
    {
        var sheets = new List<SheetLayout>();
        foreach (var material in new[] { CabinetLayout.CarcassMaterial, CabinetLayout.BackMaterial })
        {
            var pieces = groups
                .Where(g => g.Material == material && g.Length <= m.SheetLength + 1e-9 && g.Width <= m.SheetWidth + 1e-9)
                .SelectMany(g => Enumerable.Range(1, g.Qty).Select(i => (g, label: g.Qty == 1 ? g.Letter + "1" : g.Letter + i)))
                .OrderByDescending(p => p.g.Width).ThenByDescending(p => p.g.Length)
                .ToList();
            sheets.AddRange(PackMaterial(material, pieces, m));
        }
        return sheets;
    }

    private class Strip(double y, double height) { public double Y = y; public double Height = height; public double Used; }
    private class Sheet { public List<Strip> Strips = []; public List<PlacedPiece> Pieces = []; public double NextY; }

    private static List<SheetLayout> PackMaterial(string material, List<(PartGroup g, string label)> pieces, MaterialInput m)
    {
        var sheets = new List<Sheet>();
        foreach (var (g, label) in pieces)
        {
            Sheet? sheet = null;
            Strip? strip = null;
            foreach (var s in sheets)
            {
                strip = s.Strips.FirstOrDefault(st => st.Height >= g.Width - 1e-9 && st.Used + g.Length <= m.SheetLength + 1e-9);
                if (strip != null) { sheet = s; break; }
            }
            if (strip == null)
            {
                foreach (var s in sheets)
                {
                    if (s.NextY + g.Width <= m.SheetWidth + 1e-9)
                    {
                        sheet = s;
                        strip = new Strip(s.NextY, g.Width);
                        s.Strips.Add(strip);
                        s.NextY += g.Width + m.Kerf;
                        break;
                    }
                }
            }
            if (strip == null)
            {
                sheet = new Sheet();
                sheets.Add(sheet);
                strip = new Strip(0, g.Width);
                sheet.Strips.Add(strip);
                sheet.NextY = g.Width + m.Kerf;
            }
            sheet!.Pieces.Add(new PlacedPiece(label, g.Letter, strip.Used, strip.Y, g.Length, g.Width));
            strip.Used += g.Length + m.Kerf;
        }

        return sheets.Select((s, i) => new SheetLayout(
            material, i + 1, m.SheetWidth, m.SheetLength, s.Pieces,
            s.Pieces.Sum(p => p.Length * p.Width) / (m.SheetWidth * m.SheetLength))).ToList();
    }
}
