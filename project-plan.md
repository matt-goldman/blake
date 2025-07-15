# Blake Project Plan & Architecture

## 🧠 Project Overview

**Blake** is a Blazor-based static site generator that:

* Uses Markdown and Razor components
* Emphasises convention-over-configuration
* Supports interactive Blazor components in Markdown-based sites
* Offers easy templating and live development with hot reload

---

## 🧱 Architecture & Design Decisions

| Area                     | Decision                                                              |
| ------------------------ | --------------------------------------------------------------------- |
| **Build System**         | Use custom .NET code in the CLI (`blake build`) instead of Cake          |
| **Templates**            | Git-based; users clone/fork or use `blake new site --from <repo>`        |
| **Registry**             | Public GitHub JSON file listing templates (`blake list templates`)       |
| **Folder Structure**     | Convention-based: folders like `/Posts` contain `template.razor`      |
| **Markdown Parsing**     | Custom parser in `Blake.Markdown` NuGet package                  |
| **Markdown Rendering**   | Razor Class Library `Blake.MarkdownRenderer` with JS support     |
| **Metadata**             | YAML/JSON frontmatter mapped to `@Metadata`                           |
| **Search**               | SQLite-based indexing (JSON fallback possible)                        |
| **Config**               | `config.json` for global settings only (e.g., title, baseUrl, search) |
| **CLI Tooling**          | Console app (`blake`) with commands: `build`, `serve`, `new site`        |
| **Dependency Injection** | Use `IConfiguration` for config access inside templates               |
| **Extensibility**        | Per-folder templates via `template.razor` files                       |

---

## 🛠️ Development To-Do List

### 🔹 Core Packages & Tools

* [ ] Create `Blake.Markdown` NuGet package

  * [ ] Markdown parsing
  * [ ] Frontmatter support (YAML/JSON)
  * [ ] Output: `ParsedMarkdown` with `HtmlContent` + `Metadata`
* [ ] Create `Blake.MarkdownRenderer` Razor Class Library

  * [ ] Razor component to render Markdown HTML
  * [ ] JS interop for syntax highlighting, diagrams, etc.
  * [ ] Style Markdown output with a default CSS (GitHub-style?)

### 🔹 CLI

* [ ] Create console app: `Blake.CLI`
* [ ] Implement `blake build`

  * [ ] Scan folders for `template.razor`
  * [ ] Parse `.md` files in each folder
  * [ ] Generate `.razor` files in a staging/build folder
  * [ ] Invoke `dotnet publish` on the Blazor project
* [ ] Implement `blake serve`

  * [ ] Start Blazor dev server (`dotnet watch run`)
  * [ ] Add file watcher for Markdown changes
  * [ ] Rebuild only affected `.razor` files
* [ ] Implement `blake new site`

  * [ ] Create site from default template
  * [ ] Optional: `--from https://github.com/xyz/repo.git`
  * [ ] Optional: prompt for `site title`, `base URL`, etc.
* [ ] Implement `blake list templates`

  * [ ] Fetch from `Blake/TemplatesRegistry` JSON index

### 🔹 Template System

* [ ] Create a default site template repo (`blake-template-default`)

  * [ ] Vanilla Blazor WASM
  * [ ] `/Posts` and `/Pages` folders with `template.razor`
  * [ ] Example markdown files with frontmatter
  * [ ] Reference `Blake.MarkdownRenderer`
* [ ] Define folder structure conventions

  * [ ] `template.razor` per content folder
  * [ ] Support fallback to default layout if no template provided
* [ ] Create sample `template.razor` (e.g., for blog posts)

### 🔹 Optional but Valuable

* [ ] Create `blake upgrade` command to re-sync templates (future)
* [ ] Add support for `blake add-template` to contribute to registry (future)
* [ ] Create `blake init` for starting new folders inside an existing site (e.g., add `/Changelog`)
* [ ] Create additional templates (e.g., docs, portfolio)

---

## 📦 Project Structure

```
Blake/
├── cli/                     → `blake` CLI tool
│   └── Blake.CLI/
├── markdown/                → Markdown parser package
│   └── Blake.Markdown/
├── renderer/                → Blazor Markdown renderer
│   └── Blake.MarkdownRenderer/
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
5. Create `MarkdownRenderer` and add JS/CSS
6. Add GitHub registry & implement `blake list templates`
7. Extend with `blake new site --from`, `blake upgrade`, and other enhancements
