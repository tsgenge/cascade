namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public static class DeadLetterMessageFormatter
{
    public static string GetDeadLetterReason(Exception? exception)
    {
        if (exception is null)
            return string.Empty;

        const int maxLength = 4096;

        var lines = new List<string>();
        CollectExceptionTree(exception, [], lines);

        var reason = string.Join(Environment.NewLine, lines);

        return reason.Length <= maxLength ? reason : reason[..maxLength];
    }

    private static void CollectExceptionTree(Exception exception, string[] ancestorPath, List<string> lines)
    {
        string[] currentPath = [.. ancestorPath, $"{exception.GetType().Name}: {exception.Message}"];

        if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0)
        {
            foreach (var inner in aggregate.InnerExceptions)
                CollectExceptionTree(inner, currentPath, lines);
        }
        else
        {
            if (exception.InnerException is not null)
                CollectExceptionTree(exception.InnerException, currentPath, lines);
            else
                lines.Add(string.Join(" ---> ", currentPath));
        }
    }    
}