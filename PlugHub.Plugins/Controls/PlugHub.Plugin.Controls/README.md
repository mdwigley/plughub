
# PlugHub.Plugin.Controls

The `PlugHub.Plugin.Controls` library is a curated collection of official, reusable controls provided for development within the PlugHub environment. These controls follow consistent theming conventions to ensure visual harmony and flexibility, serving as building blocks for plugin and core UI components that adopt PlugHub’s design language.

## Purpose

`PlugHub.Plugin.Controls` offers a collection of officially maintained controls that demonstrate best practices in theming, composition, and visual consistency. These controls serve as models for how to approach control development within the PlugHub environment and provide well-structured examples of theme flexibility and modular design.

Key goals:
- Establish a **unified control architecture** grounded in theming and composition.
- Encourage **theme family separation** so each visual identity (e.g., FluentAvalonia, MaterialYou) can define its own look and behavior variants.
- Maintain clean **control logic vs. visual representation separation** through styles, templates, and resource organization.

## Concept Overview

PlugHub.Plugin.Controls separates control logic and visuals in a way that directly informs development practices:

- Each control has a C# class for core behavior and properties.
- Visual representation is defined through dedicated XAML files organized per theme family.
- The folder structure explicitly directs where to put specific resources:
  - `Controls/Themes/` holds control-specific templates and theme mappings.
  - `Controls/Styles/` contains control-specific styles augmenting those templates.
  - Shared styles and templates live in the root-level `Styles/` and `Templates/` folders of each theme family.

This layout is intentional and mirrors Avalonia’s control and theming design, providing a clear, discoverable map of where each piece belongs. By following this structure, developers naturally implement controls and themes consistently without guesswork, supporting modularity and maintainability while allowing flexible theme switching without changing control logic.

## Idealized File Structure
```
PlugHub.Plugin.Controls
│   PlugHub.Plugin.Controls.csproj
│   PlugHubControls.cs
│
├───Controls
│       ContentDeck.cs
│       ResizeBox.cs
│       RotationView.cs
│
├───Interfaces
│   └───Controls
│           IContentItem.cs
│
└───Themes
    ├───FluentAvalonia
    │   │   Theme.axaml
    │   │   Style.axaml
    │   │
    │   ├───Controls
    │   │   ├───Styles
    │   │   │       ContentDeck.axaml
    │   │   │
    │   │   └───Themes
    │   │           ContentDeck.axaml
    │   │           RotationView.axaml
    │   │           ResizeBox.axaml
    │   │
    │   ├───Styles
    │   │       Dark.axaml
    │   │       Light.axaml
    │   │
    │   └───Templates
    │           DeckTransitionTemplate.axaml
    │           SharedButtonTemplates.axaml
    │
    ├───MaterialYou
    │   │   Theme.axaml
    │   │   Style.axaml
    │   │
    │   ├───Controls
    │   │   ├───Styles
    |   │   │   
    │   │   └───Themes
    │   │           RotationView.axaml
    │   │           ResizeBox.axaml
    │   │
    │   ├───Styles
    │   │       Typography.axaml
    │   │
    │   └───Templates
    │           SharedSurfaces.axaml
    │
    └───Generic
        │   Theme.axaml
        │   Style.axaml
        │
        ├───Controls
        │   ├───Styles
        │   │
        │   └───Themes
        │           RotationView.axaml
        │           ResizeBox.axaml
        │
        ├───Styles
        │       Defaults.axaml
        │
        └───Templates
                BaseContainer.axaml
```
## Folder Breakdown

### Controls
Contains all C# control definitions.  
Controls here represent the logic layer only — they have no direct visual implementation beyond minimal functional structure.

### Interfaces
Houses shared control contracts and behavior patterns.  
`IContentItem` is an example interface for dynamic control state changes.

### Themes
Defines theme families. Each family (e.g., *FluentAvalonia*, *MaterialYou*, *Generic*) comprises:

- **Theme.axaml** – The root entry point that merges all control-specific and shared resources.  
- **Style.axaml** (optional) – Brush and color token definition for that family.  
- **Controls/** – Per-control theming.
  - **Styles/** – Per-control styling and visual state definitions.
  - **Themes/** – Control-level template mappings and style keys.
- **Styles/** – Shared, reusable style dictionaries for text, color systems, and visual tokens.
- **Templates/** – Shared control templates used across multiple controls in that family.

This design allows both *control-level customization* (in `Controls/`) and *theme family cohesion* (in root `Styles` and `Templates`).

## Example Controls

- **ResizeBox** – A generic, minimally-themed resizable border with an exposed thumb.  
- **RotationView** – A transform container rotating its content via a `Rotation` property.  
- **ContentDeck** – A composite deck control that shows PlugHub’s intended separation model:
  - Logic: `Controls/ContentDeck.cs`
  - Theming: `Themes/{Family}/Controls/Themes/ContentDeck.axaml`
  - Style customization: `Themes/{Family}/Controls/Styles/ContentDeck.axaml`

## Extending the Library

When building new controls:

1. Add control logic in `Controls/`.
2. Create per-theme resources in:
   - `Themes/{Family}/Controls/Styles/`
   - `Themes/{Family}/Controls/Themes/`
3. Define reusable globals as appropriate in:
   - `Themes/{Family}/Styles/`
   - `Themes/{Family}/Templates/`
4. Merge resources into that family’s `Theme.axaml` and `Style.axaml`.
