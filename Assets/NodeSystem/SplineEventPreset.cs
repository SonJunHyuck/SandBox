using System.Collections.Generic;
using UnityEngine;

// ==========================================================================
// 1. 시뮬레이션 돌발 이벤트 유형 정의
// ==========================================================================
public enum SimulationEventType
{
    None,
    Combat,             // 전투 발발
    SevereWeather,      // 기상 악화
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

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 값 변경 시 작동하는 데이터 무결성 검증 함수
    /// </summary>
    private void OnValidate()
    {
        if (splineEventConfigs == null) return;

        foreach (var config in splineEventConfigs)
        {
            if (config.segments == null || config.segments.Count < 2) continue;

            // 동일한 스플라인 번호 안의 세그먼트들 간에 범위가 겹치는지 체크
            for (int i = 0; i < config.segments.Count; i++)
            {
                var segA = config.segments[i];

                // 예외 조절 피드백 (최소값이 최대값보다 크게 기입된 경우 자동 스냅 정렬)
                if (segA.minT > segA.maxT)
                {
                    segA.minT = segA.maxT;
                }

                for (int j = i + 1; j < config.segments.Count; j++)
                {
                    var segB = config.segments[j];

                    // 두 세그먼트의 minT~maxT 범위가 단 1%라도 겹치는 상황인지 수학적으로 판별
                    bool isOverlapping = (segA.minT < segB.maxT) && (segB.minT < segA.maxT);

                    if (isOverlapping)
                    {
                        Debug.LogWarning(
                            $"<color=#F43F5E><b>[데이터 오류 경고]</b></color> '{name}' 프리셋 파일의 " +
                            $"<b>[Spline {config.splineIndex}]</b> 노선 내에서 <b>'{segA.segmentName}'</b>과 <b>'{segB.segmentName}'</b>의 범위가 중첩되고 있습니다!\n" +
                            $"동시에 두 개 이상의 돌발 상황이 중복 실행되는 오작동을 막기 위해 범위를 겹치지 않게 수정해 주세요."
                        );
                    }
                }
            }
        }
    }
}
#endif