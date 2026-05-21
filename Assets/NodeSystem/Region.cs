using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RegionType
{
    Foothold, // 일반 거점 (정차 및 월드 UI 버튼 표시 노드)
    Event     // 이벤트 전용 노드 (보이지 않고 스쳐 지나가는 물리 트리거 노드)
}

[SelectionBase]
[DisallowMultipleComponent]
public class Region : MonoBehaviour
{
    [Header("노드 성격 유형 설정")]
    [Tooltip("Foothold는 UI가 뜨는 활성화 거점이며, Event는 보이지 않는 런타임 트리거 구역입니다.")]
    public RegionType regionType = RegionType.Foothold;

    [Header("연결할 이웃 Region 노드들")]
    [Tooltip("인접한 다음 노드들을 드래그해 줍니다 (거점과 이벤트 노드를 순서대로 체이닝 가능)")]
    public List<Region> connectedRegions = new List<Region>();

    [Header("월드 UI 참조")]
    [Tooltip("UI Generator 툴을 통해 생성된 머리 위 월드 캔버스 버튼입니다.")]
    public UnityEngine.UI.Button worldSpaceButton;

    [Header("노드 시각화 설정 (에디터 뷰 전용)")]
    [SerializeField] private Color footholdColor = Color.cyan;
    [SerializeField] private Color eventColor = new Color(0.15f, 0.9f, 0.4f); // 에메랄드 그린
    [SerializeField] private Color connectionColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 1. 노드 성격에 따라 다르게 그려지는 입체 씬 뷰 기즈모
        Gizmos.color = (regionType == RegionType.Foothold) ? footholdColor : eventColor;
        
        if (regionType == RegionType.Foothold)
        {
            Gizmos.DrawSphere(transform.position, 0.75f);
        }
        else
        {
            // 이벤트 노드는 스쳐 지나가는 구역이므로 빈 와이어프레임 구체로 씬 뷰에 표시
            Gizmos.DrawWireSphere(transform.position, 1.0f);
        }
        
        // 2. 머리 위 텍스트 명패 달아주기
        GUIStyle textStyle = new GUIStyle()
        {
            normal = { textColor = (regionType == RegionType.Foothold) ? Color.cyan : new Color(0.4f, 1f, 0.6f) },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 10
        };
        string labelText = $"{name}\n[{(regionType == RegionType.Foothold ? "Foothold" : "Trigger")}]";
        Handles.Label(transform.position + Vector3.up * 1.3f, labelText, textStyle);

        if (connectedRegions == null) return;

        // 3. 인접 노드 궤적 선 연결
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