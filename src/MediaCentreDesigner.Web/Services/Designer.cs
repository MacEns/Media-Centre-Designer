namespace MediaCentreDesigner.Services;

public static class Designer
{
    private const string Wood = "#e8c99b";
    private static readonly string[] DeviceColors = ["#6c8ebf", "#82b366", "#b85450", "#9673a6", "#d6b656", "#5f9ea0", "#8c8c8c"];

    public static DesignResult Generate(DesignRequest req)
    {
        var c = CabinetLayout.Build(req);
        var m = c.Materials;
        var groups = CutList.Groups(c);
        var warnings = CutList.OversizeWarnings(groups, m);
        var sheets = CutList.Pack(groups, m);

        var sheetSvgs = sheets.Select(s =>
        {
            var count = sheets.Count(x => x.Material == s.Material);
            var thick = s.Material == CabinetLayout.BackMaterial ? m.BackThickness : m.CarcassThickness;
            return Svg.SheetLayoutSvg(s, $"{Units.Fmt(thick)} sheet {s.Index} of {count} — {s.Utilization:P0} used");
        }).ToList();

        AddWarnings(c, warnings);

        var shelfCount = c.Shelves.Count;
        var summary = new DesignSummary(c.OuterW, c.OuterH, c.OuterD, c.InteriorW, c.InteriorD, shelfCount, c.Shelves);
        var steps = BuildSteps(c, groups, sheets);
        var overview = Svg.Iso(FinalBoxes(c, ghostFront: true));

        return new DesignResult(summary, warnings, groups, sheets, sheetSvgs, Hardware(c, groups), steps, overview);
    }

    private static void AddWarnings(Cabinet c, List<string> warnings)
    {
        var t = c.Materials.CarcassThickness;
        var maxSpan = t < 0.7 ? 24 : t < 0.95 ? 32 : 36;
        if (c.InteriorW > maxSpan)
            warnings.Add($"Shelf span of {Units.Fmt(c.InteriorW)} may sag with {Units.Fmt(t)} material (suggested max {maxSpan}\"). " +
                         "Use thicker material, reduce the maximum width, or glue a 1×2 stiffener to the front edge of each shelf.");
        if (c.Options.MaxHeight > 0 && c.OuterH > c.Options.MaxHeight)
            warnings.Add($"Cabinet height {Units.Fmt(c.OuterH)} exceeds your limit of {Units.Fmt(c.Options.MaxHeight)}. " +
                         "Increase the maximum width so devices fit in fewer rows, or reduce clearances/plinth.");
        if (c.OuterH > 60)
            warnings.Add("This is a tall cabinet. Anchor it to the wall with an anti-tip bracket.");
        if (c.Materials.BackThickness <= 0)
            warnings.Add("No back panel selected. The cabinet will be less rigid; add diagonal bracing or a back.");
    }

    private static List<string> Hardware(Cabinet c, List<PartGroup> groups)
    {
        var horizontals = c.Shelves.Count + 1;
        var perJoint = Math.Max(2, (int)Math.Ceiling(c.InteriorD / 6));
        var screws = horizontals * 2 * perJoint;
        var list = new List<string>
        {
            $"{screws + 4} × pocket-hole screws (or 2\" wood screws) for carcass joints",
            "Wood glue (1 bottle)",
            "Sandpaper 120 and 220 grit, finish of your choice",
            "Anti-tip wall bracket (1)",
        };
        if (c.Materials.BackThickness > 0)
        {
            var perimeter = 2 * (c.OuterW + c.OuterH);
            list.Insert(1, $"{(int)Math.Ceiling(perimeter / 6) + c.Shelves.Count * 2}× 1\" brad nails or #6 × 3/4\" screws for the back panel");
        }
        if (c.Options.PlinthHeight > 0)
            list.Add($"Optional: {Units.Fmt(c.Options.PlinthHeight)} toe-kick strip (cut from offcuts) or 4 leveling feet");
        return list;
    }

    private static List<DrawBox> FinalBoxes(Cabinet c, bool ghostFront)
    {
        var boxes = new List<DrawBox>();
        foreach (var p in c.Parts)
            boxes.Add(new DrawBox(p.Box, Wood, p.Name == "Right side" && ghostFront ? 0.15 : 1));
        var i = 0;
        foreach (var d in c.Devices)
            boxes.Add(new DrawBox(d.Box, DeviceColors[i++ % DeviceColors.Length], 1, false, d.Name));
        return boxes;
    }

