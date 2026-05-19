using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

// ==========================================================================
// 1. 시뮬레이션 돌발 이벤트 유형 정의 (전투, 가상악화, 고장 등)
// ==========================================================================
public enum SimulationEventType
{
    None,
    Combat,             // 전투 발발
    SevereWeather,      // 기상 악화 (우천, 황사, 폭설)
    EngineBreakdown,    // 차량 고장 및 소모품 마모
    ResourceDiscovery,  // 자원 획득 및 긍정적 이벤트
    BanditAmbush        // 산적들의 기습 습격
}

[System.Serializable]
public class WeightedEvent
{
    [Tooltip("디스플레이용 이벤트 요약명")]
    public string eventName = "전투 돌발 상황";
    public SimulationEventType eventType = SimulationEventType.Combat;
    
    [Tooltip("선택될 가중치 (값이 높을수록 해당 타입 이벤트 중 무작위 뽑기 시 등장 빈도가 잦아짐)")]
    [Range(1f, 100f)]
    public float weight = 10f;
}

[System.Serializable]
public class SplineEventSegment
{
    [Tooltip("에디터 인스펙터 식별용 구간 이름 (예: A-B 구간 전투다발)")]
    public string segmentName = "구간 A-B";
    
    [Header("감지할 진척률 범위 (t)")]
    [Range(0f, 1f)] public float minT = 0.0f;
    [Range(0f, 1f)] public float maxT = 0.25f;

    [Header("이벤트 주사위 설정")]
    [Tooltip("해당 구간에 진입했을 때 이벤트가 발생할 총 종합 확률입니다 (0% ~ 100%)")]
    [Range(0f, 100f)] public float triggerProbability = 45f;

    [Tooltip("이 구간에서 주사위 굴려 발생에 성공했을 시, 뽑힐 수 있는 돌발 이벤트 목록 (가중치 랜덤)")]
    public List<WeightedEvent> possibleEvents = new List<WeightedEvent>();
}

[System.Serializable]
public class SplineEventConfig
{
    [Tooltip("Spline Container 내부의 스플라인 인덱스 번호")]
    public int splineIndex;
    public List<SplineEventSegment> segments = new List<SplineEventSegment>();
}

// ==========================================================================
// 2. 여러 루트 설정을 자산(.asset)으로 저장할 수 있게 하는 ScriptableObject
// ==========================================================================
[CreateAssetMenu(fileName = "NewSplineEventPreset", menuName = "Simulation/Spline Event Preset", order = 120)]
public class SplineEventPreset : ScriptableObject
{
    [Header("스플라인 인덱스별 구간 이벤트 프리셋 설정")]
    public List<SplineEventConfig> splineEventConfigs = new List<SplineEventConfig>();
}

[DisallowMultipleComponent]
public class SplineEventManager : MonoBehaviour
{
    [Header("타겟 무버 연결")]
    [Tooltip("월드 상에서 스플라인 레일을 타고 이동할 Navigator 오브젝트를 연결해 줍니다.")]
    public RegionNavigator navigator;

    [Header("이벤트 프리셋 자산 연결 (ScriptableObject)")]
    [Tooltip("에디터 프로젝트 뷰에서 우클릭으로 생성한 SplineEventPreset 파일을 드래그해 넣어주세요.")]
    public SplineEventPreset eventPreset;

    // 💡 중복 실행 방지 관리용 캐시셋 (한 번 주행 시 하나의 구간 이벤트는 한 번씩만 발동해야 함)
    // 키 형식: "{splineIndex}_{segmentIndex}"
    private HashSet<string> triggeredSegmentsThisRun = new HashSet<string>();

    private void Start()
    {
        if (navigator == null)
        {
            navigator = GetComponent<RegionNavigator>();
            if (navigator == null)
            {
                navigator = FindObjectOfType<RegionNavigator>();
                if (navigator == null)
                {
                    Debug.LogError("[SplineEventManager] 씬 내에 RegionNavigator가 존재하지 않습니다!");
                    return;
                }
            }
        }

        // Navigator의 이벤트를 구독하여 라이프사이클을 연계시킵니다.
        navigator.OnMovementStarted += OnJourneyStarted;
        navigator.OnSplinePositionChanged += OnPositionChanged;
    }

    private void OnDestroy()
    {
        if (navigator != null)
        {
            navigator.OnMovementStarted -= OnJourneyStarted;
            navigator.OnSplinePositionChanged -= OnPositionChanged;
        }
    }

    /// <summary>
    /// 새로운 구간 주행을 시작할 때, 중복 차단 플래그들을 리셋해줍니다.
    /// </summary>
    private void OnJourneyStarted()
    {
        triggeredSegmentsThisRun.Clear();
    }

