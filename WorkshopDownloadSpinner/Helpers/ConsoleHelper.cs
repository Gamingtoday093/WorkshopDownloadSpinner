using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkshopDownloadSpinner.Helpers
{
    public static class ConsoleHelper
    {
        public static bool DiscardConsoleInput
        {
            get;
            set
            {
                if (field == value) return;
                field = value;

                if (DiscardConsoleInput)
                {
                    Task.Run(() => DiscardConsoleInputs(DiscardTokenSource.Token));
                }
                else
                {
                    DiscardTokenSource.Cancel();
                    DiscardTokenSource = new();
                }
            }
        }

        private static CancellationTokenSource DiscardTokenSource { get; set; } = new();

        private static Task DiscardConsoleInputs(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
            }
            return Task.CompletedTask;
        }
    }
}
