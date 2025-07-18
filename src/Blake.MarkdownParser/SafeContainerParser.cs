using Markdig.Extensions.CustomContainers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Blake.MarkdownParser;

public class SafeContainerParser : CustomContainerParser
{
    public override BlockState TryOpen(BlockProcessor processor)
    {
        Console.WriteLine("🧪 [TryOpen] Called");
        Console.WriteLine($"🧪 [TryOpen] Current block: {processor.CurrentBlock?.GetType().Name ?? "null"}");
        Console.WriteLine($"🧪 [TryOpen] Current line: {processor.Line.ToString()}");

        if (processor.IsCodeIndent || processor.CurrentBlock is FencedCodeBlock)
        {
            Console.WriteLine("🛑 [TryOpen] Skipping: inside code block or indented content.");
            return BlockState.None;
        }

        Console.WriteLine("✅ [TryOpen] Proceeding with container parsing.");
        return base.TryOpen(processor);
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        Console.WriteLine("🧪 [TryContinue] Called");
        Console.WriteLine($"🧪 [TryContinue] Block type: {block?.GetType().Name ?? "null"}");
        Console.WriteLine($"🧪 [TryContinue] Current line: {processor.Line.ToString()}");

        if (processor.IsCodeIndent || processor.CurrentBlock is FencedCodeBlock)
        {
            Console.WriteLine("🛑 [TryContinue] Inside code block or indented content. Continuing.");
            return BlockState.Continue;
        }

        Console.WriteLine("✅ [TryContinue] Proceeding with normal continue.");
        return base.TryContinue(processor, block);
    }

    public override bool Close(BlockProcessor processor, Block block)
    {
        Console.WriteLine("🧪 [TryClose] Called");
        Console.WriteLine($"🧪 [TryClose] Block type: {block?.GetType().Name ?? "null"}");
        Console.WriteLine($"🧪 [TryClose] Current line: {processor.Line.ToString()}");

        if (processor.IsCodeIndent || processor.CurrentBlock is FencedCodeBlock)
        {
            Console.WriteLine("🛑 [TryClose] Inside code block or indented content. Not closing container.");
            return true;
        }

        Console.WriteLine("✅ [TryClose] Proceeding with normal close logic.");
        return base.Close(processor, block);
    }
}


