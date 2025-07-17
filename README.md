<div align="center">

<a href="https://blakessg.org" target="_blank" title="Go to the Blake website"><img width="196px" alt="blake logo" src="https://raw.githubusercontent.com/matt-goldman/blake/refs/heads/main/assets/blake.svg?token=GHSAT0AAAAAADGARELE4TEW34XGEDG4XKGK2DWYLBQ"></a>

# Blake

</div>

**Bake your Blazor into beautiful static sites.**

> **Note:** This is an ongoing, experimental WIP

## Quick start

### Install the CLI:

```bash
dotnet tool install -g blake
```

### Run a Blake site:

```bash
dotnet run
```

(simples!)

### Add Blake to an existing site:

```bash
blake init
```

### Build a regular Blazor site with Blake:

```bash
blake bake
```

_Why?_ A Blake template is just a Blazor WASM app with an MSBuild task that uses Blake to generate Razor files from templates. If for whatever reason you don't want to do that, Blake can generate files in any Blazor WASM app. If you have `template.razor` files that follow Blake templating conventions, and Markdown files in the same folders, Blake will generate Razor files by combining the content and template. If you want to keep your templated content generation separate from your build and run, you can do it this way.

---
_**Coming soon:**_

### Create a new Blake site:

```bash
blake new
```

Optionally to specify a template, use `--template` or the alias `-t` and specify a template name:

```bash
blake new --template docs
```

or:

```bash
blake new -t blog
```

---

## Why Blake?

Blake (a portmanteau of Blazor and Bake) was born from my frustration with existing static site generators. They're all good in their own ways, but I wanted something closer to my comfort zone.

Yes, pushing yourself and learning new things is great. But I already have enough new things to learn. Documenting what I learn on my blog shouldn't be yet another thing to learn.

**Guided by Occam's Razor: the solution with the fewest assumptions is often the best.**

Other static site generators often feel like Rube Goldberg machines:

> "Ok so you put layouts in this folder, then you template them using this weird syntax, then you sprinkle in cryptic config, then you manage separate folders for posts vs pages, then finally with a sprinkle of fairy dust it _might_ build."

**Blake's approach:**

* Put whatever you want wherever you want.
* If a folder contains a `template.razor`, file and that template includes recognized placeholders, any Markdown files in that folder will be rendered with it and added to the global site index.
* Routes are auto-generated based on folder structure.

✅ No arcane templating languages.    
✅ No endless config.    
✅ No hidden assumptions.    
✅ Just Blazor. Just .NET. Just bake.    

Blake embodies the true meaning of Occam's Razor — it minimizes assumptions and keeps things honest, intuitive, and familiar for developers who already know .NET and HTML.

## Why not Hugo, Jekyll, etc.?

Other static site generators are fantastic. Each has its own strengths, thriving communities, and great track records. But they also often come with assumptions that may not suit every developer:

* Assumes you want to learn a new templating language (e.g., Liquid, Go templates).
* Assumes you're happy to wrangle extra config formats (YAML, TOML, etc.).
* Assumes you'll maintain separate folders for content, layouts, and partials.
* Assumes you're willing to adopt an entirely new mental model for building and structuring a site.

Blake's philosophy is different: fewer assumptions.

* You already know .NET.
* You already use Blazor or Razor syntax.
* You want to drop content wherever it makes sense to you.

Blake doesn't try to be everything to everyone; it tries to be exactly what feels intuitive to Blazor and .NET developers who just want to write and publish.

## Blake Philosophy

Blake is built around simplicity, minimalism, and transparency. No config, no ceremony, just Razor and Markdown, rendered where you put them.

We believe:

* Content and structure should be self-evident from folder layout.
* If it's not in the content or the template, it doesn't exist.
* Complexity belongs in templates, not the generator.
* Developers deserve to understand and own their build process.

→ [Read the full Blake Philosophy](/docs/philosophy.md)

## Docs

Check the wiki (coming soon) for detail on templating conventions, documentation for the starter templates, and how to create (and share) your own templates.

---

**Let's bake. 🍞🚀**
