using Microsoft.Extensions.Logging;

namespace Blake.BuildTools;

/// <summary>
/// Defines a plugin interface for extending the behavior of the baking process in a <see cref="BlakeContext"/>.
/// </summary>
/// <remarks>Implement this interface to execute custom logic before and/or after the baking process. The <see
/// cref="BeforeBakeAsync"/> method is invoked prior to the baking operation, and the <see cref="AfterBakeAsync"/>
/// method is invoked after the baking operation completes.</remarks>
public interface IBlakePlugin
{
    /// <summary>
    /// Performs any necessary pre-baking operations asynchronously.
    /// </summary>
    /// <param name="context">The context containing information and resources for the baking process.</param>
    /// <param name="logger">An ILogger instance - default Console logger is passed in by the Blake CLI</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null) => Task.CompletedTask;

    /// <summary>
    /// Performs any necessary operations after the baking process is completed.
    /// </summary>
    /// <param name="context">The context containing information about the completed baking process.</param>
    /// <param name="logger">An ILogger instance - default Console logger is passed in by the Blake CLI</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AfterBakeAsync(BlakeContext context, ILogger? logger = null) => Task.CompletedTask;
}
