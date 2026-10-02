// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HueControl.Services;

namespace HueControl.Tests;

/// <summary>
/// Guards the translation table: every key is translated into every language with the same
/// placeholders, and every key the UI asks for actually exists (a missing key renders as the key itself).
/// </summary>
public class LocalizationTests
{
    [Fact]
    public void EveryKey_HasNonEmptyTranslation_ForEveryLanguage()
    {
        foreach (var (key, values) in Strings.All)
        {
            Assert.True(values.Length == Loc.Languages.Length, $"{key}: {values.Length} translations");
            Assert.All(values, v => Assert.False(string.IsNullOrWhiteSpace(v), $"{key} has an empty translation"));
        }
    }

    [Fact]
    public void EveryKey_HasSamePlaceholders_InEveryLanguage()
    {
        foreach (var (key, values) in Strings.All)
        {
            var expected = Placeholders(values[0]);
            for (int i = 1; i < values.Length; i++)
                Assert.True(expected.SetEquals(Placeholders(values[i])), $"{key} [{Loc.Languages[i].Code}] placeholders differ");
        }
    }

    [Fact]
    public void EveryKeyUsedInXamlAndCode_ExistsInTable()
    {
        string src = Path.Combine(RepoRoot(), "HueControl");
        var used = Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".xaml") || f.EndsWith(".cs")) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(File.ReadLines)
            .Where(line => !line.TrimStart().StartsWith("//")) // doc comments show {loc:Tr Key} as an example
            .SelectMany(line => Regex.Matches(line, @"\{loc:Tr (\w+)\}|Loc\.T\(""(\w+)""")
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value))
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.All(used, k => Assert.True(Strings.All.ContainsKey(k), $"missing translation key: {k}"));
    }

    [Fact]
    public void Xaml_HasNoUntranslatedStringFormatText()
    {
        string src = Path.Combine(RepoRoot(), "HueControl");
        var offenders = Directory.EnumerateFiles(src, "*.xaml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"StringFormat='([^']*)'")
                .Select(m => m.Groups[1].Value)
                .Where(fmt => Regex.IsMatch(fmt.Replace("{0}", ""), @"\p{L}{2,}"))
                .Select(fmt => $"{Path.GetFileName(f)}: {fmt}"))
            .ToList();

        Assert.True(offenders.Count == 0, "Words inside StringFormat bypass translation: " + string.Join("; ", offenders));
    }

    private static HashSet<string> Placeholders(string s) =>
        Regex.Matches(s, @"\{\d+\}").Select(m => m.Value).ToHashSet();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HueControl.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("HueControl.slnx not found above the test output.");
    }
}
