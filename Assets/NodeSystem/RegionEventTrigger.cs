using UnityEditor;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class SplineEventTrigger : MonoBehaviour
{
    [Header("물리 충돌 감지 반경 설정")]
    [Tooltip("이동형 본진이 이 반경을 밟으면 이벤트가 발생합니다.")]
    [Range(0.5f, 5.0f)]
    public float triggerRadius = 1.2f;

    [Header("돌발 상황 정보")]
    [Tooltip("수동 혹은 자동 롤링이 아닌, 이 트리거가 직접 유발할 시나리오 및 이벤트 요약을 입력합니다.")]
    public string triggerSummary = "돌발 기습 구역";

    private SphereCollider sphereCollider;

    private void Awake()
    {
        // 런타임에 안전하고 정밀하게 트리거가 세팅되도록 보장합니다.
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = triggerRadius;
    }

    private void OnValidate()
    {
        // 에디터에서 인스펙터 수치를 바꿨을 때 실시간으로 구체 콜라이더 반경 갱신
        if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
            sphereCollider.radius = triggerRadius;
        }
    }

    /// <summary>
    /// 주행 중인 이동 본진(Navigator)이 이 물리 영역을 관통하는 첫 프레임을 검출합니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        RegionNavigator navigator = other.GetComponent<RegionNavigator>();
        if (navigator != null)
        {
            // [클린 구조화 완료] 이제 이 지뢰(Trigger)는 Region과 아무 상관없이 단독으로 동작합니다.
            Debug.LogWarning($"<color=#EF4444><b>[🚨 위협 조우]</b></color> 본진 이동대형이 위험 지점 <b>[{name}]</b>에 강제 진입했습니다!" +
                $"\n - 위협 내용: {triggerSummary} / 이동 중지 및 대응 연출 단계 개시 필요");

            // [추후 확장 팁]: 이곳에 주행 중인 본진 대형을 정지(navigator.IsMoving 제어)시키거나, 
            // 몬스터 습격 씬을 열거나, 본진 내구도를 깎는 전투 엔진 코드를 바로 실행하면 됩니다.
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 에디터 씬 뷰에서 보이지 않는 물리 감지 범위를 빨간색 와이어 구체로 예쁘게 연출
        Gizmos.color = new Color(0.94f, 0.25f, 0.25f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // 정면 방향 지시 가이드 라인 그리기 (주행 차량과 정대하기 위함)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);

        GUIStyle labelStyle = new GUIStyle()
        {
            normal = { textColor = new Color(0.94f, 0.4f, 0.4f) },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 9
        };
        Handles.Label(transform.position + Vector3.down * 0.8f, $"[Trigger: {triggerSummary}]", labelStyle);
    }
#endif
}