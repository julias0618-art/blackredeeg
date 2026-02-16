#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// TextMesh Pro Essential Resources가 없을 때 자동으로 임포트합니다.
/// "Window > Text Mesh Pro > Import TMP Essential Resources" 수동 실행과 동일합니다.
/// </summary>
[InitializeOnLoad]
public static class TMPEssentialResourcesAutoImport
{
    static TMPEssentialResourcesAutoImport()
    {
        EditorApplication.delayCall += TryImportTMPEssentialResources;
    }

    static void TryImportTMPEssentialResources()
    {
        string settingsPath = Path.Combine(Application.dataPath, "TextMesh Pro", "Resources", "TMP Settings.asset");
        if (File.Exists(settingsPath))
            return;

        TMPro.TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
    }
}
#endif
