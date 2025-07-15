using System.Text;
using System.Web;
using Markdig.Parsers;
using Markdig.Prism;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Blake.MarkdownParser;

public class PrismCodeBlockRenderer(CodeBlockRenderer codeBlockRenderer, PrismOptions options) : HtmlObjectRenderer<CodeBlock>
{
    private readonly CodeBlockRenderer codeBlockRenderer = codeBlockRenderer ?? new CodeBlockRenderer();
    // Adding options to the renderer
    private readonly PrismOptions _options = options ?? new PrismOptions();
    public static readonly Dictionary<string, string> LanguageToFileExtension = new Dictionary<string, string>
    {
        { "javascript", ".js" },
        { "csharp", ".cs" },
        { "python", ".py" },
        { "markup", ".html" },
        { "css", ".css" },
        { "clike", ".js" },
        { "abap", ".abap" },
        { "actionscript", ".as" },
        { "ada", ".ada" },
        { "apacheconf", ".conf" },
        { "apl", ".apl" },
        { "applescript", ".applescript" },
        { "arduino", ".ino" },
        { "arff", ".arff" },
        { "asciidoc", ".adoc" },
        { "asm6502", ".asm" },
        { "aspnet", ".cs" },
        { "autohotkey", ".ahk" },
        { "autoit", ".au3" },
        { "bash", ".sh" },
        { "basic", ".bas" },
        { "batch", ".bat" },
        { "bison", ".bison" },
        { "brainfuck", ".bf" },
        { "bro", ".bro" },
        { "c", ".c" },
        { "cpp", ".cpp" },
        { "coffeescript", ".coffee" },
        { "clojure", ".clj" },
        { "crystal", ".cr" },
        { "csp", ".csp" },
        { "css-extras", ".css" },
        { "d", ".d" },
        { "dart", ".dart" },
        { "diff", ".diff" },
        { "django", ".django" },
        { "docker", ".docker" },
        { "eiffel", ".e" },
        { "elixir", ".ex" },
        { "elm", ".elm" },
        { "erb", ".erb" },
        { "erlang", ".erl" },
        { "fsharp", ".fs" },
        { "flow", ".js" },
        { "fortran", ".f90" },
        { "gedcom", ".ged" },
        { "gherkin", ".feature" },
        { "git", ".git" },
        { "glsl", ".glsl" },
        { "gml", ".gml" },
        { "go", ".go" },
        { "graphql", ".graphql" },
        { "groovy", ".groovy" },
        { "haml", ".haml" },
        { "handlebars", ".hbs" },
        { "haskell", ".hs" },
        { "haxe", ".hx" },
        { "http", ".http" },
        { "hpkp", ".hpkp" },
        { "hsts", ".hsts" },
        { "ichigojam", ".ijam" },
        { "icon", ".icon" },
        { "inform7", ".inform" },
        { "ini", ".ini" },
        { "io", ".io" },
        { "j", ".j" },
        { "java", ".java" },
        { "jolie", ".ol" },
        { "json", ".json" },
        { "julia", ".jl" },
        { "keyman", ".keyman" },
        { "kotlin", ".kt" },
        { "latex", ".tex" },
        { "less", ".less" },
        { "liquid", ".liquid" },
        { "lisp", ".lisp" },
        { "livescript", ".ls" },
        { "lolcode", ".lol" },
        { "lua", ".lua" },
        { "makefile", ".mk" },
        { "markdown", ".md" },
        { "markup-templating", ".html" },
        { "matlab", ".mat" },
        { "mel", ".mel" },
        { "mizar", ".miz" },
        { "monkey", ".monkey" },
        { "n4js", ".n4js" },
        { "nasm", ".asm" },
        { "nginx", ".conf" },
        { "nim", ".nim" },
        { "nix", ".nix" },
        { "nsis", ".nsis" },
        { "objectivec", ".m" },
        { "ocaml", ".ml" },
        { "opencl", ".cl" },
        { "oz", ".oz" },
        { "parigp", ".parigp" },
        { "parser", ".parser" },
        { "pascal", ".pas" },
        { "perl", ".pl" },
        { "php", ".php" },
        { "plsql", ".plsql" },
        { "powershell", ".ps1" },
        { "processing", ".pde" },
        { "prolog", ".pl" },
        { "properties", ".properties" },
        { "protobuf", ".proto" },
        { "pug", ".pug" },
        { "puppet", ".pp" },
        { "pure", ".pure" },
        { "q", ".q" },
        { "qore", ".q" },
        { "r", ".r" },
        { "jsx", ".jsx" },
        { "tsx", ".tsx" },
        { "renpy", ".rpy" },
        { "reason", ".re" },
        { "rest", ".rest" },
        { "rip", ".rip" },
        { "roboconf", ".roboconf" },
        { "ruby", ".rb" },
        { "rust", ".rs" },
        { "sas", ".sas" },
        { "sass", ".sass" },
        { "scss", ".scss" },
        { "scala", ".scala" },
        { "scheme", ".scm" },
        { "smalltalk", ".st" },
        { "smarty", ".tpl" },
        { "sql", ".sql" },
        { "soy", ".soy" },
        { "stylus", ".styl" },
        { "swift", ".swift" },
        { "tap", ".tap" },
        { "tcl", ".tcl" },
        { "textile", ".textile" },
        { "tt2", ".tt2" },
        { "twig", ".twig" },
        { "typescript", ".ts" },
        { "vbnet", ".vb" },
        { "velocity", ".vm" },
        { "verilog", ".v" },
        { "vhdl", ".vhdl" },
        { "vim", ".vim" },
        { "visual-basic", ".vb" },
        { "wasm", ".wasm" },
        { "wiki", ".wiki" },
        { "xeora", ".x" },
        { "xojo", ".xojo" },
        { "xquery", ".xq" },
        { "yaml", ".yaml" }
    };

