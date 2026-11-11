using MediaCentreDesigner.Services;

namespace MediaCentreDesigner.Tests;

public class DesignerTests
{
    private static DesignRequest Req(params DeviceInput[] d) => new([.. d]);

    [Fact]
    public void Fmt_RoundsToSixteenths()
    {
        Assert.Equal("23 1/2\"", Units.Fmt(23.5));
        Assert.Equal("3/4\"", Units.Fmt(0.75));
        Assert.Equal("12\"", Units.Fmt(12.01));
    }

    [Fact]
    public void Devices_FitInsideTheirShelves()
    {
        var c = CabinetLayout.Build(Req(
            new DeviceInput("PS5", 15.4, 4.1, 10.2), new DeviceInput("Xbox", 6, 11.9, 6), new DeviceInput("Receiver", 17.1, 6.9, 14.8), new DeviceInput("Soundbar", 40, 2.5, 4)));
        foreach (var d in c.Devices)
        {
            Assert.True(d.Box.X >= c.Materials.CarcassThickness - 1e-9);
            Assert.True(d.Box.X1 <= c.OuterW - c.Materials.CarcassThickness + 1e-9);
            Assert.True(d.Box.Y1 <= c.InteriorD + 1e-9);
            var shelf = c.Shelves[d.Shelf - 1];
            Assert.True(d.Box.H <= shelf.InteriorHeight);
        }
        var boxes = c.Devices.Select(d => d.Box).ToList();
        for (var i = 0; i < boxes.Count; i++)
            for (var j = i + 1; j < boxes.Count; j++)
                Assert.False(Overlaps(boxes[i], boxes[j]));
    }

    [Fact]
    public void RowsRespectMaxWidth()
    {
        var req = Req(new DeviceInput("A", 15, 4, 8, 4)) with { Options = new OptionsInput(MaxWidth: 36) };
        var c = CabinetLayout.Build(req);
        Assert.True(c.OuterW <= 36 + 1e-9);
        Assert.True(c.Shelves.Count >= 2);
    }

    [Fact]
    public void CutList_PartsMatchCabinetAndFitSheets()
    {
        var result = Designer.Generate(Req(new DeviceInput("PS5", 15.4, 4.1, 10.2), new DeviceInput("Receiver", 17.1, 6.9, 14.8, 2)));
        var pieces = result.Sheets.SelectMany(s => s.Pieces).ToList();
        Assert.Equal(result.Parts.Sum(p => p.Qty), pieces.Count);
        foreach (var s in result.Sheets)
            foreach (var p in s.Pieces)
            {
                Assert.True(p.X + p.Length <= s.Length + 1e-9);
                Assert.True(p.Y + p.Width <= s.Width + 1e-9);
            }
        Assert.Equal(result.SheetSvgs.Count, result.Sheets.Count);
        Assert.Contains(result.Steps, s => s.Title.Contains("back", StringComparison.OrdinalIgnoreCase));
        Assert.All(result.Steps, s => Assert.StartsWith("<svg", s.Svg));
    }

    [Fact]
    public void PackedPiecesDoNotOverlap()
    {
        var result = Designer.Generate(Req(new DeviceInput("A", 15, 4, 8, 6), new DeviceInput("B", 10, 12, 8, 3)));
        foreach (var s in result.Sheets)
            for (var i = 0; i < s.Pieces.Count; i++)
                for (var j = i + 1; j < s.Pieces.Count; j++)
                {
                    var a = s.Pieces[i]; var b = s.Pieces[j];
                    var overlap = a.X < b.X + b.Length && b.X < a.X + a.Length && a.Y < b.Y + b.Width && b.Y < a.Y + a.Width;
                    Assert.False(overlap, $"{a.Label} overlaps {b.Label}");
                }
    }

    [Fact]
    public void OversizedPartsAreReported()
    {
        var req = Req(new DeviceInput("Tall", 10, 100, 10)) with { Options = new OptionsInput(PlinthHeight: 0) };
        var result = Designer.Generate(req);
        Assert.Contains(result.Warnings, w => w.Contains("does not fit"));
    }

    [Fact]
    public void InvalidInput_Throws()
    {
        Assert.Throws<DesignException>(() => Designer.Generate(Req()));
        Assert.Throws<DesignException>(() => Designer.Generate(Req(new DeviceInput("Bad", 0, 1, 1))));
    }

    private static bool Overlaps(Box a, Box b) =>
        a.X < b.X1 - 1e-9 && b.X < a.X1 - 1e-9 && a.Y < b.Y1 - 1e-9 && b.Y < a.Y1 - 1e-9 && a.Z < b.Z1 - 1e-9 && b.Z < a.Z1 - 1e-9;
}
