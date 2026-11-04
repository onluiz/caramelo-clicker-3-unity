using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Fills the Android keystore passwords from environment variables so they never touch the repo:
// CARAMELO_KEYSTORE_PASS (the .jks file) and CARAMELO_KEYSTORE_ALIAS_PASS (the key alias).
// Without the variables nothing changes and Unity asks for the passwords in Publishing Settings as usual.
[InitializeOnLoad]
public class AndroidSigning : IPreprocessBuildWithReport
{
    const string KeystoreVar = "CARAMELO_KEYSTORE_PASS";
    const string KeyVar = "CARAMELO_KEYSTORE_ALIAS_PASS";

    public int callbackOrder => 0;

    static AndroidSigning() => Apply();

    public void OnPreprocessBuild(BuildReport report) => Apply();

    static void Apply()
    {
        var keystorePass = System.Environment.GetEnvironmentVariable(KeystoreVar);
        var keyPass = System.Environment.GetEnvironmentVariable(KeyVar);
        if (string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(keyPass)) return;
        if (!PlayerSettings.Android.useCustomKeystore) return;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasPass = keyPass;
    }
}
