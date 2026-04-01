using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using System.Linq;
using UnityEditor.XR.Management.Metadata;

namespace Styly.NetSync.Utility
{
    /// <summary>
    /// Enables OpenXR only when USE_OPENXR is present in Scripting Define Symbols.
    /// </summary>
    public class XRLoaderAutoConfigurator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            CheckDefineSymbolsAndConfigureXR(report.summary.platformGroup);
        }
        
        [InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            CheckDefineSymbolsAndConfigureXR(EditorUserBuildSettings.selectedBuildTargetGroup);
        }
        
        static void CheckDefineSymbolsAndConfigureXR(BuildTargetGroup buildTargetGroup)
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            var manager = settings != null ? settings.AssignedSettings : null;
            if (manager == null)
            {
                Debug.LogWarning($"[BuildScript] XRManagerSettings not found for {buildTargetGroup}. Please ensure XR Plug-in Management is installed and configured at least once.");
                return;
            }

            const string openXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
            var existingOpenXR = manager.activeLoaders.FirstOrDefault(l => l != null && l.GetType().FullName == openXRLoaderType);

#if USE_OPENXR
            // Enable OpenXR loader when USE_OPENXR is defined
            if (existingOpenXR == null)
            {
                XRPackageMetadataStore.AssignLoader(manager, openXRLoaderType, buildTargetGroup);
            }
#else
            // Disable OpenXR loader when USE_OPENXR is NOT defined
            if (existingOpenXR != null)
            {
                XRPackageMetadataStore.RemoveLoader(manager, openXRLoaderType, buildTargetGroup);
            }
#endif

            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }
    }
}