    /// <summary>
    /// 캐릭터가 주행 중일 때 매 프레임 위치 체크 (t값 변동 추적 및 범위 충족 판정)
    /// </summary>
    private void OnPositionChanged(int splineIndex, float prevT, float currT)
    {
        // 프리셋이 비어있다면 검사 생략
        if (eventPreset == null || eventPreset.splineEventConfigs == null) return;

        // 1. 해당 스플라인을 위한 이벤트 설정서가 프리셋에 존재하는지 색인
        SplineEventConfig config = eventPreset.splineEventConfigs.Find(c => c.splineIndex == splineIndex);
        if (config == null || config.segments == null) return;

        // 2. 등록된 모든 구간(Segment)들과 충돌 여부 체크
        for (int i = 0; i < config.segments.Count; i++)
        {
            var segment = config.segments[i];
            string segmentKey = $"{splineIndex}_{i}";

            // 이번 주행에서 이미 실행 검사를 완수했다면 더 이상 돌발 주사위를 굴리지 않고 통과
            if (triggeredSegmentsThisRun.Contains(segmentKey)) continue;

            // 캐릭터가 이 구간 바운더리 내부(minT ~ maxT)로 진입했는지 감지
            if (currT >= segment.minT && currT <= segment.maxT)
            {
                // 즉시 중복 처리 완료 선언 후, 이벤트 주사위 롤링 진행
                triggeredSegmentsThisRun.Add(segmentKey);
                RollSegmentEvent(segment);
            }
        }
    }

    /// <summary>
    /// 구간 진입 성공 시, 고유의 트리거 확률과 개별 가중치를 활용하여 롤링을 처리합니다.
    /// </summary>
    private void RollSegmentEvent(SplineEventSegment segment)
    {
        // 1단계: 종합 이벤트 발생 유무 주사위 던지기
        float diceRoll = Random.Range(0f, 100f);

        Debug.Log($"<color=#38BDF8>[이벤트 엔진]</color> <b>[{segment.segmentName}]</b> 구간 진입 완료. (주사위 결과: {diceRoll:F1}% / 조건 성공 확률: {segment.triggerProbability}%)");

        if (diceRoll <= segment.triggerProbability)
        {
            // 2단계: 이벤트 발생 성공! 가중치 무작위 방식으로 풀에서 최적의 이벤트 하나 선별
            WeightedEvent selected = ChooseWeightedRandomEvent(segment.possibleEvents);

            if (selected != null)
            {
                TriggerEvent(segment.segmentName, selected);
            }
            else
            {
                Debug.LogWarning($"[이벤트 엔진] {segment.segmentName}에서 이벤트가 정상 트리거 되었으나, 풀 안에 들어있는 가능한 이벤트 항목이 비어있어 취소되었습니다.");
            }
        }
        else
        {
            Debug.Log($"<color=#94A3B8>[이벤트 엔진]</color> {segment.segmentName} 구간은 위협 없이 안전하게 통과했습니다.");
        }
    }

    /// <summary>
    /// 가중치 비중(Weight)을 골고루 합산하여 다이스 확률 기반으로 단 한 개의 이벤트를 뽑아냅니다.
    /// </summary>
    private WeightedEvent ChooseWeightedRandomEvent(List<WeightedEvent> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        // 전체 가중치 합산 구하기
        float totalWeight = 0f;
        foreach (var ev in pool)
        {
            totalWeight += ev.weight;
        }

        // 0부터 총 가중치 합 사이의 값을 랜덤으로 선정
        float targetVal = Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var ev in pool)
        {
            currentSum += ev.weight;
            if (targetVal <= currentSum)
            {
                return ev;
            }
        }

