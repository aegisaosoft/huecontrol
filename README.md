# HueControl

A WPF desktop app (MVVM, light & dark themes) for controlling **and managing**
Philips Hue over the local bridge REST API (v1). Works fully offline on your LAN
— no Hue cloud account required after pairing.

## Features

- **Bridges & homes** — auto-discover on the network (Philips N-UPnP) or add by
  IP; pair via the link button. Group several bridges into **homes** and switch
  instantly. Remembered between sessions (`%AppData%\HueControl\homes.json`).
- **Lights** — on/off toggle, brightness slider, live colour swatch, and a
  native colour picker for colour-capable bulbs; room and light sliders synced.
- **Rooms** — per-room on/off and brightness; on/off per room row in Scenes.
- **Scenes** — one-click recall grouped by room, each with a colour preview; add
  built-in Hue presets (Relax, Concentrate, Energize, Savanna sunset…) or delete
  scenes directly on the page.
- **Settings window** — search for and add **any** Hue device (lights, plugs,
  sensors, dimmer switches, buttons), rename/remove; manage rooms & zones;
  add/delete scenes; accessory battery & last activity; **program a dimmer
  switch** to control a room. Lists group by room/type and are collapsible.
- **Themes** — light and dark, borderless custom window.
- **Quick actions** — All on / All off across every light, plus Refresh.

## Requirements

- Windows, .NET 9 SDK (or the .NET 9 Desktop Runtime to run a published build).

## Build & run

```powershell
cd C:\aegis-ao\HueControl
dotnet run --project HueControl
```

Or run the compiled binary:

```
HueControl\bin\Debug\net9.0-windows\HueControl.exe
```

## First use

1. Click **+ Add bridge**.
2. Click **Discover on network** (or type the bridge IP, e.g. `192.168.1.x`).
3. **Press the round link button on the bridge**, then click **Pair**.
4. The bridge appears in the sidebar; its lights, rooms and scenes load
   automatically.

Several bridges can be added and switched between from the sidebar.

## Project layout

```
HueControl/
  Models/        DTOs for the Hue datastore + saved-bridge/home models
  Services/      HueApiClient, discovery, settings store, colour math,
                 ThemeManager, SwitchProgrammer, HueScenePresets, thumbnails
  Mvvm/          ObservableObject, RelayCommand / AsyncRelayCommand, Grouping
  ViewModels/    Main, Light, Group, Scene, Home, Settings + Devices /
                 Accessories / Rooms / Scenes sections
  Themes/        Dark.xaml, Light.xaml (swappable palettes)
  Views          MainWindow, SettingsWindow, AddBridgeWindow,
                 InputDialog, ScenePickerDialog, App (styles)
```
