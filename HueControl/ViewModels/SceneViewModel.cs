// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Input;
using System.Windows.Media;
using HueControl.Models;
using HueControl.Mvvm;
using HueControl.Services;

namespace HueControl.ViewModels;

/// <summary>Represents a Hue scene that can be recalled with a single click.</summary>
public sealed class SceneViewModel : ObservableObject
{
    private readonly HueApiClient _client;
    private readonly Action<Exception> _onError;

    public SceneViewModel(
        string id,
        HueSceneDto dto,
        string? roomName,
        HueApiClient client,
        Action<Exception> onError)
    {
        Id = id;
        _client = client;
        _onError = onError;
        Name = dto.Name;
        Group = dto.Group;
        RoomName = roomName ?? (string.IsNullOrEmpty(dto.Group) ? "Custom" : "Room");
        LightCount = dto.Lights.Count;

        ActivateCommand = new AsyncRelayCommand(ActivateAsync);
    }

    public string Id { get; }
    public string Name { get; }
    public string? Group { get; }
    public string RoomName { get; }
    public int LightCount { get; }

    /// <summary>Our own gradient preview built from the scene's colours.</summary>
    public Brush Thumbnail => SceneThumbnails.ForName(Name);

    public ICommand ActivateCommand { get; }

    private async Task ActivateAsync(object? _)
    {
        try
        {
            await _client.ActivateSceneAsync(Id, Group);
        }
        catch (Exception ex)
        {
            _onError(ex);
        }
    }
}
