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
    /// Keeps the OpenXR loader in sync with the USE_OPENXR scripting define symbol:
    /// - USE_OPENXR defined and the loader missing: assigns the OpenXR loader.
    /// - USE_OPENXR not defined but the loader configured: this is almost certainly a
    ///   configuration mistake, so a warning is logged on domain reload and the build is
    ///   failed (BuildFailedException) rather than silently shipping a player with no XR
    ///   loader. The loader is never removed automatically, so a manual fix is not
    ///   silently reverted.
    /// </summary>
    public class XRLoaderAutoConfigurator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            CheckDefineSymbolsAndConfigureXR(report.summary.platformGroup, isBuild: true);
        }

        [InitializeOnLoadMethod]
        static void OnEditorLoad()
        {
            CheckDefineSymbolsAndConfigureXR(EditorUserBuildSettings.selectedBuildTargetGroup, isBuild: false);
        }

        static void CheckDefineSymbolsAndConfigureXR(BuildTargetGroup buildTargetGroup, bool isBuild)
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            var manager = settings != null ? settings.AssignedSettings : null;
            if (manager == null)
            {
                Debug.LogWarning($"[XRLoaderAutoConfigurator] XRManagerSettings not found for {buildTargetGroup}. Please ensure XR Plug-in Management is installed and configured at least once.");
                return;
            }

            const string openXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
            var existingOpenXR = manager.activeLoaders.FirstOrDefault(l => l != null && l.GetType().FullName == openXRLoaderType);

            bool changed = false;

#if USE_OPENXR
            // Enable OpenXR loader when USE_OPENXR is defined
            if (existingOpenXR == null)
            {
                XRPackageMetadataStore.AssignLoader(manager, openXRLoaderType, buildTargetGroup);
                changed = true;
            }
#else
            // USE_OPENXR is NOT defined but the OpenXR loader is configured: treat it as a
            // configuration mistake instead of silently removing the loader (which used to
            // revert manual fixes with no trace and ship a non-functional player).
            if (existingOpenXR != null)
            {
                const string message =
                    "The OpenXR loader is configured in XR Plug-in Management, but USE_OPENXR is not " +
                    "defined in Scripting Define Symbols. Add USE_OPENXR to Scripting Define Symbols if " +
                    "your project uses OpenXR, or remove the OpenXR loader from XR Plug-in Management for " +
                    "an intentional non-XR build.";

                if (isBuild)
                {
                    // Fail the build rather than ship a player with no XR loader (on device the
                    // app would hang on the loading screen forever with no crash or error).
                    throw new BuildFailedException("[XRLoaderAutoConfigurator] " + message);
                }

                Debug.LogWarning("[XRLoaderAutoConfigurator] " + message);
            }
#endif

            if (changed)
            {
                EditorUtility.SetDirty(manager);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
