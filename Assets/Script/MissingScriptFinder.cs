#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    public static void FindInScene()
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int found = 0;

        foreach (GameObject go in all)
        {
            Component[] comps = go.GetComponents<Component>();

            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                {
                    Debug.LogWarning($"Missing script on: '{GetPath(go)}' (component index {i})", go);
                    found++;
                }
            }
        }

        Debug.Log($"Scan complete. Found {found} missing script slots.");
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;

        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }

        return path;
    }
}
#endif