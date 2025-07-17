# Blake Roadmap

Blake aims to stay small, fast, and focused, but there is value in supporting extensibility for template authors or users who want to go further.

## Future Considerations

- 🔌 **Plugin Support**
  - Interface: `IBlakePlugin`
  - Purpose: Add domain-specific logic (e.g., RSS, search indexing, sitemap)
  - Status: On the radar — not implemented

- 📦 **Optional NuGet Packages**
  - For advanced users who want to include features like:
    - RSS feeds for podcasts
    - Auto-sitemap generation
    - Git-based last-modified tracking
    - Structured data (schema.org)

- 🔍 **Template Marketplace / Index**
  - Community-made templates with plug-and-play features

## What Blake Will Never Do

- Maintain complex config DSLs
- Enforce folder hierarchies
- Add built-in search engines, shortcodes, taxonomies, or asset bundlers

We’ll always default to "no"; unless saying yes creates value **without creating ceremony**.
