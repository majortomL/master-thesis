using System.Collections;
using UnityEngine;

public static class ExtensionMethods
{
    public static Transform FindChildRecursive(this Transform parent, string name)
    {
		foreach (Transform child in parent)
		{
			if (child.name.Contains(name))
				return child;

			var result = child.FindChildRecursive(name);
			if (result != null)
				return result;
		}
		return null;
	}
}
