// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Input;
using System.Windows.Media;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>A scene shown in Settings → Scenes, with delete (name is read-only).</summary>
public sealed class SceneItemViewModel : ObservableObject
{
    private readonly Func<SceneItemViewModel, Task> _requestDelete;

    public SceneItemViewModel(string id, string name, string roomName, Func<SceneItemViewModel, Task> requestDelete)
    {
        Id = id;
        Name = name;
        RoomName = roomName;
        _requestDelete = requestDelete;

        DeleteCommand = new AsyncRelayCommand(_ => _requestDelete(this));
    }

    public string Id { get; }
    public string Name { get; }
    public string RoomName { get; }

    /// <summary>Our own gradient preview built from the scene's colours.</summary>
    public Brush Thumbnail => SceneThumbnails.ForName(Name);

    public ICommand DeleteCommand { get; }
}
