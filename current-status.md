# Blake Project Plan & Architecture (Updated)

## 🧠 Project Overview

Blake is a Blazor-based static site generator (SSG) that combines Markdown content, Razor templates, and Blazor interactive components. It supports a convention-over-configuration approach and integrates deeply with .NET workflows.

---

## ✅ Current Status

- Working CLI (`blake`) for generating new sites, building, and serving content.
- Supports content folders (e.g., `/Posts`, `/Pages`) with per-folder `template.razor` files.
- Generates `.razor` pages with correct routing (`@page`) and pre-rendered Markdown.
- Generates a strongly-typed `GeneratedContentIndex` class containing page metadata for dynamic navigation and lists.
- Builds successfully via Blazor (`dotnet run`) after running CLI.
- Correct csproj configuration to include `.generated` files and exclude templates.

---

## 🟢 New Features & Architecture Decisions

### Site-wide config support

- **YAML-based global `config.yaml` file** (like Hugo/Jekyll).
- Generates a strongly-typed `SiteConfig` class inside `.generated` folder.
- Example properties: `Title`, `Description`, `Social`, `Analytics`.
- Used directly in templates via `@using Generated`.

### Template registry

- Separate `Blake.Templates` repo containing `registry.json`.
- Template metadata: name, description, author, repo URL, last updated, tags, optional preview URL.
- Community contributes templates via PRs.
- GitHub Actions workflow validates templates: clones, injects test content, runs `blake build`, `dotnet build`, Playwright checks.
- CLI supports `--template` (registry) and `--templateUrl` (custom direct URL).

### MSBuild integration

- Templates include a preconfigured MSBuild `Target` that runs `blake build` before build.
- Allows `dotnet run` or `dotnet watch run` to work as if it's a normal Blazor app.
- CLI remains useful for explicit build, live serve, or CI.

### Dynamic navigation

- Navigation can be generated using `GeneratedContentIndex` data.
- Future support for filtering (tags, folders).

---

## ⚡ Next Steps

- [ ] Implement `blake serve` with Markdown watch support.
- [ ] Complete `config.yaml` parsing and `SiteConfig` generator.
- [ ] Implement final template registry CLI integration (`blake list templates`, `blake new site --template`).
- [ ] Add incremental build support.
- [ ] Improve error handling and CLI UX.
- [ ] Create starter templates (blog, docs, portfolio).
- [ ] Add contributor documentation and starter guides.

---

## ✅ Concept Proved

- Dynamic Markdown page generation with correct routing works.
- Blazor build and runtime integration fully confirmed.
- Dynamic nav and metadata proven.

---

## 💬 Notes

- `.generated/` folder used for all generated `.razor` and `.cs` files.
- Templates use YAML frontmatter per file for metadata and global YAML for site config.
- No separate NuGet package needed for MSBuild tasks — logic lives in template `.csproj`.
- CLI required but designed to feel invisible for most dev workflows.

---

## 📄 ASCII Diagram (build flow)

```plaintext
Markdown + Templates → blake build → .generated/*.razor + .generated/*.cs → dotnet run → Blazor app
```

---

✅ This updated plan captures all recent decisions and architecture changes.
