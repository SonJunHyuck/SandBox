using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
    
// ==========================================================================
// 1. 실시간 그래프 내비게이터 (이동 컴포넌트)
// ==========================================================================
[DisallowMultipleComponent]
public class RegionNavigator : MonoBehaviour
{
    [Header("스플라인 데이터 소스")]
    [Tooltip("RegionSplineGenerator로 생성한 Spline Container를 연결해주세요.")]
    public SplineContainer splineContainer;

    [Header("현재 위치한 Region 노드")]
    [Tooltip("현재 위치 혹은 시작 위치를 지정합니다.")]
    public Region currentRegion;

    [Header("이동 설정")]
    [Tooltip("이동 거리에 관계없이 목표 노드까지 도달하는 총 시간(초)입니다.")]
    [Range(0.1f, 10f)]
    public float travelDuration = 2.0f;

    // 상태 확인용 프로퍼티
    public bool IsMoving { get; private set; } = false;

    // UI 및 기타 시스템과의 연동을 위한 이벤트 델리게이트
    public System.Action OnMovementStarted;
    public System.Action<Region> OnRegionReached;

    private Coroutine movementCoroutine;

    private void Start()
    {
        // 시작 시 현재 지정된 노드로 강제 순간이동 시켜 정렬합니다.
        if (currentRegion != null)
        {
            TeleportTo(currentRegion);
        }
        else
        {
            // 지정되어 있지 않다면 씬 내의 아무 노드나 하나 잡아서 초기화해 줍니다.
            Region firstRegion = FindObjectOfType<Region>();
            if (firstRegion != null)
            {
                TeleportTo(firstRegion);
            }
        }
    }

    /// <summary>
    /// 에디터 혹은 런타임에서 지정한 Region 노드로 대상을 즉시 순간이동 시키고 정렬합니다.
    /// </summary>
    public void TeleportTo(Region target)
    {
        if (target == null) return;

        currentRegion = target;
        transform.position = target.transform.position;

        // 시각적으로 어색하지 않게 첫 번째 연결된 이웃 노드가 있다면 그쪽을 바라보게 처리합니다.
        if (target.connectedRegions != null && target.connectedRegions.Count > 0 && target.connectedRegions[0] != null)
        {
            Vector3 lookDir = (target.connectedRegions[0].transform.position - target.transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    /// <summary>
    /// 대상 Region으로 정해진 시간 동안 스플라인 경로를 타고 주행을 시작합니다.
    /// </summary>
    public void MoveTo(Region targetRegion)
    {
        if (IsMoving) return;
        if (targetRegion == null || currentRegion == null) return;

        // Spline Container 내부에서 [현재 노드 -> 목표 노드]를 잇는 스플라인 트랙과 정/역방향 여부를 검색합니다.
        if (FindSplineConnection(currentRegion, targetRegion, out int splineIndex, out bool isReversed))
        {
            if (movementCoroutine != null) StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(Co_FollowSpline(targetRegion, splineIndex, isReversed));
        }
        else
        {
            Debug.LogWarning($"[RegionNavigator] {currentRegion.name}에서 {targetRegion.name}으로 가는 직통 스플라인 경로를 찾지 못했습니다! 직접 순간이동 처리합니다.");
            TeleportTo(targetRegion);
        }
    }

    /// <summary>
    /// 거리에 상관없이 travelDuration 시간 동안 지정한 Spline 트랙 위를 주행하는 핵심 코루틴
    /// </summary>
    private IEnumerator Co_FollowSpline(Region targetRegion, int splineIndex, bool isReversed)
    {
        IsMoving = true;
        OnMovementStarted?.Invoke();

        float elapsed = 0f;
        var spline = splineContainer.Splines[splineIndex];

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / travelDuration);

            // 정방향 주행이면 0 -> 1로 진행, 역방향 주행이면 1 -> 0으로 진행
            float t = isReversed ? (1f - progress) : progress;

            // 로컬 좌표를 월드 좌표로 복원
            Vector3 worldPos = splineContainer.transform.TransformPoint(spline.EvaluatePosition(t));
            Vector3 tangent = spline.EvaluateTangent(t);

            transform.position = worldPos;

            // 회전 적용 (역주행인 경우 탄젠트 방향도 반대로 꺾어줍니다)
            if (tangent != Vector3.zero)
            {
                Vector3 moveDirection = isReversed ? -tangent : tangent;
                if (moveDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(moveDirection);
                }
            }

            yield return null;
        }

        // 목적지 노드의 물리적인 위치에 정확하게 안착시킵니다.
        transform.position = targetRegion.transform.position;
        currentRegion = targetRegion;
        IsMoving = false;

        OnRegionReached?.Invoke(currentRegion);
    }

    /// <summary>
    /// Spline Container 내의 스플라인들 중, 두 Region 노드의 좌표 경계와 일치하는 트랙을 찾아냅니다. (정/역주행 자동 감지)
    /// </summary>
    private bool FindSplineConnection(Region from, Region to, out int splineIndex, out bool isReversed)
    {
        splineIndex = -1;
        isReversed = false;

        if (splineContainer == null) return false;

        float threshold = 1.5f; // 노드 구체 스냅 오차 범위 허용값

        for (int i = 0; i < splineContainer.Splines.Count; i++)
        {
            var spline = splineContainer.Splines[i];
            if (spline.Count < 2) continue;

            // 스플라인의 첫 노드와 끝 노드의 월드 좌표 계산
            Vector3 splineStart = splineContainer.transform.TransformPoint(spline[0].Position);
            Vector3 splineEnd = splineContainer.transform.TransformPoint(spline[spline.Count - 1].Position);

            // Case A: 정방향 트랙 발견 (from -> to)
            if (Vector3.Distance(from.transform.position, splineStart) < threshold &&
                Vector3.Distance(to.transform.position, splineEnd) < threshold)
            {
                splineIndex = i;
                isReversed = false;
                return true;
            }

            // Case B: 역방향 트랙 발견 (to -> from 을 잇는 1개짜리 양방향 대응 스플라인인 경우)
            if (Vector3.Distance(from.transform.position, splineEnd) < threshold &&
                Vector3.Distance(to.transform.position, splineStart) < threshold)
            {
                splineIndex = i;
                isReversed = true;
                return true;
            }
        }

        return false;
    }
}