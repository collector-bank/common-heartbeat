using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Collector.Common.Heartbeat
{
    /// <summary>
    ///     Data collected from an invokation of a test action
    /// </summary>
    public class DiagnosticsResult
    {
        public DiagnosticsResult(string componentName)
        {
            ComponentName = componentName;
            Success = true;
            ErrorMessages = new List<string>();
            DiagnosticsStartTime = DateTime.Now;
        }

        public DateTimeOffset DiagnosticsStartTime { get; set; }

        public long ElapsedMilliseconds { get; set; }

        public string ComponentName { get; set; }

        public bool Success { get; set; }

        public List<string> ErrorMessages { get; set; }
    }

    /// <summary>
    ///     Aggregation of multiple DiagnosticsResult
    /// </summary>
    public class DiagnosticsResults
    {
        public DiagnosticsResults(List<DiagnosticsResult> componentResults)
        {
            ComponentResults = componentResults;
            Success = componentResults.Count == 0 || componentResults.All(component => component.Success);
            ElapsedSequentialMilliseconds = componentResults.Sum(component => component.ElapsedMilliseconds);
            ElapsedMilliseconds = ElapsedSequentialMilliseconds;
            DiagnosticsStartTime = DateTimeOffset.Now;
        }

        public List<DiagnosticsResult> ComponentResults { get; }

        public bool Success { get; }

        public long ElapsedSequentialMilliseconds { get; }

        public long ElapsedMilliseconds { get; set; }

        public DateTimeOffset DiagnosticsStartTime { get; set; }
        public ProcessInformation ProcessInformation { get; set; }
    }

    public class ProcessInformation
    {
        public long UptimeMilliseconds { get; set; }
        public DateTime StartTime { get; set; }
    }


    /// <summary>
    ///     Utility class to make it easy to invoke components that implements ISupportsDiagnostics and to collect the result
    /// </summary>
    public static class DiagnosticsHelper
    {
        public static async Task<DiagnosticsResult> RunDiagnosticsTest(Func<Task> testAction)
        {
            var stopWatch = Stopwatch.StartNew();
            var result = new DiagnosticsResult(GetDescriptiveNameFromTestAction(testAction));
            try
            {
                await testAction();
            }
            catch (Exception error)
            {
                result.Success = false;
                result.ErrorMessages.Add(error.ToString());
            }

            result.ElapsedMilliseconds = stopWatch.ElapsedMilliseconds;
            return result;
        }

        public static async Task<DiagnosticsResults> RunDiagnosticsTests(
            IEnumerable<Func<Task>> testActions, bool parallel)
        {
            var startTime = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();
            List<DiagnosticsResult> componentResults;
            if (parallel)
            {
                var testTasks = testActions.Select(RunDiagnosticsTest).ToList();
                await Task.WhenAll(testTasks);
                componentResults = testTasks.Select(testTask => testTask.Result).ToList();
            }
            else
            {
                var results = new List<DiagnosticsResult>();
                foreach (var testAction in testActions)
                {
                    var result = await RunDiagnosticsTest(testAction);
                    results.Add(result);
                }

                componentResults = results;
            }

            return new DiagnosticsResults(componentResults)
            {
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DiagnosticsStartTime = startTime
            };
        }

        private static string TrimGenericTypeName(string typeName)
        {
            var i = typeName.IndexOf("`");
            return i >= 0 ? typeName.Substring(0, i) : typeName;
        }

        private static string GetDescriptiveNameFromTestAction(Func<Task> testAction)
        {
            var className = NormalizeString(TrimGenericTypeName(testAction.GetMethodInfo()?.DeclaringType?.Name));
            var genericArgs = string.Join(".", testAction.GetMethodInfo()?.DeclaringType?.GenericTypeArguments.Select(x => NormalizeString(x.Name)));
            var methodName = testAction.GetMethodInfo()?.Name;
            return string.Join(".", new[] { className, genericArgs, methodName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string NormalizeString(string s)
        {
            return string.Join(string.Empty, s.ToCharArray().Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '.'));
        }
    }
}