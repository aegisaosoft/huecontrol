// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

namespace HueControl.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that is skipped unless <c>HUE_LIVE=1</c> is set,
/// so tests that touch a real bridge on the LAN stay opt-in and deterministic by default.
/// </summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HUE_LIVE") != "1")
            Skip = "Set HUE_LIVE=1 (and optionally HUE_BRIDGE_IP) to run live bridge tests.";
    }
}
