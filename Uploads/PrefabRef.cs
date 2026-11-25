using System;
using System.IO;
using UnityEngine;

[Serializable]
public class PrefabRef<T> where T : Object
{
	[SerializeField]
	private string prefabName;

	[SerializeField]
	private string resourcePath;

	[SerializeField]
	private AssetBundle bundle;

	[SerializeField]
	private bool cachedFinalized = true;

	public string PrefabName => prefabName;

	public string ResourcePath
	{
		get
		{
			if (!Object.op_Implicit((Object)(object)bundle))
			{
				return resourcePath;
			}
			return "AssetBundles/" + ((Object)bundle).name + "/" + resourcePath;
		}
	}

	public AssetBundle Bundle => bundle;

	public bool IsFinalized => cachedFinalized;

	public T Prefab
	{
		get
		{
			if (string.IsNullOrEmpty(resourcePath))
			{
				return default;
			}
			string key = ((!Object.op_Implicit((Object)(object)bundle)) ? resourcePath : ResourcePath);
			if (RunManager.instance.singleplayerPool.TryGetValue(key, out var value))
			{
				return (T)(object)((value is T) ? value : null);
			}
			if (RunManager.instance.multiplayerPool.ResourceCache.TryGetValue(key, out var value2))
			{
				return (T)(object)((value2 is T) ? value2 : null);
			}
			T val = ((!Object.op_Implicit((Object)(object)bundle)) ? Resources.Load<T>(resourcePath) : bundle.LoadAsset<T>(resourcePath));
			if ((Object)(object)val == (Object)null)
			{
				if (Object.op_Implicit((Object)(object)bundle))
				{
					Debug.LogError((object)("PrefabRef failed to load \"" + resourcePath + "\" as " + typeof(T).Name + " from \"" + ((Object)bundle).name + "\" asset bundle."));
				}
				else
				{
					Debug.LogError((object)("PrefabRef failed to load \"" + resourcePath + "\" as " + typeof(T).Name + ". Make sure it's in a \"Resources\" folder."));
				}
				return default;
			}
			RunManager.instance.singleplayerPool.Add(key, (Object)(object)val);
			return val;
		}
	}

	public bool IsValid()
	{
		return !string.IsNullOrEmpty(resourcePath);
	}

	public void SetPrefab(T _prefab, string _resourcePath = null)
	{
		if ((Object)(object)_prefab != (Object)null)
		{
			prefabName = ((Object)_prefab).name;
			resourcePath = _resourcePath;
		}
		else
		{
			prefabName = null;
			resourcePath = null;
		}
	}

	public void SetPrefab(AssetBundle _bundle, string _resourcePath = null)
	{
		bundle = _bundle;
		if ((Object)(object)bundle != (Object)null)
		{
			prefabName = Path.GetFileNameWithoutExtension(_resourcePath);
			resourcePath = _resourcePath;
		}
		else
		{
			prefabName = null;
			resourcePath = null;
		}
	}
}
[Serializable]
public class PrefabRef : PrefabRef<GameObject>
{
}
