using UnityEditor;
using UnityEngine;

namespace steph.Unity.Curve.Editor
{
    public class CurveDebugSettings
    {
        public Color PointColor;
        public float PointSize;
        public float ArrowSize;
        public Color ArrowColor;
    }

    public class CurveDebugWindow : EditorWindow
    {
        public static CurveDebugSettings Settings { get; private set; } = new CurveDebugSettings
        {
            PointColor = Color.white,
            ArrowColor = Color.blue,
            PointSize = .1f,
            ArrowSize = 1f,
        };

        [MenuItem("Stefan/Curve Settings")]
        public static void ShowExample()
        {
            CurveDebugWindow wnd = GetWindow<CurveDebugWindow>();
            wnd.titleContent = new GUIContent("Curve Settings");
        }

        private void OnGUI()
        {
            GUILayout.Label("Curve Appearance", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            Settings.PointColor = EditorGUILayout.ColorField("Point Color", Settings.PointColor);
            Settings.PointSize = EditorGUILayout.Slider("Point Size", Settings.PointSize, 0.001f, 10);

            Settings.ArrowColor = EditorGUILayout.ColorField("Arrow Color", Settings.ArrowColor);
            Settings.ArrowSize = EditorGUILayout.Slider("Arrow Size", Settings.ArrowSize, 0.001f, 10);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
                
            }
        }
    }
}
