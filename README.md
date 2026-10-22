# AutoCAD / GstarCAD / CADmate Layout Template Plugin

This project is a starter plugin for automating layout-template setup in CAD hosts that expose an AutoCAD-compatible .NET API.

It is designed to work as a cross-host plugin for:

- AutoCAD
- GstarCAD
- CADmate

## What it does

- Reads a source drawing that contains the correct layout/page-setup configuration
- Applies that layout configuration to selected layouts in the active drawing
- Lets the user choose either all layouts or selected layouts by name
- Can be run as a command after loading the DLL with `NETLOAD`

## Host compatibility notes

The three CAD products use similar .NET APIs, but their assembly names and namespaces are slightly different.

This project is prepared for multi-host compilation by selecting the target host in the project file:

- `-p:CADHost=autocad`
- `-p:CADHost=gstarcad`
- `-p:CADHost=cadmate`

To compile successfully, update the install paths to match your actual CAD installation folder.

## Typical command

After loading the plugin with `NETLOAD`, run:

- `LAYOUTTEMPLATE`

Then enter:

1. Source template drawing path
2. Whether to apply to all layouts or only selected ones
3. Layout names (if needed)

## Files

- `LayoutTemplatePlugin/LayoutTemplatePlugin.csproj`
- `LayoutTemplatePlugin/LayoutTemplateExtension.cs`

## Important

The exact DLL names can differ slightly between CAD versions and vendors. The project file is arranged so you only need to adjust the install path and host-specific assembly names for your environment.