    protected override void Write(HtmlRenderer renderer, CodeBlock node)
    {
        var stringWriter = new StringWriter();
        var debugRenderer = new HtmlRenderer(stringWriter)
        {
            EnableHtmlForInline = renderer.EnableHtmlForInline,
            EnableHtmlForBlock = renderer.EnableHtmlForBlock
        };

        if (node is not FencedCodeBlock fencedCodeBlock || node.Parser is not FencedCodeBlockParser parser)
        {
            codeBlockRenderer.Write(renderer, node);
            return;
        }

        if (fencedCodeBlock.Info == null)
        {
            codeBlockRenderer.Write(renderer, node);
            return;
        }

        if (parser.InfoPrefix == null || !fencedCodeBlock.Info.StartsWith(parser.InfoPrefix))
        {
            codeBlockRenderer.Write(renderer, node);
            return;
        }

        var languageCode = fencedCodeBlock!.Info!.Replace(parser.InfoPrefix, string.Empty);
        if (string.IsNullOrWhiteSpace(languageCode) || !PrismSupportedLanguages.IsSupportedLanguage(languageCode))
        {
            codeBlockRenderer.Write(renderer, node);
            return;
        }

        var preAttributes = new HtmlAttributes();
        var codeAttributes = new HtmlAttributes();

        if (_options.UseLineDiff && fencedCodeBlock.Arguments is not null)
        {
            ParseLineDiffs(fencedCodeBlock.Arguments, codeAttributes);
        }


        codeAttributes.AddClass($"language-{languageCode}");

        if (_options.UseLineHighlighting && fencedCodeBlock.Arguments is not null)
        {
            if (fencedCodeBlock.Arguments.Contains("marked="))
            {
                var markedValue = GetArgumentValue(fencedCodeBlock.Arguments, "marked");

                if (!string.IsNullOrEmpty(markedValue))
                {
                    codeAttributes.AddProperty("data-line", markedValue);
                }
            }
        }

        if (_options.UseLineNumbers)
        {
            codeAttributes.AddClass("line-numbers");
        }

        if (_options.UseCopyButton)
        {
            codeAttributes.AddProperty("data-prismjs-copy", "Copy");
        }

        debugRenderer.Write("<pre");
        debugRenderer.WriteAttributes(preAttributes);
        debugRenderer.Write(">");

        debugRenderer.Write("<code");
        debugRenderer.WriteAttributes(codeAttributes);
        debugRenderer.Write(">");

        var code = ExtractSourceCode(node);
        var escapedCode = HttpUtility.HtmlEncode(code);

        debugRenderer.Write(escapedCode)
               .Write("</code>");

       debugRenderer.Write("</pre>");

       renderer.Write(stringWriter.ToString());
    }

    private static string GetArgumentValue(string arguments, string key)
    {
        var args = arguments.Split(' ');

        var argument = args.FirstOrDefault(arg => arg.StartsWith($"{key}="));

        var argValue = argument?.Substring($"{key}=".Length);
        return argValue?? string.Empty;
    }

    private static void ParseLineDiffs(string argument, HtmlAttributes attributes)
    {
        var removedLines = GetArgumentValue(argument, "removed");
        var addedLines = GetArgumentValue(argument, "added");

        if (!string.IsNullOrEmpty(addedLines))
        {
            attributes.AddProperty("data-added-line", addedLines);
        }

        if (!string.IsNullOrEmpty(removedLines))
        {
            attributes.AddProperty("data-removed-line", removedLines);
        }
    }


    protected static string ExtractSourceCode(LeafBlock node)
    {
        var code = new StringBuilder();
        var lines = node.Lines.Lines;
        int totalLines = lines.Length;
        for (int i = 0; i < totalLines; i++)
        {
            var line = lines[i];
            var slice = line.Slice;
            if (slice.Text == null)
            {
                continue;
            }

            var lineText = slice.Text.Substring(slice.Start, slice.Length);
            if (i > 0)
            {
                code.AppendLine();
            }

            code.Append(lineText);
        }

        return code.ToString();
    }
}
