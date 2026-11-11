namespace MediaCentreDesigner.Services;

public record CatalogItem(string Category, string Name, double Width, double Height, double Depth);

/// <summary>Approximate footprints (inches) in their usual shelf orientation. Always verify against your own unit.</summary>
public static class DeviceCatalog
{
    public static readonly IReadOnlyList<CatalogItem> Items =
    [
        new("Game console", "PlayStation 5 (horizontal)", 15.4, 4.1, 10.2),
        new("Game console", "PlayStation 5 Slim (horizontal)", 14.4, 3.6, 8.9),
        new("Game console", "Xbox Series X (standing)", 6, 11.9, 6),
        new("Game console", "Xbox Series S", 10.8, 2.5, 6),
        new("Game console", "Nintendo Switch dock", 6.8, 4.1, 2.1),
        new("Game console", "Retro console (SNES-size)", 9, 2.5, 8),
        new("Streaming", "Apple TV 4K", 3.9, 1.2, 3.9),
        new("Streaming", "Roku Ultra", 4.9, 1.4, 4.9),
        new("Streaming", "Cable / satellite box", 12, 2.2, 8),
        new("Audio / Video", "AV receiver", 17.1, 6.9, 14.8),
        new("Audio / Video", "Soundbar (40\")", 40, 2.5, 4),
        new("Audio / Video", "4K Blu-ray player", 17, 2.2, 8.5),
        new("Audio / Video", "Turntable", 17.1, 5, 14),
        new("Audio / Video", "Center-channel speaker", 24, 8, 10),
        new("Computing", "Mac mini / mini PC", 7.7, 1.4, 7.7),
        new("Computing", "NAS (2-bay)", 6.5, 6.5, 9),
        new("Network", "Wi-Fi router", 11, 2.5, 8),
        new("Network", "Cable modem", 5, 7, 5),
        new("Network", "Network switch (8-port)", 8, 1.5, 5),
        new("Other", "Power conditioner / UPS", 17, 3.5, 12),
    ];
}
