using System;
using System.Collections.Generic;
using Azzazelloqq.Config;
using UnityEngine;

namespace Code.Configuration
{
	[CreateAssetMenu(menuName = "DungeonTeam/Configuration/Config Catalog", fileName = "ConfigCatalog")]
	public sealed class ConfigCatalog : ScriptableObject
	{
		[SerializeField]
		private ConfigPage[] _pages = Array.Empty<ConfigPage>();

		public IConfigPage[] GetPages()
		{
			var pageTypes = new HashSet<Type>();
			var pages = new IConfigPage[_pages.Length];

			for (var index = 0; index < _pages.Length; index++)
			{
				var page = _pages[index];
				if (page == null)
					throw new InvalidOperationException($"Config catalog '{name}' has a missing page at index {index}.");

				if (!pageTypes.Add(page.GetType()))
					throw new InvalidOperationException($"Config catalog '{name}' contains multiple pages of type {page.GetType().Name}.");

				pages[index] = page;
			}

			return pages;
		}
	}
}