        return pool[0];
    }

    /// <summary>
    /// 뽑힌 돌발 이벤트를 타입 성격에 맞게 아름다운 RichText 폰트 컬러로 디버그 출력합니다.
    /// </summary>
    private void TriggerEvent(string segmentName, WeightedEvent ev)
    {
        string colorHex = "#FFFFFF";

        // 직관적인 디버그 모니터링을 위한 테마 컬러 부여
        switch (ev.eventType)
        {
            case SimulationEventType.Combat:
                colorHex = "#EF4444"; // 강렬한 레드
                break;
            case SimulationEventType.SevereWeather:
                colorHex = "#FB923C"; // 기후 주황색
                break;
            case SimulationEventType.EngineBreakdown:
                colorHex = "#A855F7"; // 기술 고장 연보라
                break;
            case SimulationEventType.ResourceDiscovery:
                colorHex = "#22C55E"; // 자원 보급 긍정 그린
                break;
            case SimulationEventType.BanditAmbush:
                colorHex = "#F43F5E"; // 산적 분홍색
                break;
        }

        Debug.LogWarning($"<color={colorHex}><b>[🚨 시뮬레이션 돌발 상황]</b></color> {segmentName}에서 <b>[{ev.eventName}]</b> 현상이 관측되었습니다! " +
            $"\n - 분류: <color={colorHex}>{ev.eventType}</color> / 가중치 선별 성공");
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터 상에서 하이어라키의 SplineEventManager를 마우스로 '클릭(선택)'했을 때, 
    /// 스플라인 위 해당 세그먼트 구간들을 고유 테마 색상으로 색칠하고 요약 가이드 텍스트를 공중에 띄워줍니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 유효성 예외 처리
        if (eventPreset == null || navigator == null || navigator.splineContainer == null) return;

        var container = navigator.splineContainer;

        foreach (var config in eventPreset.splineEventConfigs)
        {
            // 스플라인 컨테이너에 등록된 인덱스 범위를 벗어나는 예외 가드
            if (config.splineIndex < 0 || config.splineIndex >= container.Splines.Count) continue;

            var spline = container.Splines[config.splineIndex];
            if (spline == null) continue;

            foreach (var segment in config.segments)
            {
                // 1. 이벤트 리스트 내 대표 타입의 컬러 가져오기 (비어있으면 기본 노란색)
                Color zoneColor = Color.yellow;
                string primaryEventName = "평화 지대";
                if (segment.possibleEvents != null && segment.possibleEvents.Count > 0)
                {
                    zoneColor = GetEventGizmoColor(segment.possibleEvents[0].eventType);
                    primaryEventName = segment.possibleEvents[0].eventName;
                }

                // 2. 미세 간격으로 세그먼트 포인트들 샘플링하여 곡선 좌표 추출
                int segmentsResolution = 15; // 곡률 정확도 해상도
                Vector3[] drawPoints = new Vector3[segmentsResolution + 1];
                for (int i = 0; i <= segmentsResolution; i++)
                {
                    float factor = (float)i / segmentsResolution;
                    float sampleT = Mathf.Lerp(segment.minT, segment.maxT, factor);
                    
                    // 로컬 스플라인 좌표 계산 후 월드 좌표 변환
                    Vector3 localPos = spline.EvaluatePosition(sampleT);
                    drawPoints[i] = container.transform.TransformPoint(localPos);
                }

                // 3. 씬 뷰에 스플라인을 덮어 그릴 부드러운 두꺼운 안티앨리어싱 패스 그리기
                Handles.color = zoneColor;
                Handles.DrawAAPolyLine(7f, drawPoints); // 굵기 7f의 두터운 실선 그리기

                // 4. 구간의 양 끝 바운더리에 직관적인 캡슐 마커형 미니 구체(Sphere) 그리기
                Gizmos.color = zoneColor;
                Gizmos.DrawSphere(drawPoints[0], 0.25f);
                Gizmos.DrawSphere(drawPoints[drawPoints.Length - 1], 0.25f);

                // 5. 구간 정중앙 좌표 공중에 텍스트 빌보드 띄우기
                float centerT = (segment.minT + segment.maxT) * 0.5f;
                Vector3 centerLocalPos = spline.EvaluatePosition(centerT);
                Vector3 centerWorldPos = container.transform.TransformPoint(centerLocalPos);

                // 가독성 높은 GUI 스타일 세팅
                GUIStyle boldLabelStyle = new GUIStyle();
                boldLabelStyle.normal.textColor = zoneColor;
                boldLabelStyle.fontSize = 11;
                boldLabelStyle.fontStyle = FontStyle.Bold;
                boldLabelStyle.alignment = TextAnchor.MiddleCenter;

                // 디테일한 구간 요약 문자열 조립
                string labelContent = $"★ {segment.segmentName}\n({segment.minT:F2} ~ {segment.maxT:F2})\n[확률: {segment.triggerProbability}%, 대표: {primaryEventName}]";
                
                // 중심점 살짝 위(Y + 1.2m)에 빌보드 노출
                Handles.Label(centerWorldPos + Vector3.up * 1.2f, labelContent, boldLabelStyle);
            }
        }
    }

    /// <summary>
    /// 디버그 로그와 매칭되는 씬 뷰 시각화 전용 컬러 테이블
    /// </summary>
    private Color GetEventGizmoColor(SimulationEventType type)
    {
        switch (type)
        {
            case SimulationEventType.Combat:
                return new Color(1f, 0.25f, 0.25f, 0.9f);      // 강렬한 네온 레드
            case SimulationEventType.SevereWeather:
                return new Color(1f, 0.6f, 0.15f, 0.9f);       // 경고 오렌지
            case SimulationEventType.EngineBreakdown:
                return new Color(0.68f, 0.35f, 1f, 0.9f);      // 마법 연보라
            case SimulationEventType.ResourceDiscovery:
                return new Color(0.15f, 0.9f, 0.4f, 0.9f);      // 회복 에메랄드 그린
            case SimulationEventType.BanditAmbush:
                return new Color(1f, 0.2f, 0.55f, 0.9f);       // 위험 마젠타 핑크
            default:
                return new Color(0.9f, 0.9f, 0.2f, 0.8f);       // 대기 옐로우
        }
    }
#endif
}