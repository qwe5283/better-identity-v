using Microsoft.Extensions.Logging;

namespace BetterIdentityV.GameTask.Common;

public class TaskControl
{
    public static ILogger Logger { get; } = App.GetLogger<TaskControl>();
}