using System;
using System.Reflection;
using UnityEditor;

namespace PhalanxChronicle.Editor
{
    [InitializeOnLoad]
    public static class TmpAnnotationSuppressor
    {
        private static readonly string[] TargetScriptClasses =
        {
            "TextMeshPro",
            "TextMeshProUGUI"
        };

        static TmpAnnotationSuppressor()
        {
            ScheduleApply();
            EditorApplication.projectChanged += ScheduleApply;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Phalanx Chronicle/Editor/Hide TMP Icons In Game View")]
        private static void ApplyFromMenu()
        {
            Apply();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode ||
                change == PlayModeStateChange.EnteredPlayMode)
            {
                ScheduleApply();
            }
        }

        private static void ScheduleApply()
        {
            EditorApplication.delayCall -= Apply;
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            Type annotationUtilityType = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");
            if (annotationUtilityType == null)
            {
                return;
            }

            MethodInfo getAnnotationsMethod = annotationUtilityType.GetMethod(
                "GetAnnotations",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo setIconEnabledMethod = annotationUtilityType.GetMethod(
                "SetIconEnabled",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (getAnnotationsMethod == null || setIconEnabledMethod == null)
            {
                return;
            }

            if (!(getAnnotationsMethod.Invoke(null, null) is Array annotations))
            {
                return;
            }

            foreach (object annotation in annotations)
            {
                if (annotation == null)
                {
                    continue;
                }

                string scriptClass = ReadMember<string>(annotation, "scriptClass");
                if (string.IsNullOrWhiteSpace(scriptClass) || !TargetsTextMeshPro(scriptClass))
                {
                    continue;
                }

                int classId = ReadMember<int>(annotation, "classID");
                try
                {
                    setIconEnabledMethod.Invoke(null, new object[] { classId, scriptClass, 0 });
                }
                catch
                {
                    // Unity keeps this API internal and changes it across versions.
                    // Failing silently is safer than blocking editor startup.
                }
            }
        }

        private static bool TargetsTextMeshPro(string scriptClass)
        {
            foreach (string candidate in TargetScriptClasses)
            {
                if (string.Equals(scriptClass, candidate, StringComparison.Ordinal) ||
                    scriptClass.IndexOf(candidate, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static T ReadMember<T>(object instance, string memberName)
        {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.GetValue(instance) is T fieldValue)
            {
                return fieldValue;
            }

            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.GetValue(instance, null) is T propertyValue)
            {
                return propertyValue;
            }

            return default;
        }
    }
}
