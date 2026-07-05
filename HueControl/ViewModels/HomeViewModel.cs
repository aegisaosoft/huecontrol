// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Collections.ObjectModel;
using System.Windows.Input;
using HueControl.Models;
using HueControl.Mvvm;

namespace HueControl.ViewModels;

/// <summary>A bridge entry in the homes sidebar; clickable and highlightable.</summary>
public sealed class BridgeItemViewModel : ObservableObject
{
    private bool _isSelected;

    public BridgeItemViewModel(SavedBridge bridge, Action<BridgeItemViewModel> onSelect)
    {
        Bridge = bridge;
        SelectCommand = new RelayCommand(() => onSelect(this));
    }

    public SavedBridge Bridge { get; }
    public string Name => Bridge.Name;
    public string IpAddress => Bridge.IpAddress;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ICommand SelectCommand { get; }
}

/// <summary>A home grouping several bridges in the sidebar.</summary>
public sealed class HomeViewModel : ObservableObject
{
    public HomeViewModel(
        Home home,
        Action<BridgeItemViewModel> onSelectBridge,
        Action<HomeViewModel> onRename,
        Action<HomeViewModel> onDelete)
    {
        Home = home;
        Bridges = new ObservableCollection<BridgeItemViewModel>(
            home.Bridges.Select(b => new BridgeItemViewModel(b, onSelectBridge)));

        RenameCommand = new RelayCommand(() => onRename(this));
        DeleteCommand = new RelayCommand(() => onDelete(this));
    }

    public Home Home { get; }
    public string Name => Home.Name;
    public ObservableCollection<BridgeItemViewModel> Bridges { get; }

    public ICommand RenameCommand { get; }
    public ICommand DeleteCommand { get; }
}
