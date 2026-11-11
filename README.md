# Media Centre Designer

A .NET 10 web app that designs a DIY media centre cabinet for the gear you own.

1. Pick devices (consoles, receivers, streamers, etc.) from the catalog or enter custom sizes (inches; decimals or fractions like `11 1/2`).
2. Choose materials (1/2", 3/4" plywood, back panel, sheet size, saw kerf) and layout preferences.
3. Get overall dimensions, a cut list, shopping list, sheet cutting layouts and IKEA-style step-by-step assembly illustrations. Use **Print / save as PDF** for a shop copy.

## Run

```bash
dotnet run --project src/MediaCentreDesigner.Web
dotnet test
```

## Design assumptions

- Devices go in rows (shelves) no wider than the max cabinet width, tallest row at the bottom.
- Carcass: two full-height sides, bottom, top and shelves between them, optional overlay back panel, optional plinth.
- Joinery is glue + pocket screws; the shelf-span sag check is a rule of thumb.
- Catalog sizes are approximate — measure your own gear.
