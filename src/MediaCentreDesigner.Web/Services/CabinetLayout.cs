namespace MediaCentreDesigner.Services;

public record CabinetPart(string Letter, string Name, Box Box, string Material, string? Label = null);

public record PlacedDevice(string Name, Box Box, int Shelf);

public record Cabinet(
    MaterialInput Materials,
    OptionsInput Options,
    double OuterW, double OuterH, double OuterD,
    double InteriorW, double InteriorD,
    List<CabinetPart> Parts,
    List<PlacedDevice> Devices,
    List<Shelf> Shelves);

public static class CabinetLayout
{
    public const string CarcassMaterial = "carcass";
    public const string BackMaterial = "back";

    public static Cabinet Build(DesignRequest req)
    {
        var m = req.Materials ?? new MaterialInput();
        var o = req.Options ?? new OptionsInput();
        Validate(req, m, o);

        var items = req.Devices
            .SelectMany(d => Enumerable.Repeat(d, Math.Max(1, d.Qty)))
            .OrderByDescending(d => d.Width)
            .ToList();

        var t = m.CarcassThickness;
        var gap = o.ClearanceSide;
        var maxInner = Math.Max(o.MaxWidth - 2 * t, items.Max(d => d.Width) + 2 * gap);

        // First-fit decreasing into rows (one row per shelf).
        var rows = new List<List<DeviceInput>>();
        foreach (var d in items)
        {
            var row = rows.FirstOrDefault(r => RowWidth(r, gap) + d.Width + gap <= maxInner + 1e-9);
            if (row == null) rows.Add(row = []);
            row.Add(d);
        }

        // Tallest row at the bottom keeps the heavy/large gear low.
        rows = rows.OrderByDescending(r => r.Max(d => d.Height)).ToList();

        var innerW = Math.Max(rows.Max(r => RowWidth(r, gap)), 0);
        var innerD = items.Max(d => d.Depth) + o.ClearanceBack;
        var outerW = innerW + 2 * t;
        var rowHeights = rows.Select(r => r.Max(d => d.Height) + o.ClearanceTop).ToList();
        var plinth = Math.Max(0, o.PlinthHeight);
        var outerH = plinth + t + rowHeights.Sum() + (rows.Count - 1) * t + t;
        var hasBack = m.BackThickness > 0;
        var outerD = innerD + (hasBack ? m.BackThickness : 0);

        var parts = new List<CabinetPart>
        {
            new("A", "Left side", new Box(0, 0, 0, t, innerD, outerH), CarcassMaterial, "A1"),
            new("A", "Right side", new Box(outerW - t, 0, 0, t, innerD, outerH), CarcassMaterial, "A2"),
            new("B", "Bottom panel", new Box(t, 0, plinth, innerW, innerD, t), CarcassMaterial, "B1"),
        };

        var shelves = new List<Shelf>();
        var devices = new List<PlacedDevice>();
        var z = plinth + t;
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            shelves.Add(new Shelf(i + 1, z, rowHeights[i], row.Select(d => d.Name).ToList()));

            var used = row.Sum(d => d.Width);
            var space = (innerW - used) / (row.Count + 1);
            var x = t + space;
            foreach (var d in row.OrderBy(d => d.Name))
            {
                devices.Add(new PlacedDevice(d.Name, new Box(x, 0, z, d.Width, d.Depth, d.Height), i + 1));
                x += d.Width + space;
            }

            z += rowHeights[i];
            if (i < rows.Count - 1)
            {
                parts.Add(new CabinetPart("D", "Shelf", new Box(t, 0, z, innerW, innerD, t), CarcassMaterial, $"D{i + 1}"));
                z += t;
            }
        }

        parts.Add(new CabinetPart("C", "Top panel", new Box(t, 0, z, innerW, innerD, t), CarcassMaterial, "C1"));
        if (hasBack)
            parts.Add(new CabinetPart("E", "Back panel", new Box(0, innerD, 0, outerW, m.BackThickness, outerH), BackMaterial, "E1"));

        return new Cabinet(m, o, outerW, outerH, outerD, innerW, innerD, parts, devices, shelves);
    }

    private static double RowWidth(List<DeviceInput> row, double gap) =>
        row.Sum(d => d.Width) + gap * (row.Count + 1);

    private static void Validate(DesignRequest req, MaterialInput m, OptionsInput o)
    {
        if (req.Devices == null || req.Devices.Count == 0)
            throw new DesignException("Add at least one device.");
        foreach (var d in req.Devices)
        {
            if (string.IsNullOrWhiteSpace(d.Name)) throw new DesignException("Every device needs a name.");
            if (d.Width <= 0 || d.Height <= 0 || d.Depth <= 0)
                throw new DesignException($"'{d.Name}' needs width, height and depth greater than zero.");
            if (d.Qty is < 1 or > 20) throw new DesignException($"'{d.Name}' quantity must be between 1 and 20.");
            if (d.Width > 200 || d.Height > 200 || d.Depth > 200)
                throw new DesignException($"'{d.Name}' dimensions look too large (inches).");
        }
        if (req.Devices.Sum(d => d.Qty) > 60) throw new DesignException("Too many devices (max 60).");
        if (m.CarcassThickness is < 0.125 or > 2) throw new DesignException("Carcass thickness must be between 1/8\" and 2\".");
        if (m.BackThickness is < 0 or > 1) throw new DesignException("Back thickness must be between 0 and 1\".");
        if (m.SheetWidth <= 0 || m.SheetLength <= 0 || m.SheetWidth > m.SheetLength)
            throw new DesignException("Sheet size must be positive, with width no longer than length.");
        if (m.Kerf is < 0 or > 0.5) throw new DesignException("Saw kerf must be between 0 and 1/2\".");
        if (o.MaxWidth < 6 || o.MaxWidth > 240) throw new DesignException("Maximum width must be between 6\" and 240\".");
        if (o.ClearanceSide < 0 || o.ClearanceTop < 0 || o.ClearanceBack < 0 || o.PlinthHeight < 0 || o.MaxHeight < 0)
            throw new DesignException("Clearances and heights cannot be negative.");
    }
}
