using Markdig.Extensions.CustomContainers;
using Markdig.Parsers;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Blake.MarkdownParser;

public class SafeContainerParser(ILogger? logger = null) : CustomContainerParser
{
    public override BlockState TryOpen(BlockProcessor processor)
    {
        logger?.LogDebug("🧪 [TryOpen] Called");
        logger?.LogDebug("🧪 [TryOpen] Current block: {currentBlock}", processor.CurrentBlock?.GetType().Name ?? "null");
        logger?.LogDebug("🧪 [TryOpen] Current line: {currentLine}", processor.Line.ToString());

        if (processor.IsCodeIndent || processor.CurrentBlock is FencedCodeBlock)
        {
            logger?.LogDebug("🛑 [TryOpen] Skipping: inside code block or indented content.");
            return BlockState.None;
        }

        logger?.LogDebug("✅ [TryOpen] Proceeding with container parsing.");
        return base.TryOpen(processor);
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        logger?.LogDebug("🧪 [TryContinue] Called");
        logger?.LogDebug("🧪 [TryContinue] Block type: {blockType}", block?.GetType().Name ?? "null");
        logger?.LogDebug("🧪 [TryContinue] Current line: {currentLine}", processor.Line.ToString());

        if (processor.IsCodeIndent || processor.CurrentBlock is FencedCodeBlock)
        {
            logger?.LogDebug("🛑 [TryContinue] Inside code block or indented content. Continuing.");
            return BlockState.Continue;
        }

        logger?.LogDebug("✅ [TryContinue] Proceeding with normal continue.");
        return base.TryContinue(processor, block);
    }

    public override bool Close(BlockProcessor processor, Block block)
    {
        logger?.LogDebug("🧪 [TryClose] Called");
        logger?.LogDebug("🧪 [TryClose] Block type: {blockType}", block?.GetType().Name ?? "null");
        logger?.LogDebug("🧪 [TryClose] Current line: {currentLine}", processor.Line.ToString());

        if (processor.IsCodeIndent || processor.CurrentBlock is FencedCodeBlock)
        {
            logger?.LogDebug("🛑 [TryClose] Inside code block or indented content. Not closing container.");
            return true;
        }

        logger?.LogDebug("✅ [TryClose] Proceeding with normal close logic.");
        return base.Close(processor, block);
    }
}


