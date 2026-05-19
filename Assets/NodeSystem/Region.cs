using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ==========================================================================
// 1. 월드에 배치될 Region 노드 컴포넌트
// ==========================================================================
[SelectionBase]
[DisallowMultipleComponent]
public class Region : MonoBehaviour
{
    [Header("연결할 이웃 Region 노드들 (단방향/양방향 가능)")]
    public List<Region> connectedRegions = new List<Region>();
    public UnityEngine.UI.Button worldSpaceButton; // 이 노드가 제어할 머리 위 버튼

    [Header("노드 시각화 설정")]
    [SerializeField] private Color nodeColor = Color.cyan;
    [SerializeField] private Color connectionColor = Color.green;

#if UNITY_EDITOR
    // 씬 뷰에서 직관적으로 노드 연결 상태를 볼 수 있도록 Gizmo를 그려줍니다.
    private void OnDrawGizmos()
    {
        // 1. 자기 자신 노드 그리기
        Gizmos.color = nodeColor;
        Gizmos.DrawSphere(transform.position, 0.75f);
        
        // 씬 뷰 텍스트 표시
        Handles.Label(transform.position + Vector3.up * 1.2f, name, new GUIStyle() {
            normal = { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        });

        if (connectedRegions == null) return;

        // 2. 연결된 간선(화살표) 그리기
        foreach (var target in connectedRegions)
        {
            if (target != null)
            {
                Gizmos.color = connectionColor;
                DrawGizmoArrow(transform.position, target.transform.position);
            }
        }
    }

    // 화살표 머리까지 그려주는 보조 메서드
    private void DrawGizmoArrow(Vector3 from, Vector3 to)
    {
        Gizmos.DrawLine(from, to);
        
        Vector3 dir = (to - from).normalized;
        if (dir == Vector3.zero) return;

        float arrowLength = 1.2f;
        float arrowAngle = 25f;

        // 화살표 촉의 양 날개 벡터 계산
        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + arrowAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - arrowAngle, 0) * Vector3.forward;

        // 도착점으로부터 약간 뒤에 화살표 머리 배치 (구체 구멍 방지)
        Vector3 arrowTip = to - dir * 0.5f;
        Gizmos.DrawRay(arrowTip, right * arrowLength);
        Gizmos.DrawRay(arrowTip, left * arrowLength);
    }
#endif
}