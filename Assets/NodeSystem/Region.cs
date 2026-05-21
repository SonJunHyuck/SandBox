using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[SelectionBase]
[DisallowMultipleComponent]
public class Region : MonoBehaviour
{
    [Header("연결할 이웃 거점(Region) 노드들")]
    [Tooltip("이동형 본진이 이 거점에서 직접 주행해서 도달할 수 있는 이웃 거점들을 드래그해 줍니다.")]
    public List<Region> connectedRegions = new List<Region>();

    [Header("월드 UI 참조")]
    [Tooltip("Region UI Generator 툴을 통해 생성된 머리 위 월드 캔버스 버튼입니다.")]
    public UnityEngine.UI.Button worldSpaceButton;

    [Header("노드 시각화 설정 (에디터 뷰 전용)")]
    [SerializeField] private Color nodeColor = Color.cyan;
    [SerializeField] private Color connectionColor = new Color(0.2f, 0.8f, 1f, 0.4f);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 1. 거점을 표현하는 하늘색 입체 기즈모 그리기
        Gizmos.color = nodeColor;
        Gizmos.DrawSphere(transform.position, 0.8f);
        
        // 2. 머리 위 거점 명패 UI 텍스트 달아주기
        GUIStyle textStyle = new GUIStyle()
        {
            normal = { textColor = Color.cyan },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 11
        };
        Handles.Label(transform.position + Vector3.up * 1.3f, $"★ {name}\n[Foothold]", textStyle);

        if (connectedRegions == null) return;

        // 3. 인접 거점 연결선 단순화 (양방향 연결선 표시)
        Gizmos.color = connectionColor;
        foreach (var target in connectedRegions)
        {
            if (target != null)
            {
                Gizmos.DrawLine(transform.position, target.transform.position);
            }
        }
    }
#endif
}