<div align="center">

<a href="https://blake-ssg.org" target="_blank" title="Go to the Blake website"><img width="196px" alt="blake logo" src="https://raw.githubusercontent.com/matt-goldman/blake/refs/heads/main/assets/blake.svg"></a>

# Blake

### 🍞 **Bake your Blazor into beautiful static sites**

*The static site generator for .NET developers who want familiar tools, not foreign languages.*

| `Blake.Types` | `Blake.MarkdownParser` | `Blake.BuildTools` | `Blake.CLI` |
|-------------|----------------------|------------------|-----------|
| ![NuGet Version](https://img.shields.io/nuget/v/Blake.Types?style=for-the-badge) | ![NuGet Version](https://img.shields.io/nuget/v/Blake.MarkdownParser?style=for-the-badge)| ![NuGet Version](https://img.shields.io/nuget/v/Blake.BuildTools?style=for-the-badge) | ![NuGet Version](https://img.shields.io/nuget/v/Blake.CLI?style=for-the-badge) |

![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/matt-goldman/blake/ci.yml?branch=main&style=for-the-badge)

**[📚 Read the docs](https://www.blake-ssg.org/)** • **[🚀 Quick Start](#-quick-start)** • **[🌟 See it in action](#-see-it-in-action)**

</div>

---

## 🌟 See it in action

**Live examples built with Blake:**

🌐 **[Blake Documentation](https://blake-ssg.org)** - The official Blake docs site  
*Modern documentation with clean design and full-text search*

🎨 **[Tailwind Sample Blog](https://tailwindsample.blake-ssg.org)** - A beautiful blog template  
*Responsive blog with Tailwind CSS styling and modern typography*

---

## 🚀 Quick start

Get up and running in seconds:

### 1️⃣ Install Blake globally
```bash
dotnet tool install -g blake
```

### 2️⃣ Create a new site
```bash
# Create from template
blake new --template tailwind-sample

# Create without a template (uses the default Blazor WASM template)
blake new

# Or init an existing Blazor WASM site
blake init
```

### 3️⃣ Start building
```bash
blake bake && dotnet run
# or
blake serve  # does both bake and serve
```

**That's it!** ✨ Your static site is ready at your configured port

### 🔧 Advanced usage

**Add Blake to existing Blazor app:**
```bash
blake init
```

**Generate content only:**
```bash
blake bake
```

**List all templates:**
```bash
blake new --list
```

**Generate a new post or page scaffold:**
```bash
blake new post -t "Adding new templates to Blake"
blake new page -t "About"
blake new post "Positional title works too"
blake new post --directory Posts/Tech "Nested post"
```


## ✨ Why Blake?

**Tired of learning new templating languages just to blog?** Blake brings static site generation to your comfort zone.

### 🎯 Built for .NET developers
- **No foreign syntax** - Use Razor templates you already know
- **No config chaos** - Folder structure determines everything  
- **No build mysteries** - Just Blazor, just bake, just works

### 🧠 Guided by Occam's Razor
*The solution with the fewest assumptions is often the best.*

**Other generators:** "Put layouts here, templates there, config everywhere, sprinkle fairy dust, pray it builds."

**Blake:** "Put content wherever makes sense. Add a `template.razor`. Done."

### ⚡ Key features
✅ **Convention over configuration** - Smart defaults, zero setup  
✅ **Familiar tooling** - Razor, Markdown, Blazor components  
✅ **Plugin system** - Extend functionality without complexity  
✅ **Live templates** - Community-driven starter templates  
✅ **Modern workflow** - Integrates with existing .NET tools  

## 🤔 Blake vs. Others

Love Hugo, Jekyll, or Gatsby? They're fantastic! But they make too many assumptions:

- 😭 **Hugo:** Assumes you're happy to learn Go templating + TOML/YAML config  
- 😭 **Jekyll:** Assumes you're cool with Liquid templating + Ruby ecosystem  
- 😭 **Gatsby:** Assumes you want a static site with GraphQL + React + complex build chains  
- 🤩 **Blake:** Assumes you have the .NET CLI installed

**Blake doesn't try to be everything to everyone.** It tries to be exactly what feels intuitive to .NET developers who just want to write and publish.

---

## 📖 Documentation

**[Complete documentation →](https://blake-ssg.org)**

- [Getting Started Guide](https://blake-ssg.org/1%20introduction/quickstart)
- [Template Development](https://blake-ssg.org/6%20creating%20site%20templates/overview)
- [Plugin Development](https://blake-ssg.org/7%20writing%20plugins/overview)
- [Blake Philosophy](https://blake-ssg.org/4%20meta/philosophy)

---

<div align="center">

**Ready to bake?** 🍞✨

[**Get Started →**](https://blake-ssg.org) • [**View Templates →**](https://blake-ssg.org/2%20using%20blake/sitetemplates) • [**Join Community →**](https://github.com/matt-goldman/blake/discussions)

</div>
