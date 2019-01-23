using System.Collections.Generic;

namespace System.Threading.Tasks
{
    internal static class ValueTaskExtensions
    {
        public static async ValueTask WhenAll(this IEnumerable<ValueTask> valueTasks)
        {
            var tasks = new List<Task>();
            foreach (ValueTask valueTask in valueTasks)
            {
                if (!valueTask.IsCompletedSuccessfully)
                {
                    tasks.Add(valueTask.AsTask());
                }
            }

            if (tasks.Count == 0)
            {
                return;
            }

            await Task.WhenAll(tasks);
        }
    }
}
