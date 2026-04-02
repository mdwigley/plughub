# NucleusAF.Mock

This is a **test modules** for the NucleusAF platform.  
NucleusAF.Mock is intended for demonstration and validation of the NucleusAF module system.
It serves as a reference implementation and is **not intended for production use**.

---

## About

NucleusAF.Mock demonstrates core modules integration patterns for the NucleusAF host application.  
It is included in the official NucleusAF repository to showcase module structure, registration, and behavior within the modular NucleusAF ecosystem.

### What does it demonstrate?
- **Type-safe configuration**: Example strongly-typed config model (`NucleusMockConfig`) with permissions managed via access tokens.
- **Dependency injection**: Provides and consumes services (`IEchoService`) to illustrate cross-module communication and handler extension.
- **Token-based configuration security**: Shows owner/read/write access control on module configuration data.
- **Application rebranding**: Demonstrates white-label support by renaming NucleusAF and overriding app storage directories when loaded at the system level.
- **Style and theming**: Supplies UI resources (e.g., icon styles) for host customization.
- **Extensible UI**: Registers navigation pages and configuration settings pages, giving end users familiar, integrated module experiences.

### Who should use this?

- **Module developers** wanting a working reference for descriptor interfaces and integration points.
- **Testers** or system integrators validating NucleusAF-side changes to the module load, conflict, or configuration systems.

---

## License

This module is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.  
See the [LICENSE](LICENSE) file in this directory for full details.

---

For more information about NucleusAF, module development, and community discussions, please refer to the main [NucleusAF repository](../../) and documentation.
