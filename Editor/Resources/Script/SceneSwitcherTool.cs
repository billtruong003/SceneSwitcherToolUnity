using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class SceneSwitcherTool : EditorWindow
{
    private List<SceneAsset> bookmarkedScenes = new List<SceneAsset>();
    private string selectedScene = null;
    private string searchQuery = "";
    private Vector2 scenesInBuildScrollPosition;
    private Vector2 bookmarkedScenesScrollPosition;
    private double lastClickTime = 0;
    private const float doubleClickThreshold = 0.3f; // Adjust the threshold as needed
    private string lastClickedScene = null;

    private const string BookmarkedScenesKey = "BookmarkedScenes";

    [MenuItem("Tools/Bill Utils/Scene Switcher Tool")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcherTool>("Scene Switcher Tool");
    }

    private void OnEnable()
    {
        LoadBookmarkedScenes();
    }

    private void OnGUI()
    {
        GUILayout.Label("Search Scenes", EditorStyles.boldLabel);
        searchQuery = EditorGUILayout.TextField(searchQuery);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        // Start drawing border for the Scenes in Build section
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)); // Make it responsive

        GUILayout.Label("Scenes in Build", EditorStyles.boldLabel);

        scenesInBuildScrollPosition = GUILayout.BeginScrollView(scenesInBuildScrollPosition, GUILayout.ExpandHeight(true));

        // Display scenes in build settings
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

            if (!string.IsNullOrEmpty(searchQuery) && !sceneName.ToLower().Contains(searchQuery.ToLower()))
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            GUIContent content = new GUIContent(sceneName, EditorGUIUtility.IconContent("d_UnityEditor.GameView").image);
            bool isSelected = GUILayout.Toggle(selectedScene == sceneName, content, "Button");

            if (isSelected)
            {
                selectedScene = sceneName;
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace(); // Add flexible space to balance height

        GUILayout.EndVertical();

        GUILayout.Space(10);

        // Start drawing border for the Bookmarked Scenes section
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)); // Make it responsive

        GUILayout.Label("Bookmarked Scenes", EditorStyles.boldLabel);

        bookmarkedScenesScrollPosition = GUILayout.BeginScrollView(bookmarkedScenesScrollPosition, GUILayout.ExpandHeight(true));

        // Display bookmarked scenes
        for (int i = 0; i < bookmarkedScenes.Count; i++)
        {
            SceneAsset sceneAsset = bookmarkedScenes[i];
            if (sceneAsset == null)
            {
                continue;
            }

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (!string.IsNullOrEmpty(searchQuery) && !sceneName.ToLower().Contains(searchQuery.ToLower()))
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            GUIContent content = new GUIContent(sceneName, EditorGUIUtility.IconContent("d_UnityEditor.GameView").image);
            if (GUILayout.Button(content))
            {
                HandleBookmarkButtonClick(sceneName);
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace(); // Add flexible space to balance height

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Start drawing border for the fixed button section
        GUILayout.BeginVertical("box", GUILayout.MaxWidth(150), GUILayout.ExpandHeight(true));

        GUILayout.Space(10);

        // Title for the Play button
        GUILayout.Label("Play Scene", EditorStyles.boldLabel);

        // Button to switch scenes
        if (GUILayout.Button(new GUIContent("Play", EditorGUIUtility.IconContent("d_PlayButton").image), GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"Play button clicked for scene: {selectedScene}");
                if (EditorApplication.isPlaying)
                {
                    SceneManager.LoadScene(selectedScene);
                }
                else
                {
                    string scenePath = GetScenePathByName(selectedScene);
                    EditorSceneManager.OpenScene(scenePath);
                    EditorApplication.isPlaying = true;
                }
            }
        }

        // Title for other buttons
        GUILayout.Label("Scene Options", EditorStyles.boldLabel);

        // Button to switch scenes additively
        if (GUILayout.Button(new GUIContent("Additive", EditorGUIUtility.IconContent("d_Toolbar Plus More").image), GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"Additive button clicked for scene: {selectedScene}");
                string scenePath = GetScenePathByName(selectedScene);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
        }

        // Button to load scenes
        if (GUILayout.Button(new GUIContent("Load", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"Load button clicked for scene: {selectedScene}");
                string scenePath = GetScenePathByName(selectedScene);
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        // Button to bookmark scenes
        if (GUILayout.Button(new GUIContent("Bookmark", EditorGUIUtility.IconContent("Favorite").image), GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"Bookmark button clicked for scene: {selectedScene}");
                string scenePath = GetScenePathByName(selectedScene);
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null && !bookmarkedScenes.Contains(sceneAsset))
                {
                    bookmarkedScenes.Add(sceneAsset);
                    SaveBookmarkedScenes();
                }
            }
        }

        // Button to remove all bookmarked scenes
        if (GUILayout.Button(new GUIContent("Remove All Bookmarks", EditorGUIUtility.IconContent("d_CacheServerDisconnected@2x").image), GUILayout.Height(30)))
        {
            Debug.Log("Remove All Bookmarks button clicked");
            RemoveAllBookmarkedScenes();
        }

        GUILayout.EndVertical();
        // End drawing border for the fixed button section
    }

    private void HandleBookmarkButtonClick(string sceneName)
    {
        double currentTime = EditorApplication.timeSinceStartup;
        Debug.Log($"HandleBookmarkButtonClick - sceneName: {sceneName}, currentTime: {currentTime}");
        if (lastClickedScene == sceneName && (currentTime - lastClickTime) < doubleClickThreshold)
        {
            Debug.Log($"Double-click detected for scene: {sceneName}");
            string scenePath = GetScenePathByName(sceneName);
            EditorSceneManager.OpenScene(scenePath);
            lastClickedScene = null; // Reset to prevent continuous loading
        }
        else
        {
            lastClickedScene = sceneName;
            lastClickTime = currentTime;
            Debug.Log($"Single click - setting lastClickedScene: {sceneName}, lastClickTime: {lastClickTime}");
        }
    }

    private void LoadScene(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        if (EditorApplication.isPlaying)
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            string scenePath = GetScenePathByName(sceneName);
            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.isPlaying = true;
        }
    }

    private string GetScenePathByName(string sceneName)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(scene.path) == sceneName)
            {
                return scene.path;
            }
        }
        return null;
    }

    private void LoadBookmarkedScenes()
    {
        bookmarkedScenes.Clear();
        string[] sceneGUIDs = EditorPrefs.GetString(BookmarkedScenesKey, "").Split(';');
        foreach (string guid in sceneGUIDs)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (sceneAsset != null)
                {
                    bookmarkedScenes.Add(sceneAsset);
                }
            }
        }
    }

    private void SaveBookmarkedScenes()
    {
        List<string> sceneGUIDs = new List<string>();
        foreach (SceneAsset sceneAsset in bookmarkedScenes)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            sceneGUIDs.Add(guid);
        }
        EditorPrefs.SetString(BookmarkedScenesKey, string.Join(";", sceneGUIDs));
    }

    private void RemoveAllBookmarkedScenes()
    {
        Debug.Log("Removing all bookmarked scenes");
        bookmarkedScenes.Clear();
        EditorPrefs.DeleteKey(BookmarkedScenesKey);
    }
}
