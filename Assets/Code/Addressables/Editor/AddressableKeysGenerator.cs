using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Code.Addressables.Editor
{
    public static class AddressableKeysGenerator
    {
        private const string OutputFilePath = "Assets/Code/Addressables/Generated/AddressableIds.g.cs";
        private const string GeneratedNamespace = "Code.Addressables.Generated";
        private const string RootClassName = "AddressableIds";

        [MenuItem("Tools/Addressables/Generate Resource IDs")]
        public static void Generate()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Configure Addressables before generating resource IDs.");
                return;
            }

            if (!TryGenerateSource(settings, out var source, out var errors))
            {
                Debug.LogError($"Addressable resource ID generation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
                return;
            }

            var outputDirectory = Path.GetDirectoryName(OutputFilePath);
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(OutputFilePath, source, new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log($"Generated Addressables resource IDs at '{OutputFilePath}'.");
        }

        [MenuItem("Tools/Addressables/Log Addresses")]
        public static void LogAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Configure Addressables before logging addresses.");
                return;
            }

            foreach (var group in GetGroups(settings).OrderBy(group => group.Name, StringComparer.Ordinal))
            {
                foreach (var entry in GetEntries(group).OrderBy(entry => entry.address, StringComparer.Ordinal))
                {
                    Debug.Log($"Group: {group.Name} → Address: {entry.address}");
                }
            }
        }

        private static bool TryGenerateSource(
            AddressableAssetSettings settings,
            out string source,
            out List<string> errors)
        {
            errors = new List<string>();
            var groups = new List<GeneratedGroup>();
            var groupNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var group in GetGroups(settings))
            {
                if (string.IsNullOrWhiteSpace(group.Name))
                {
                    errors.Add("An Addressables group has an empty name.");
                    continue;
                }

                var entries = new List<GeneratedEntry>();
                var entryNames = new HashSet<string>(StringComparer.Ordinal);
                var addresses = new HashSet<string>(StringComparer.Ordinal);

                foreach (var entry in GetEntries(group))
                {
                    if (string.IsNullOrWhiteSpace(entry.address))
                    {
                        errors.Add($"Group '{group.Name}' contains an entry with an empty address.");
                        continue;
                    }

                    if (!addresses.Add(entry.address))
                    {
                        errors.Add($"Group '{group.Name}' contains duplicate address '{entry.address}'.");
                        continue;
                    }

                    var generatedEntryName = ToPascalIdentifier(entry.address);
                    if (!entryNames.Add(generatedEntryName))
                    {
                        errors.Add($"Addresses in group '{group.Name}' produce the same generated name '{generatedEntryName}'. Rename address '{entry.address}'.");
                        continue;
                    }

                    entries.Add(new GeneratedEntry(generatedEntryName, entry.address));
                }

                if (entries.Count > 0)
                {
                    var generatedGroupName = ToPascalIdentifier(group.Name);
                    if (!groupNames.Add(generatedGroupName))
                    {
                        errors.Add($"Groups produce the same generated name '{generatedGroupName}'. Rename group '{group.Name}'.");
                        continue;
                    }

                    groups.Add(new GeneratedGroup(generatedGroupName, entries));
                }
            }

            if (errors.Count > 0)
            {
                source = null;
                return false;
            }

            source = BuildSource(groups);
            return true;
        }

        private static string BuildSource(IEnumerable<GeneratedGroup> groups)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine($"namespace {GeneratedNamespace}");
            builder.AppendLine("{");
            builder.AppendLine($"    public static class {RootClassName}");
            builder.AppendLine("    {");

            foreach (var group in groups.OrderBy(group => group.Name, StringComparer.Ordinal))
            {
                builder.AppendLine($"        public static class {group.Name}");
                builder.AppendLine("        {");

                foreach (var entry in group.Entries.OrderBy(entry => entry.Name, StringComparer.Ordinal))
                {
                    builder.AppendLine($"            public const string {entry.Name} = \"{EscapeStringLiteral(entry.Address)}\";");
                }

                builder.AppendLine("        }");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static IEnumerable<AddressableAssetGroup> GetGroups(AddressableAssetSettings settings)
        {
            return settings.groups?.Where(group => group != null) ?? Enumerable.Empty<AddressableAssetGroup>();
        }

        private static IEnumerable<AddressableAssetEntry> GetEntries(AddressableAssetGroup group)
        {
            return group.entries?.Where(entry => entry != null) ?? Enumerable.Empty<AddressableAssetEntry>();
        }

        private static string ToPascalIdentifier(string value)
        {
            var builder = new StringBuilder(value.Length);
            var capitalizeNext = true;

            foreach (var character in value)
            {
                if (!char.IsLetterOrDigit(character))
                {
                    capitalizeNext = true;
                    continue;
                }

                if (builder.Length == 0 && char.IsDigit(character))
                {
                    builder.Append("Id");
                }

                builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
                capitalizeNext = false;
            }

            return builder.Length == 0 ? "Id" : builder.ToString();
        }

        private static string EscapeStringLiteral(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(character))
                        {
                            builder.AppendFormat("\\u{0:X4}", (int)character);
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private sealed class GeneratedGroup
        {
            public GeneratedGroup(string name, IReadOnlyList<GeneratedEntry> entries)
            {
                Name = name;
                Entries = entries;
            }

            public string Name { get; }

            public IReadOnlyList<GeneratedEntry> Entries { get; }
        }

        private sealed class GeneratedEntry
        {
            public GeneratedEntry(string name, string address)
            {
                Name = name;
                Address = address;
            }

            public string Name { get; }

            public string Address { get; }
        }
    }
}
