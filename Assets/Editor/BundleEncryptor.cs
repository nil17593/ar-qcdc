using UnityEditor;
using UnityEngine;
using System.IO;

namespace QCDC.EditorTools
{
    /// <summary>
    /// Builds the 3D model into an Asset Bundle and applies a lightweight XOR cipher.
    /// Includes separate build options to fix the Editor vs Android shader mismatch (Pink Materials).
    /// </summary>
    public class BundleEncryptor
    {
        private static byte key = 123;

        // Button 1: Use this when you want to hit the "Play" button in Unity to test
        [MenuItem("QCDC Tools/1. Build Encrypted Bundle (For Editor Testing)")]
        public static void BuildForEditor()
        {
#if UNITY_EDITOR_WIN
            BuildAndEncrypt(BuildTarget.StandaloneWindows64);
#elif UNITY_EDITOR_OSX
            BuildAndEncrypt(BuildTarget.StandaloneOSX);
#else
            BuildAndEncrypt(BuildTarget.StandaloneWindows64);
#endif
        }

        // Button 2: Use this right before you build your final .apk file!
        [MenuItem("QCDC Tools/2. Build Encrypted Bundle (For Android APK)")]
        public static void BuildForAndroid()
        {
            BuildAndEncrypt(BuildTarget.Android);
        }

        private static void BuildAndEncrypt(BuildTarget targetPlatform)
        {
            string buildPath = "Assets/AssetBundles";
            if (!Directory.Exists(buildPath)) Directory.CreateDirectory(buildPath);

            string streamingPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingPath)) Directory.CreateDirectory(streamingPath);

            // Builds the bundle for the specific platform requested by the menu button
            BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None, targetPlatform);

            string rawBundlePath = Path.Combine(buildPath, "qcdc_model_bundle");
            if (!File.Exists(rawBundlePath))
            {
                Debug.LogError("Bundle not found. Did you assign the AssetBundle tag to your prefab?");
                return;
            }

            byte[] data = File.ReadAllBytes(rawBundlePath);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key;
            }

            string encryptedPath = Path.Combine(streamingPath, "qcdc_encrypted.bundle");
            File.WriteAllBytes(encryptedPath, data);

            File.Delete(rawBundlePath);
            File.Delete(rawBundlePath + ".manifest");

            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Bundle Built and Encrypted for {targetPlatform}!</color>");
        }
    }
}