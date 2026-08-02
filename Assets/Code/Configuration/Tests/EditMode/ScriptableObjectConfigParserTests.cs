using System;
using Azzazelloqq.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Configuration.Tests
{
	public sealed class ScriptableObjectConfigParserTests
	{
		[Test]
		public void Initialize_WithCatalogPage_ExposesPageByConcreteType()
		{
			var catalog = CreateCatalog(out var page);

			try
			{
				var config = new Config(new ScriptableObjectConfigParser(catalog));
				config.Initialize();

				Assert.That(config.GetConfigPage<TestConfigPage>(), Is.SameAs(page));
			}
			finally
			{
				Destroy(catalog, page);
			}
		}

		[Test]
		public void Initialize_WithDuplicatePageTypes_Throws()
		{
			var first = ScriptableObject.CreateInstance<TestConfigPage>();
			var second = ScriptableObject.CreateInstance<TestConfigPage>();
			var catalog = CreateCatalog(first, second);

			try
			{
				var config = new Config(new ScriptableObjectConfigParser(catalog));

				Assert.That(
					() => config.Initialize(),
					Throws.TypeOf<InvalidOperationException>().With.Message.Contains("multiple pages"));
			}
			finally
			{
				Destroy(catalog, first, second);
			}
		}

		private static ConfigCatalog CreateCatalog(out TestConfigPage page)
		{
			page = ScriptableObject.CreateInstance<TestConfigPage>();
			return CreateCatalog(page);
		}

		private static ConfigCatalog CreateCatalog(params ConfigPage[] pages)
		{
			var catalog = ScriptableObject.CreateInstance<ConfigCatalog>();
			var serializedCatalog = new SerializedObject(catalog);
			var serializedPages = serializedCatalog.FindProperty("_pages");
			serializedPages.arraySize = pages.Length;

			for (var index = 0; index < pages.Length; index++)
				serializedPages.GetArrayElementAtIndex(index).objectReferenceValue = pages[index];

			serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
			return catalog;
		}

		private static void Destroy(params Object[] objects)
		{
			foreach (var target in objects)
				Object.DestroyImmediate(target);
		}

		private sealed class TestConfigPage : ConfigPage
		{
		}
	}
}
