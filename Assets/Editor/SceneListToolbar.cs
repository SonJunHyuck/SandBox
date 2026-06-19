using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class SceneListToolbar
{
    private const string ElementPath = "SandBox/SceneList";

    static SceneListToolbar()
    {
        EditorBuildSettings.sceneListChanged += Refresh;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        EditorApplication.playModeStateChanged += _ => Refresh();
    }

    [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 1)]
    private static MainToolbarElement CreateSceneListDropdown()
    {
        string sceneName = GetActiveSceneName();
        var content = new MainToolbarContent(sceneName, "Open a scene from Build Settings");
        return new MainToolbarDropdown(content, ShowSceneMenu);
    }

    private static void ShowSceneMenu(Rect dropdownRect)
    {
        var menu = new GenericMenu();
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        string activePath = GetActiveScenePath();

        if (scenes.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No scenes in Build Settings"));
        }
        else
        {
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                string path = scene.path;
                string label = FormatSceneLabel(i, scene);
                bool isActive = path == activePath;

                menu.AddItem(new GUIContent(label), isActive, () => OpenScene(path));
            }
        }

        menu.AddSeparator("");
        AddActiveSceneItem(menu);
        menu.AddItem(new GUIContent("Open Build Settings"), false, OpenBuildSettings);
        menu.DropDown(dropdownRect);
    }

    private static string FormatSceneLabel(int index, EditorBuildSettingsScene scene)
    {
        string sceneName = string.IsNullOrEmpty(scene.path)
            ? "Missing Scene"
            : Path.GetFileNameWithoutExtension(scene.path);

        return scene.enabled
            ? string.Format("{0}. {1}", index, sceneName)
            : string.Format("{0}. {1} (disabled)", index, sceneName);
    }

    private static void AddActiveSceneItem(GenericMenu menu)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path) || IsInBuildSettings(activeScene.path))
        {
            menu.AddDisabledItem(new GUIContent("Add Active Scene to Build Settings"));
            return;
        }

        menu.AddItem(new GUIContent("Add Active Scene to Build Settings"), false, AddActiveSceneToBuildSettings);
    }

    private static bool IsInBuildSettings(string path)
    {
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path == path)
                return true;
        }

        return false;
    }

    private static void AddActiveSceneToBuildSettings()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path) || IsInBuildSettings(activeScene.path))
            return;

        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        var nextScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
        currentScenes.CopyTo(nextScenes, 0);
        nextScenes[nextScenes.Length - 1] = new EditorBuildSettingsScene(activeScene.path, true);
        EditorBuildSettings.scenes = nextScenes;
        Refresh();
    }

    private static void OpenScene(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        if (Application.isPlaying)
        {
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                SceneManager.LoadScene(sceneName);
            else
                Debug.LogError("Scene '" + sceneName + "' is not enabled in Build Settings.");

            Refresh();
            return;
        }

        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("SceneList", "Scene file is missing:\n" + path, "OK");
            return;
        }

        if (EditorSceneManager.GetActiveScene().path == path)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Refresh();
    }

    private static void OpenBuildSettings()
    {
        if (!EditorApplication.ExecuteMenuItem("File/Build Profiles"))
            EditorApplication.ExecuteMenuItem("File/Build Settings...");
    }

    private static string GetActiveSceneName()
    {
        Scene scene = Application.isPlaying
            ? SceneManager.GetActiveScene()
            : EditorSceneManager.GetActiveScene();

        return string.IsNullOrEmpty(scene.name) ? "SceneList" : scene.name;
    }

    private static string GetActiveScenePath()
    {
        return Application.isPlaying
            ? SceneManager.GetActiveScene().path
            : EditorSceneManager.GetActiveScene().path;
    }

    private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        Refresh();
    }

    private static void Refresh()
    {
        MainToolbar.Refresh(ElementPath);
    }
}
