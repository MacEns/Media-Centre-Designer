namespace MediaCentreDesigner.Services;

/// <summary>All dimensions are in inches.</summary>
public record DeviceInput(string Name, double Width, double Height, double Depth, int Qty = 1);

public record MaterialInput(
    double CarcassThickness = 0.75,
    double BackThickness = 0.25,
    double SheetWidth = 48,
    double SheetLength = 96,
    double Kerf = 0.125);

public record OptionsInput(
    double MaxWidth = 36,
    double ClearanceSide = 1,
    double ClearanceTop = 2,
    double ClearanceBack = 2,
    double PlinthHeight = 4,
    double MaxHeight = 0);

public record DesignRequest(
    List<DeviceInput> Devices,
    MaterialInput? Materials = null,
    OptionsInput? Options = null);

/// <summary>An axis-aligned box. x = width (left→right), y = depth (front→back), z = height (up).</summary>
public record Box(double X, double Y, double Z, double W, double D, double H)
{
    public double X1 => X + W;
    public double Y1 => Y + D;
    public double Z1 => Z + H;
}

public record PartGroup(
    string Letter,
    string Name,
    int Qty,
    double Length,
    double Width,
    double Thickness,
    string Material);

public record PlacedPiece(string Label, string Letter, double X, double Y, double Length, double Width);

public record SheetLayout(string Material, int Index, double Width, double Length, List<PlacedPiece> Pieces, double Utilization);

public record Shelf(int Index, double Z, double InteriorHeight, List<string> Devices);

public record Step(int Number, string Title, string Text, string Svg);

public record DesignSummary(
    double OuterWidth, double OuterHeight, double OuterDepth,
    double InteriorWidth, double InteriorDepth,
    int ShelfCount, List<Shelf> Shelves);

public record DesignResult(
    DesignSummary Summary,
    List<string> Warnings,
    List<PartGroup> Parts,
    List<SheetLayout> Sheets,
    List<string> SheetSvgs,
    List<string> Hardware,
    List<Step> Steps,
    string OverviewSvg);

public class DesignException(string message) : Exception(message);
