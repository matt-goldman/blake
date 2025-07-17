# The Blake Philosophy

Blake is built for .NET and Blazor developers who value:

- **Predictability over magic**
- **Convention over configuration**
- **Clarity over cleverness**

## Guiding Principles

### No hidden logic

If it's not in the content or the template, it doesn't exist. Want a default author? Add it to your template logic. Blake won't guess.

### Folders are structure

Put files where they make sense to you. If a folder contains a `template.razor` file and Markdown content, Blake will combine them. No special folders like `_posts`, `layouts`, or `partials` are required.

### Templates own complexity

If you want custom metadata, fallback values, author lookups, or layouts, build it into your Razor template. Blake stays out of your way.

### No required config files

Blake works with zero configuration. If you do need reusable site-wide values, you can use `.NET`'s existing `appsettings.json` support under a `"BlakeSettings"` key (or `[TemplateName]Settings`, or whatever you like). Blake has no special parser or syntax. See _Template Authoring Guide_ (coming soon) for more details.

### Draft content is opt-in

The only generation logic that Blake controls is whether to include drafts. If a Markdown file has `draft: true` in the frontmatter, it will be skipped — unless you pass the `--includeDrafts` flag.

## Why This Matters

Many static site generators are awesome, but they add too many assumptions for my liking. They assume:

- You'll learn a new templating language.
- You'll maintain multiple config files and folders.
- You'll understand a custom mental model.

Blake assumes **you already know .NET** — and lets you use that knowledge, without reinventing how web development works.

## What About Features Like RSS?

Blake doesn’t include built-in features like RSS, search indexing, or tag pages.

Why? Because those are **not core concerns** of a static site generator; they’re **template concerns** or, if complex enough, **plugin concerns**.

If your site needs podcast feeds, sitemap generation, or custom content graphs, the right place to handle that is:

- In your template logic (if simple)
- Or via a future plugin system (if reusable)

Blake’s job is just to bake.


## Future Vision

If you want more power, you can opt-in to:

- Custom plugins via `IBlakePlugin` (coming soon)
- Custom `BuildContext` logic via `Func<BuildContext, Task>` hooks

But if you're happy with just Razor and Markdown, you’ll never need to know they exist.

---

> _"The solution with the fewest assumptions is often the best."_  
> — Occam's Razor (and Blake's heart)
