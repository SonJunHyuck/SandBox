using UnityEditor;
using UnityEngine;

// ==========================================================================
// 3. 에디터 편의성을 향상시키는 커스텀 인스펙터 편집기 (에디터 전용)
// ==========================================================================
#if UNITY_EDITOR
[CustomEditor(typeof(RegionNavigator))]
public class RegionNavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RegionNavigator nav = (RegionNavigator)target;

        EditorGUILayout.Space(15);
        GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);

        if (GUILayout.Button("선택한 Current Region으로 즉시 순간이동", GUILayout.Height(35)))
        {
            if (nav.currentRegion != null)
            {
                // Ctrl+Z(실행취소) 히스토리에 기록을 남겨 에디팅 실수를 줄입니다.
                Undo.RecordObject(nav.transform, "Teleport Object to Region Node");
                nav.TeleportTo(nav.currentRegion);
                EditorUtility.SetDirty(nav.gameObject);
                
                Debug.Log($"[RegionNavigatorEditor] {nav.gameObject.name}이(가) {nav.currentRegion.name} 노드로 강제 스냅 이동되었습니다.");
            }
            else
            {
                EditorUtility.DisplayDialog("경고", "강제 이동할 Current Region 필드가 비어있습니다. 에디터에서 Region을 드래그해 넣어주세요!", "확인");
            }
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif