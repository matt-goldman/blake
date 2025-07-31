# Blake Project Plan & Architecture

## 🧠 Project Overview

**Blake** is a Blazor-based static site generator that:

* Uses Markdown and Razor components
* Emphasises convention-over-configuration
* Supports interactive Blazor components in Markdown-based sites
* Offers easy templating and live development with hot reload

---

## 🧱 Architecture & Design Decisions

| Area                     | Decision                                                                                                                                   |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **Build System**         | Use custom .NET code in the CLI (`blake bake`) instead of Cake                                                                             |
| **Templates**            | Git-based; users clone/fork or use `blake new --template <name/short name>`                                                                |
| **Registry**             | Public GitHub JSON file listing templates (`blake  new --list`)                                                                            |
| **Folder Structure**     | Convention-based: folders like `/Posts` contain `template.razor`                                                                           |
| **Markdown Parsing**     | Custom parser and renderer in `Blake.MarkdownParser` NuGet package                                                                         |
| **Metadata**             | YAML/JSON frontmatter mapped to `PageModel` with unrecognised values in a `Metadata` dictionary                                            |
| **Search**               | `GeneratedContentIndex` contains a static list of all rendererd Razor pages. Possibility to add SQLite if wanted too for full-text search. |
| **Config**               | Not required.             |
| **CLI Tooling**          | Console app (`blake`) with commands: `bake`, `serve`, `new`                                                                          |
| **Extensibility**        | Per-folder templates via `template.razor` files                                                                                            |

---

## 🛠️ Development To-Do List

### 🔹 Core Packages & Tools

* [x] Create `Blake.MarkdownParser` NuGet package

  * [x] Markdown parsing
  * [x] Frontmatter support (YAML/JSON)
  * [x] Output rendered Razor files combining Markdown and templates

### 🔹 CLI

* [x] Create console app: `Blake.CLI`
* [x] Implement `blake bake`

  * [x] Scan folders for `template.razor`
  * [x] Parse `.md` files in each folder
  * [x] Generate `.razor` files in a staging/build folder
* [x] Implement `blake serve`

  * [x] Start Blazor dev server (`dotnet watch run`)
  * [ ] Add file watcher for Markdown changes
  * [ ] Rebuild only affected `.razor` files
* [x] Implement `blake new`

  * [x] Create site from default template
  * [x] Optional: `--from https://github.com/xyz/repo.git`
  * [x] Optional: prompt for `site title`, `base URL`, etc.
* [x] Implement `blake new --list`

  * [x] Fetch from `Blake/TemplatesRegistry` JSON index

### 🔹 Template System

* [x] Create a default site template repo (`blake-template-default`)

  * [x] Vanilla Blazor WASM
  * [x] `/Posts` and `/Pages` folders with `template.razor`
  * [x] Example markdown files with frontmatter
  * [x] Reference `Blake.MarkdownParser`
* [x] Define folder structure conventions

  * [x] `template.razor` per content folder
  * [x] Support fallback to default layout if no template provided
* [x] Create sample `template.razor` (e.g., for blog posts)

### 🔹 Optional but Valuable

* [ ] Create `blake upgrade` command to re-sync templates (future) (not a priority)
* [ ] Add support for `blake add-template` to contribute to registry (future) (not a priority)
* [ ] Create `blake init` for starting new folders inside an existing site (e.g., add `/Changelog`) (done for adding Blake to an existing Blazor site)
* [ ] Create additional templates (e.g., docs, portfolio) (Need to consider what templates would be useful)

---

**Note:** Remaining in this file is out of date and no longer relevant. Superceded by the `current-status.md` and other files.

## 📦 Project Structure

```
Blake/
├── cli/                     → `blake` CLI tool
│   └── Blake.CLI/
├── markdown/                → Markdown parser package
│   └── Blake.Markdown/
├── renderer/                → Blazor Markdown renderer
│   └── Blake.MarkdownParser/
├── templates/               → Optional dev templates or testing
│   └── default/
├── registry/                → Template registry (JSON index)
└── examples/                → Sample sites built with Blake
```

---

## 🧭 Suggested Development Phases

1. Create `Blake.Markdown`
2. Create `blake build` command
3. Build the default template
4. Add `blake serve` with hot reload
5. Create `MarkdownParser` and add JS/CSS
6. Add GitHub registry & implement `blake list templates`
7. Extend with `blake new site --from`, `blake upgrade`, and other enhancements
