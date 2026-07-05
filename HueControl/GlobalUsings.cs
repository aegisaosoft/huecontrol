// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

// WinForms is referenced only for the native colour dialog, but its implicit
// usings pull in System.Drawing / System.Windows.Forms, which collide with WPF
// types. These aliases make the WPF types win everywhere by default; the few
// WinForms usages are fully qualified or aliased at their call sites.
global using Application = System.Windows.Application;
global using Color = System.Windows.Media.Color;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using MessageBox = System.Windows.MessageBox;
