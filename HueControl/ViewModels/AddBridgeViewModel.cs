// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.Windows.Input;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Drives the "add bridge" dialog: discovery, manual IP entry and link-button pairing.</summary>
public sealed class AddBridgeViewModel : ObservableObject
{
    private readonly BridgeDiscoveryService _discovery = new();

    private DiscoveredBridge? _selectedBridge;
    private string _manualIp = string.Empty;
    private string _homeName;
    private string _statusMessage = "Discover bridges, or type an IP address, then press Pair.";
    private bool _isBusy;

    public AddBridgeViewModel()
        : this(Array.Empty<string>())
    {
    }

    public AddBridgeViewModel(IEnumerable<string> homeNames)
    {
        HomeNames = new ObservableCollection<string>(homeNames);
        _homeName = HomeNames.FirstOrDefault() ?? "My Home";

        DiscoverCommand = new AsyncRelayCommand(DiscoverAsync, () => !IsBusy);
        PairCommand = new AsyncRelayCommand(PairAsync, () => !IsBusy && HasTarget);
    }

    public ObservableCollection<DiscoveredBridge> DiscoveredBridges { get; } = new();

    /// <summary>Existing home names to choose from; the field is editable to create a new one.</summary>
    public ObservableCollection<string> HomeNames { get; }

    /// <summary>The home the paired bridge should be placed in (existing or new).</summary>
    public string HomeName
    {
        get => _homeName;
        set => SetProperty(ref _homeName, value);
    }

    public ICommand DiscoverCommand { get; }
    public ICommand PairCommand { get; }

    /// <summary>Set when pairing succeeds; the dialog reads this and closes.</summary>
    public SavedBridge? Result { get; private set; }

    public event EventHandler? PairingSucceeded;

    public DiscoveredBridge? SelectedBridge
    {
        get => _selectedBridge;
        set
        {
            if (SetProperty(ref _selectedBridge, value) && value is not null)
                ManualIp = value.IpAddress;
        }
    }

    public string ManualIp
    {
        get => _manualIp;
        set => SetProperty(ref _manualIp, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    private bool HasTarget => !string.IsNullOrWhiteSpace(ManualIp);

    private async Task DiscoverAsync()
    {
        IsBusy = true;
        StatusMessage = "Searching the network for bridges…";
        try
        {
            DiscoveredBridges.Clear();
            var found = await _discovery.DiscoverAsync();
            foreach (var bridge in found)
                DiscoveredBridges.Add(bridge);

            StatusMessage = DiscoveredBridges.Count > 0
                ? $"Found {DiscoveredBridges.Count} bridge(s). Select one or enter an IP, then Pair."
                : "No bridges found automatically. Enter the bridge IP manually.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PairAsync()
    {
        string ip = ManualIp.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            StatusMessage = "Enter a bridge IP address first.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Pairing with {ip} — press the link button on the bridge now…";
        try
        {
            string key = await HueApiClient.PairAsync(ip);
            var client = new HueApiClient(ip, key);
            string name = await client.GetBridgeNameAsync();

            Result = new SavedBridge
            {
                Id = SelectedBridge?.Id ?? ip,
                Name = name,
                IpAddress = ip,
                AppKey = key,
            };

            StatusMessage = $"Paired with {name}.";
            PairingSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (LinkButtonNotPressedException)
        {
            StatusMessage = "Link button not detected. Press it on the bridge, then click Pair again.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Pairing failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
