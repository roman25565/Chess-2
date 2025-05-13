#if UNITY_EDITOR
using UnityEditor;

public class ServerBuildPreProcessor
{
    [InitializeOnLoadMethod]
    public static void OnProjectLoadedInEditor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            #if UNITY_SERVER
            RemoveFirebasePlugins();
            #endif
        }
    }

    [MenuItem("Build/Server Build")]
    public static void PrepareServerBuild()
    {
        RemoveFirebasePlugins();
        // Інші налаштування збірки...
    }

    private static void RemoveFirebasePlugins()
    {
        // Список плагінів Firebase для видалення
        string[] firebasePlugins = new string[]
        {
            "FirebaseCppApp-12_8_0",
            "FirebaseCppAuth",
            "FirebaseCppDatabase",
            "FirebaseCppAnalytics",
            "FirebaseCppFirestore",
            "FirebaseCppRemoteConfig",
        };

        // Вимкнення плагінів для Linux платформи
        PluginImporter[] importers = PluginImporter.GetAllImporters();
        foreach (PluginImporter importer in importers)
        {
            if (importer == null) continue;

            foreach (string pluginName in firebasePlugins)
            {
                if (importer.assetPath.Contains(pluginName))
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
                    importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux, false);
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif