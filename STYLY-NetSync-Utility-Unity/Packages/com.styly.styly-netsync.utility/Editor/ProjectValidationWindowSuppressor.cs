using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Styly.NetSync.Utility
{
    /// <summary>
    /// Comments out processes that are incompatible with Multiplayer Playmode in XR Hands and XR Interaction Toolkit Editor Scripts added by STYLY XR Rig.
    /// </summary>
    public class ProjectValidationWindowSuppressor
    {
        private static string[] GetTargetFiles()
        {
            var targetFiles = new List<string>();
            
            // Dynamically search for XR Interaction Toolkit version
            string[] handsSampleFiles = Directory.GetFiles("Assets/Samples", "HandsSampleProjectValidation.cs", SearchOption.AllDirectories);
            string[] starterAssetsFiles = Directory.GetFiles("Assets/Samples", "StarterAssetsSampleProjectValidation.cs", SearchOption.AllDirectories);
            
            targetFiles.AddRange(handsSampleFiles);
            targetFiles.AddRange(starterAssetsFiles);
            
            return targetFiles.ToArray();
        }

        [MenuItem("Tools/Suppress Project Validation Window")]
        public static void SuppressProjectValidationWindow()
        {
            int fixedCount = 0;
            
            foreach (string filePath in GetTargetFiles())
            {
                if (SuppressInFile(filePath))
                {
                    fixedCount++;
                }
            }
            
            if (fixedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"Project validation window suppressed in {fixedCount} file(s)");
            }
            else
            {
                Debug.Log("No files needed suppressing or files not found");
            }
        }

        private static bool SuppressInFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File not found: {filePath}");
                return false;
            }

            string[] lines = File.ReadAllLines(filePath);
            bool modified = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Skip if already commented out
                if (line.Contains("// SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath)"))
                {
                    continue;
                }
                
                // Find SettingsService.OpenProjectSettings line and comment it out
                if (line.Contains("SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath)"))
                {
                    lines[i] = line.Replace("SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath);", 
                                          "// SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath);");
                    modified = true;
                }
            }

            if (modified)
            {
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Suppressed: {filePath}");
                return true;
            }
            else
            {
                Debug.Log($"Already suppressed or no target found: {filePath}");
                return false;
            }
        }

        [MenuItem("Tools/Restore Project Validation Window")]
        public static void RestoreProjectValidationWindow()
        {
            int revertedCount = 0;
            
            foreach (string filePath in GetTargetFiles())
            {
                if (RestoreInFile(filePath))
                {
                    revertedCount++;
                }
            }
            
            if (revertedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"Project validation window restored in {revertedCount} file(s)");
            }
            else
            {
                Debug.Log("No files needed restoring or files not found");
            }
        }

        private static bool RestoreInFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File not found: {filePath}");
                return false;
            }

            string[] lines = File.ReadAllLines(filePath);
            bool modified = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Skip if already restored
                if (line.Contains("SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath)") && 
                    !line.Contains("//"))
                {
                    continue;
                }
                
                // Find commented out line and restore it
                if (line.Contains("// SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath)"))
                {
                    lines[i] = line.Replace("// SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath);", 
                                          "SettingsService.OpenProjectSettings(k_ProjectValidationSettingsPath);");
                    modified = true;
                }
            }

            if (modified)
            {
                File.WriteAllLines(filePath, lines);
                Debug.Log($"Restored: {filePath}");
                return true;
            }
            else
            {
                Debug.Log($"Already restored or no target found: {filePath}");
                return false;
            }
        }
    }
}
