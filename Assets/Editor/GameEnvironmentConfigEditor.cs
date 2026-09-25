using UnityEditor;
using UnityEditor.AddressableAssets;

namespace DefaultNamespace.Editor
{
    [CustomEditor(typeof(GameEnvironmentConfig))]
    public class GameEnvironmentConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "addressablesProfileId");

            var profileSettings = AddressableAssetSettingsDefaultObject.Settings.profileSettings;
            var profileNames = profileSettings.GetAllProfileNames();
            SerializedProperty profileIdProperty = serializedObject.FindProperty("addressablesProfileId");
            int profileIndex = profileNames.IndexOf(profileSettings.GetProfileName(profileIdProperty.stringValue));

            EditorGUI.BeginChangeCheck();
            int selectedProfileIndex = EditorGUILayout.Popup("Addressables Profile", profileIndex, profileNames.ToArray());
            if (EditorGUI.EndChangeCheck())
                profileIdProperty.stringValue = profileSettings.GetProfileId(profileNames[selectedProfileIndex]);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
