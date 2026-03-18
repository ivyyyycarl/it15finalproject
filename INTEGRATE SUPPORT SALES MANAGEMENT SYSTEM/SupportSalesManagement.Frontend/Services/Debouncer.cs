using System;
using System.Threading;
using System.Threading.Tasks;

namespace SupportSalesManagement.Frontend.Services
{
    public sealed class Debouncer : IDisposable
    {
        private CancellationTokenSource? _cts;

        public async Task DebounceAsync(Func<Task> action, int delayMs = 400)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await action();
            }
            catch (TaskCanceledException)
            {
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
