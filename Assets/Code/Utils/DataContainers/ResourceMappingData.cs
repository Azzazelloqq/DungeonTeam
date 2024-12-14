using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Utils.DataContainers
{
[Serializable]
public struct ResourceMapping
{
	[field: SerializeField]
	public string ObjectId { get; private set; }
	
	[field: SerializeField]
	public string AddressableKey { get; private set; }
}

[Serializable]
public struct ResourceList
{
	[field: SerializeField]
	public string ListId { get; private set; }
	
	[field: SerializeField]
	public ResourceMapping[] Resources { get; private set; }
}

[CreateAssetMenu(fileName = "ResourceMappingData", menuName = "Data/ResourceMapping")]
public class ResourceMappingData : ScriptableObject
{
	[SerializeField]
	private ResourceList[] _resources;

	public IReadOnlyDictionary<string, string> AddressableKeyByObjectId => _addressableKeyByObjectId;
	public bool IsResourcesCashed { get; private set; }
	
	private Dictionary<string, string> _addressableKeyByObjectId;
	
	public void CashResources()
	{
		_addressableKeyByObjectId = new Dictionary<string, string>();
		
		foreach (var resourceList in _resources)
		{
			foreach (var resourceMap in resourceList.Resources)
			{
				_addressableKeyByObjectId[resourceMap.ObjectId] = resourceMap.AddressableKey;
			}
		}
		
		IsResourcesCashed = true;
	}

	public string GetResourceId(string objectId)
	{
		//just for test (to remove)
		return "TestHero";
		if (!IsResourcesCashed)
		{
			return _addressableKeyByObjectId[objectId];
		}

		foreach (var resourceList in _resources)
		{
			foreach (var resourceMap in resourceList.Resources)
			{
				if (resourceMap.ObjectId != objectId)
				{
					continue;
				}

				return resourceMap.AddressableKey;
			}
		}
		
		Debug.LogError("Resource with id " + objectId + " not found");
		return string.Empty;
	}
}
}