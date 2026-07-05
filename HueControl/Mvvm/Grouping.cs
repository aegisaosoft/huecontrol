// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.ComponentModel;
using System.Windows.Data;

namespace HueControl.Mvvm;

/// <summary>Helper to re-apply grouping levels to a collection view.</summary>
public static class Grouping
{
    /// <summary>Replaces the view's grouping with the given property names (none = flat).</summary>
    public static void Apply(ICollectionView view, params string[] properties)
    {
        using (view.DeferRefresh())
        {
            view.GroupDescriptions.Clear();
            foreach (string property in properties)
                view.GroupDescriptions.Add(new PropertyGroupDescription(property));
        }
    }
}