    private static List<Step> BuildSteps(Cabinet c, List<PartGroup> groups, List<SheetLayout> sheets)
    {
        var steps = new List<Step>();
        var t = c.Materials.CarcassThickness;
        void Add(string title, string text, string svg) => steps.Add(new Step(steps.Count + 1, title, text, svg));

        Add("Cut your parts",
            $"Cut all panels following the cut list and sheet layouts ({sheets.Count} sheet(s) in total). " +
            "Keep the grain running along each part's length. Lightly sand all edges. If using pocket screws, drill pocket holes on the underside of A and the ends of B, C and D as shown in the next steps.",
            Svg.PartsSvg(groups));

        var placed = new List<DrawBox>();
        CabinetPart P(string name) => c.Parts.First(p => p.Name == name);

        void Place(CabinetPart part, string title, string text, (double dx, double dy, double dz) off)
        {
            var b = part.Box;
            var moved = new Box(b.X + off.dx, b.Y + off.dy, b.Z + off.dz, b.W, b.D, b.H);
            var all = new List<DrawBox>(placed) { new(moved, Wood, 1, true) };
            Arrow? arrow = null;
            if (off != (0, 0, 0))
            {
                var len = Math.Sqrt(off.dx * off.dx + off.dy * off.dy + off.dz * off.dz);
                var k = 0.7;
                arrow = new Arrow(
                    b.X + b.W / 2 + off.dx, b.Y + b.D / 2 + off.dy, b.Z + b.H / 2 + off.dz,
                    b.X + b.W / 2 + off.dx * (1 - k), b.Y + b.D / 2 + off.dy * (1 - k), b.Z + b.H / 2 + off.dz * (1 - k));
            }
            Add(title, text, Svg.Iso(all, arrow, part.Label ?? part.Letter));
            placed.Add(new DrawBox(b, Wood));
        }

        var marks = string.Join(", ", c.Shelves.Skip(1).Select(s => $"shelf {s.Index - 1}: {Units.Fmt(s.Z - t)}"));
        var left = P("Left side");
        Place(left, "Stand the left side (A1) upright",
            $"Lay panel A1 on its edge with the front facing you. Mark the underside of every horizontal panel on the inside face, measured from the floor edge: " +
            $"bottom panel: {Units.Fmt(c.Options.PlinthHeight)}" + (marks.Length > 0 ? ", " + marks : "") +
            $", top panel: {Units.Fmt(c.OuterH - t)} (all to the underside of each panel).",
            (0, 0, 0));

        Place(P("Bottom panel"), "Attach the bottom panel (B1)",
            $"Apply glue to the end of B1 and fasten it to A1 with pocket screws, keeping the front edges flush. B1 sits {Units.Fmt(c.Options.PlinthHeight)} above the floor edge of A1.",
            (0, 0, 10));

        foreach (var part in c.Parts.Where(p => p.Name == "Shelf"))
            Place(part, $"Attach shelf {part.Label}",
                $"Glue and screw {part.Label} to A1 with its underside at {Units.Fmt(part.Box.Z)} from the floor edge. Check that it is square to A1 and flush with the front edge. " +
                $"Clear opening below: {Units.Fmt(c.Shelves[int.Parse(part.Label![1..]) - 1].InteriorHeight)}.",
                (0, 0, 10));

        Place(P("Top panel"), "Attach the top panel (C1)",
            "Glue and screw C1 to A1, flush with the front edge and the top of the side.", (0, 0, 10));

        Place(P("Right side"), "Attach the right side (A2)",
            "Glue the free ends of B1, every shelf D and C1, then lower A2 on and fasten it with pocket screws, working from the bottom up. " +
            "Check diagonals are equal so the cabinet is square.",
            (10, 0, 0));

        var back = c.Parts.FirstOrDefault(p => p.Name == "Back panel");
        if (back != null)
            Place(back, "Fit the back panel (E1)",
                "Lay the cabinet on its front. Apply glue to the rear edges, set E1 on, and use it to square the cabinet. " +
                $"Fasten every 6\" around the perimeter and into each shelf edge. Overall depth is now {Units.Fmt(c.OuterD)}.",
                (0, 10, 0));

        var finalBoxes = FinalBoxes(c, ghostFront: true);
        Add("Finish and fill",
            $"Sand, finish and let it cure. Move the cabinet into place, level it, and anchor it to the wall. " +
            "Slide in your devices (the right side is shown transparent). Leave the rear gap for cables and airflow. " +
            string.Join("; ", c.Shelves.Select(s => $"Shelf {s.Index}: {string.Join(", ", s.Devices)}")) + ".",
            Svg.Iso(finalBoxes));

        return steps;
    }
}
