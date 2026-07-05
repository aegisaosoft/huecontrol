// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

namespace HueControl.Mvvm;

/// <summary>
/// Coalesces a burst of rapid calls into a single deferred action — each new call
/// cancels the previous pending one, so only the last value in a burst runs. Used to
/// stop slider drags from flooding the bridge with per-pixel requests.
/// </summary>
public sealed class Debouncer
{
    private CancellationTokenSource? _cts;

    public void Run(int delayMs, Func<Task> action)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = RunAsync(delayMs, action, cts.Token);
    }

    private static async Task RunAsync(int delayMs, Func<Task> action, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delayMs, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!ct.IsCancellationRequested)
            await action();
    }
}
