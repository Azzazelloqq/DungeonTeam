using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Code.Editor
{
public static class AddressableKeysGenerator
{
	private const string OutputFolder = "Assets/Code/Generated/AddressablesGroups";
	private const string NamespaceName = "Code.Generated.Addressables";
	private const string MainClassName = "ResourceIdsContainer";


	[MenuItem("Tools/Generate Addressable Groups (CamelCase)")]
	public static void Generate()
	{
		CleanOldFiles();

		var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
		if (settings == null)
		{
			Debug.LogError("AddressableAssetSettings not found.");
			return;
		}

		if (!Directory.Exists(OutputFolder))
		{
			Directory.CreateDirectory(OutputFolder);
		}

		var groupClassNames = new List<string>();

		foreach (var group in settings.groups)
		{
			if (group == null || group.entries == null)
			{
				continue;
			}

			var entries = group.entries
				.Where(e => e != null && !string.IsNullOrEmpty(e.address))
				.Distinct()
				.ToList();

			var className = ToCamelCase(group.Name, true);
			var filePath = Path.Combine(OutputFolder, $"{className}.cs");

			var sb = new StringBuilder();
			sb.AppendLine("using System;");
			sb.AppendLine();
			sb.AppendLine($"namespace {NamespaceName}");
			sb.AppendLine("{");
			sb.AppendLine($"    public class {className}");
			sb.AppendLine("    {");

			foreach (var entry in entries)
			{
				var fieldName = ToCamelCaseIdentifier(entry.address);
				sb.AppendLine($"        public const string {fieldName} = \"{entry.address}\";");
			}

			sb.AppendLine("    }");
			sb.AppendLine("}");

			File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
			Debug.Log($"Generated {className}.cs for group '{group.Name}' at {filePath}");

			groupClassNames.Add(className);
		}

		GenerateMainContainer(groupClassNames);
		AssetDatabase.Refresh();
	}

	private static void GenerateMainContainer(List<string> groupClassNames)
	{
		var filePath = Path.Combine(OutputFolder, $"{MainClassName}.cs");

		var sb = new StringBuilder();
		sb.AppendLine("using System;");
		sb.AppendLine($"using {NamespaceName};");
		sb.AppendLine();
		sb.AppendLine($"namespace {NamespaceName}");
		sb.AppendLine("{");
		sb.AppendLine($"    public static class {MainClassName}");
		sb.AppendLine("    {");

		foreach (var className in groupClassNames)
		{
			// Каждый класс группы становится readonly полем
			sb.AppendLine($"        public static readonly {className} {className} = new {className}();");
		}

		sb.AppendLine("    }");
		sb.AppendLine("}");

		File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
		Debug.Log($"Generated main container {MainClassName}.cs with {groupClassNames.Count} groups at {filePath}");
	}

	private static void CleanOldFiles()
	{
		if (Directory.Exists(OutputFolder))
		{
			var existingFiles = Directory.GetFiles(OutputFolder, "*.cs", SearchOption.TopDirectoryOnly);
			foreach (var f in existingFiles)
			{
				File.Delete(f);
			}
		}
	}

	private static string ToCamelCaseIdentifier(string address)
	{
		var cleaned = ReplaceDelimitersWithSpace(address);

		cleaned = EnsureStartsWithLetter(cleaned);

		return JoinPartsToCamelCase(cleaned);
	}

	private static string ToCamelCase(string input, bool isGroupName = false)
	{
		var cleaned = ReplaceDelimitersWithSpace(input);

		cleaned = EnsureStartsWithLetter(cleaned);

		return JoinPartsToCamelCase(cleaned);
	}

	private static string ReplaceDelimitersWithSpace(string input)
	{
		char[] delimiters = { ' ', '_', '-', '.', '/', '\\' };
		var sb = new StringBuilder(input.Length);
		foreach (var c in input)
		{
			if (delimiters.Contains(c))
			{
				sb.Append(' ');
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.ToString().Trim();
	}

	private static string EnsureStartsWithLetter(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "Id";
		}

		var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
		if (parts.Count == 0)
		{
			return "Id";
		}

		if (!char.IsLetter(parts[0], 0))
		{
			// Добавим новую часть "Id" в начало
			parts.Insert(0, "Id");
		}

		return string.Join(" ", parts);
	}

	private static string JoinPartsToCamelCase(string input)
	{
		var parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

		for (var i = 0; i < parts.Length; i++)
		{
			parts[i] = CapitalizeFirstLetter(parts[i]);
		}

		return string.Join(string.Empty, parts);
	}

	private static string CapitalizeFirstLetter(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}

		if (s.Length == 1)
		{
			return s.ToUpperInvariant();
		}

		return char.ToUpperInvariant(s[0]) + s.Substring(1);
	}
}
}