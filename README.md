# NucleusAF

[![License: MPL-2.0](https://img.shields.io/badge/License-MPL--2.0-blue.svg)](LICENSE.md)
[![Join us on Discord](https://img.shields.io/badge/Discord-Join-blue?logo=discord)](https://discord.com/invite/mWDHDqkzeR) 
[![Release](https://img.shields.io/github/v/release/enterlucent/nucleusaf?include_prereleases)](../../releases)

**NucleusAF** is an enterprise-grade application designed around a modular architecture, enabling developers and organizations to build, manage, and deploy extensible software systems with ease. It provides a robust framework where modules serve as independent, interchangeable components that seamlessly integrate into a unified host application.

With NucleusAF, you can rapidly extend functionality, validate module compatibility, and orchestrate complex workflows, all while maintaining a clean separation of concerns and maximizing maintainability.

**Join us** in creating a flexible and scalable platform that empowers innovation through modular design and dynamic extensibility.

## Table of Contents
- [Getting Started](#getting-started)
- [Contributing](#contributing)
- [Authors](#authors)
- [License](#license)

## Getting Started
- [Getting Information](#how-to-get-information) – Where to find documentation and support resources.
- [Support Questions](#how-to-open-a-support-question) – Check if your question has already been answered.
- [Bug Report](#how-to-submit-a-bug-report) – See if your issue has already been reported.
- [Feature Requests](#how-to-request-a-feature) – Find out if your idea has already been suggested.
- [Site Mention](#how-to-request-a-site-mention) – Submit community resources for inclusion.

### How to Get Information
- **Documentation** – Comprehensive guides and references for NucleusAF:
    - [Project Docs](docs/)
    - [GitHub Pages](https://enterlucent.github.io/nucleusaf/)
    - [Wiki](../../wiki)
    - [Discussions](../../discussions)
- **Community Chat** – Join our [Discord](https://discord.com/invite/mWDHDqkzeR) for real-time support and collaboration.

## How to Open a Support Question
1. **Search Existing Discussions**  
   Browse [existing discussions](../../discussions/categories/support) to see if your question has already been addressed.
   - If you find a relevant discussion, join the conversation by commenting or reacting.
2. **Start a New Discussion**  
   If your question isn't answered, create a new discussion in the appropriate category (such as Q&A or Support).
   - Use a clear, descriptive title (e.g., “How do I configure modules?”).
   - Provide detailed information and context to help others assist you efficiently.
3. **Follow Up**  
   Enable notifications or check back for responses. Please be patient, as maintainers and community members may reply asynchronously.

> Please use the Support category for these requests, not GitHub Issues. This helps keep actionable issues separate from community and promotional content, and makes it easier for maintainers to review and organize submissions.

### How to Submit a Bug Report
1. **Search for Existing Bugs:**  
   Review [open bug reports](../../issues?q=label%3Abug+is%3Aopen).
   - Add details or reopen if your issue matches an existing report.
2. **File a Bug Report:**  
   Use the [Bug Report template](../../issues/new?template=report-bug.yml).
   - Include detailed steps to reproduce, expected and actual behavior, and environment details.
3. **Monitor and Respond:**  
   Watch for follow-up questions or requests for additional information.

### How to Request a Feature
1. **Check for Existing Requests:**  
   Search [open feature requests](../../issues?q=label%3Aenhancement+is%3Aopen).
   - Comment to expand on existing requests; avoid duplicates.
2. **Submit a Feature Request:**  
   Open a [Feature Request](../../issues/new?template=request-feature.md).
   - Reference any related documentation needs in your request.
   - Clearly describe the feature, its purpose, and potential impact.
3. **Stay Engaged:**  
   Enable notifications or check back for discussion and status updates.
   - Features are accepted when labeled "approved" and may be scheduled for future releases.

## How to Request a Site Mention

If you have created a resource relevant to NucleusAF and would like it featured on our GitHub Pages:

- Start a new discussion in the [General category](../../discussions/categories/general).
- Prefix your discussion title with [PROMO]** (e.g., `[PROMO] mysite.com: NucleusAF Module/Application Showcase`).
- Respond to any follow-up questions from maintainers.
- You will be notified if your submission is accepted and published.

> Please use the General category for these requests, not GitHub Issues. This helps keep actionable issues separate from community and promotional content, and makes it easier for maintainers to review and organize submissions.

## Contributing
Please review our [CONTRIBUTING](.github/CONTRIBUTING.md) guidelines for details on how to participate, coding standards, and the development workflow.  

Refer to our [Design Document](docs/Design.md) and [Code of Conduct](.github/CODE_OF_CONDUCT.md) for further information.

## Authors

* **Michael Wigley** - *Programming* - [mdwigley](https://github.com/mdwigley)

See the list of [contributors](../../graphs/contributors) who have participated in this project.

## License

This repository uses the **MPL‑2.0 licensing model**.

### **Root License (MPL‑2.0)**
- All code in this repository is licensed under **MPL‑2.0** unless a module directory provides its own `LICENSE` file.
- MPL‑2.0 permits use inside proprietary or closed‑source applications.
- **Only modified MPL‑covered files must be shared**, and only as patches or file‑level diffs.

### **Module‑Level License Overrides**
- Any module folder containing its own `LICENSE` file is governed by that license.
- These module‑specific licenses override the root MPL‑2.0 terms for that module only.
- This allows official or third‑party modules to use **GPL, LGPL, MPL, MIT, or commercial** licensing as needed.

### Summary Table

| Component / Folder          | License           | Requirement                                          |
|-----------------------------|-------------------|-------------------------------------------------------|
| Host / Shared Code          | MPL‑2.0           | Modified files must be shared under MPL‑2.0.          |
| Module Folders w/ LICENSE   | Declared license  | Overrides MPL‑2.0 for that module.                    |
| Module Folders w/o LICENSE  | MPL‑2.0           | Inherits root MPL‑2.0 terms.                          |

> **Always check each module’s directory**: if it contains a `LICENSE` file, that license governs that module.
