using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Region))]
public class RegionEventTrigger : MonoBehaviour
{
    private Region cachedRegion;

    private void Start()
    {
        cachedRegion = GetComponent<Region>();

        // 1단계: 혹시 에디터에서 실수로 일반 Foothold 노드에 이 스크립트를 올린 경우 경고 후 자멸 처리
        if (cachedRegion.regionType != RegionType.Event)
        {
            Debug.LogError($"[RegionEventTrigger] {name} 노드는 일반 거점(Foothold)입니다! 이벤트 구역이 아닌 곳에 트리거 장착을 차단하고 컴포넌트를 즉시 파괴합니다.");
            Destroy(this);
            return;
        }

        // 2단계: 구체형 물리 트리거 콜라이더(IsTrigger = True)가 없다면 런타임에 안전하게 장착시킵니다.
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        sphereCollider.isTrigger = true;
        sphereCollider.radius = 1.2f; // 캐릭터가 관통할 때 닿을 수 있게 넉넉한 반지름 제공
    }

    /// <summary>
    /// 무버 캐릭터가 스플라인을 주행하다 이 보이지 않는 구역을 스쳐 지나갈 때 발동하는 충돌 콜백
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 3단계: 트리거 박스 안으로 기차/차량 무버가 지나가는지 컴포넌트 검출 체크
        RegionNavigator navigator = other.GetComponent<RegionNavigator>();
        if (navigator != null)
        {
            // 실시간 주행 감각 모니터링을 위한 풍부한 디버깅 출력
            Debug.LogWarning($"<color=#10B981><b>[🚨 이벤트 노드 진입 감지]</b></color> 차량 무버가 숨겨진 위협 구역 <b>[{name}]</b>의 중심부를 무사히 통과하고 있습니다!" +
                             $"\n - 현재 경로 상 목적지: {navigator.currentRegion?.name}");
            
            // [연동 힌트]: 이 부분에 이벤트 매니저나 시뮬레이션 HP 감소, 전투 연출 개시 등의 기획 로직을 자유롭게 연결하시면 됩니다!
        }
    }
}