using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class DirectAssetScanner : EditorWindow
{
    private Vector2 scroll;
    private List<string> unusedAssets = new();
    private Dictionary<string, bool> selection = new();
    private string search = "";
    private bool moveToTrash = true;

    [MenuItem("Tools/Direct Asset Scanner")]
    public static void Open()
    {
        var win = GetWindow<DirectAssetScanner>("Unused Asset Scanner");
        win.minSize = new Vector2(600, 400);
        win.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        if (GUILayout.Button("Scan Assets Folder (No Build Needed)", GUILayout.Height(30)))
            Scan();

        EditorGUILayout.Space();
        search = EditorGUILayout.TextField("Filter", search);

        EditorGUILayout.Space();
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scrollView.scrollPosition;

            foreach (var asset in unusedAssets.Where(p => p.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool curr = selection.TryGetValue(asset, out var val) && val;
                    bool next = EditorGUILayout.ToggleLeft(asset, curr);
                    if (next != curr) selection[asset] = next;

                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<Object>(asset);
                        if (obj != null) EditorGUIUtility.PingObject(obj);
                    }
                }
            }
        }

        EditorGUILayout.Space();
        moveToTrash = EditorGUILayout.ToggleLeft("Move to Trash", moveToTrash);

        GUI.enabled = selection.Any(kv => kv.Value);
        if (GUILayout.Button("Purge Selected", GUILayout.Height(30)))
            Purge();
        GUI.enabled = true;
    }

    private void Scan()
    {
        unusedAssets.Clear();
        selection.Clear();

        var allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)).ToList();

        var allTextFiles = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".prefab") || f.EndsWith(".unity") || f.EndsWith(".mat") || f.EndsWith(".asset") || f.EndsWith(".controller"))
            .ToList();

        var referencedPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var file in allTextFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var asset in allAssets)
            {
                var guid = AssetDatabase.AssetPathToGUID(asset);
                if (!string.IsNullOrEmpty(guid) && content.Contains(guid))
                    referencedPaths.Add(asset);
            }
        }

        foreach (var asset in allAssets)
        {
            if (!referencedPaths.Contains(asset))
            {
                unusedAssets.Add(asset);
                // Auto-select all detected unused assets by default
                selection[asset] = true;
            }
        }

        Debug.Log($"Scan complete. {unusedAssets.Count} unused assets found. All auto-selected.");
    }

    private void Purge()
    {
        var targets = selection.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        if (targets.Count == 0) return;

        int purged = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in targets)
            {
                bool ok = moveToTrash ? AssetDatabase.MoveAssetToTrash(path) : AssetDatabase.DeleteAsset(path);
                if (ok)
                {
                    purged++;
                    selection.Remove(path);
                    unusedAssets.Remove(path);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Purged {purged} asset(s).");
    }
}