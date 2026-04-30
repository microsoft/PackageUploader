using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.UI.Test
{
    // Code from: https://getyourbitstogether.com/wpf-and-mstest/
    public class WpfTestMethodAttribute : TestMethodAttribute
    {
        public WpfTestMethodAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
            : base(callerFilePath, callerLineNumber)
        {
        }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return base.ExecuteAsync(testMethod);

            var tcs = new TaskCompletionSource<TestResult[]>();
            var thread = new Thread(() =>
            {
                var result = base.ExecuteAsync(testMethod).GetAwaiter().GetResult();
                tcs.SetResult(result);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return tcs.Task;
        }
    }
}
