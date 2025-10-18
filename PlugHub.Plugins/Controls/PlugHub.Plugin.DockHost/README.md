# PlugHub.Plugin.DockHost

This is the **DockHost plugin** for the PlugHub platform.  
PlugHub.Plugin.DockHost provides an extensible docking and panel orchestration framework for Avalonia-based applications.  
It is designed for production use and serves as the foundation for building complex, IDE-style user interfaces with dockable panels, tabbed documents, and flexible layouts.

---

## About

PlugHub.Plugin.DockHost delivers a full docking system that allows developers to create rich, modular UIs with panels that can be pinned, unpinned, closed, reordered, and rearranged via drag-and-drop.  
It integrates seamlessly with the PlugHub plugin ecosystem, exposing services, styles, and descriptors that make it easy to extend and customize.

### What does it provide?

- **Docking framework**: Core services (`IDockService`) and controls for managing dockable panels and hosts.
- **Panel descriptors**: Declarative `DockPanelDescriptor`s that register panels (e.g., Characters, Quests, Inventory, Console) with targeted hosts.
- **Demo implementation**: `DockHostDemo` plugin showcasing how to register panels, pages, and views against the DockHost system.
- **Drag-drop support**: Behaviors (`DraggableTabItemBehavior`, `TabReorderBehavior`) enabling tab reordering and panel movement.
- **Content switching**: `ContentDeck` control with routed events for opening, closing, active content changes, and item reordering.
- **Style and theming**: Fluent-style resources included via `Generic.axaml` for consistent look and feel.
- **Extensible architecture**: Designed to be extended with custom panels, services, and themes.

### Who should use this?

- **Application developers** building complex UIs that need dockable, resizable, and rearrangeable panels.
- **Plugin authors** who want to contribute new panels or extend the docking system within the PlugHub ecosystem.
- **Teams** looking for a production-ready docking framework similar to those found in IDEs and professional tools.

---

## License

This plugin is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.  
See the [LICENSE](LICENSE) file in this directory for full details.

---

## Resources

- **Documentation**: [PlugHub Wiki](https://github.com/enterlucent/plughub/wiki/)  
- **Support**: [Support Portal](https://support.enterlucent.com/plughub/)  
- **Contact**: contact@enterlucent.com  
- **Changelog**: [Releases](https://github.com/enterlucent/plughub/releases/)

---

PlugHub.Plugin.DockHost is part of the official PlugHub repository and demonstrates how to build extensible, production-grade docking systems within the modular PlugHub ecosystem.