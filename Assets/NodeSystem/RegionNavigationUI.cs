using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==========================================================================
// 2. 런타임 자동 생성 및 동적 상태 조절 네비게이션 UI 시스템
// ==========================================================================
public class RegionNavigationUI : MonoBehaviour
{
    [Header("네비게이터 대상 연결")]
    public RegionNavigator navigator;

    // 각 지역별 UI 버튼 매핑을 보관하는 캐싱 딕셔너리
    private Dictionary<Region, Button> regionButtons = new Dictionary<Region, Button>();
    private GameObject uiCanvasInstance;

    private void Start()
    {
        if (navigator == null)
        {
            navigator = FindObjectOfType<RegionNavigator>();
            if (navigator == null)
            {
                Debug.LogError("[RegionNavigationUI] 씬에 RegionNavigator가 배치되어 있지 않습니다!");
                return;
            }
        }

        // 런타임에 UI가 없을 경우를 대비해 캔버스와 하이얼라키 구조를 자동 생성합니다.
        CreateAutomaticUIHierarchy();

        // 씬 내의 모든 Region 노드들을 검색하여 해당하는 디폴트 버튼을 자동 마운트합니다.
        BuildRegionButtons();

        // 네비게이터의 움직임 피드백 이벤트를 받아와 실시간으로 UI 상태를 업데이트합니다.
        navigator.OnMovementStarted += UpdateUIStates;
        navigator.OnRegionReached += (region) => UpdateUIStates();

        // 초기 상태 적용
        UpdateUIStates();
    }

    /// <summary>
    /// 씬 내에 UI 캔버스 및 이벤트 시스템이 없을 때, 테스트가 정상 동작하도록 코드로 완벽하게 만들어줍니다.
    /// </summary>
    private void CreateAutomaticUIHierarchy()
    {
        // 1. 캔버스 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Dynamic_Navigation_Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        uiCanvasInstance = canvas.gameObject;

        // 2. 이벤트 시스템 확인
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 3. 버튼들을 이쁘게 배치해 줄 수직 정렬 컨테이너(Panel)를 좌측 상단에 배치합니다.
        GameObject panelGo = new GameObject("Navigation_Button_Panel");
        panelGo.transform.SetParent(uiCanvasInstance.transform, false);

        RectTransform rect = panelGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(25, -25);
        rect.sizeDelta = new Vector2(240, 500);

        // 배경색을 위해 투명한 검정 박스 이미지 부착
        Image bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.15f, 0.2f, 0.75f);

        // 세로 레이아웃 정렬 컴포넌트 추가
        VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;

        // 타이틀 헤더 텍스트 생성
        GameObject titleGo = new GameObject("Panel_Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.text = "Region Selector";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = Color.cyan;
    }

    /// <summary>
    /// 월드 상에 뿌려져 있는 모든 Region 컴포넌트들을 찾아서 디폴트 버튼으로 생성해 줍니다.
    /// </summary>
    private void BuildRegionButtons()
    {
        Region[] allRegions = FindObjectsOfType<Region>();
        Transform container = uiCanvasInstance.transform.Find("Navigation_Button_Panel");

        foreach (Region region in allRegions)
        {
            if (region == null) continue;

            // 유니티 디폴트 UI 디자인을 활용해 런타임에 날것의 버튼 오브젝트 생성
            DefaultControls.Resources uiResources = new DefaultControls.Resources();
            GameObject btnObj = DefaultControls.CreateButton(uiResources);
            btnObj.name = $"Btn_{region.name}";
            btnObj.transform.SetParent(container, false);

            // 버튼 컴포넌트 추출
            Button button = btnObj.GetComponent<Button>();
            Text textComp = btnObj.GetComponentInChildren<Text>();

            if (textComp != null)
            {
                textComp.text = region.name;
                textComp.fontSize = 14;
                textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            // 클릭했을 때 해당 Region을 타겟으로 Navigator가 출발하도록 연동
            button.onClick.AddListener(() => OnButtonClicked(region));

            // 딕셔너리에 매핑 캐싱 저장
            regionButtons.Add(region, button);
        }
    }

    /// <summary>
    /// 특정 Region 버튼이 눌렸을 때 발동하는 런타임 이동 트리거
    /// </summary>
    private void OnButtonClicked(Region targetRegion)
    {
        if (navigator == null || navigator.IsMoving) return;
        navigator.MoveTo(targetRegion);
    }

    /// <summary>
    /// 현재 Navigator의 위치와 상태(움직임 유무)에 반응하여, 이동 가능한 간선의 버튼들만 활성화/비활성화 해줍니다.
    /// </summary>
    private void UpdateUIStates()
    {
        if (navigator == null) return;

        bool isMovingNow = navigator.IsMoving;
        Region current = navigator.currentRegion;

        foreach (var pair in regionButtons)
        {
            Region region = pair.Key;
            Button button = pair.Value;

            // 1. 오브젝트가 현재 열심히 이동 중일 때는 사고 방지를 위해 모든 버튼 클릭 차단
            if (isMovingNow)
            {
                button.interactable = false;
                continue;
            }

            // 2. 현재 내 발 밑에 밟고 있는 자기 자신 노드 버튼도 차단
            if (region == current)
            {
                button.interactable = false;
                continue;
            }

            // 3. [핵심 조건] 현재 머물고 있는 노드의 connectedRegions 리스트에 들어가 있는 이웃한 노드들의 버튼만 활성화
            if (current != null && current.connectedRegions != null && current.connectedRegions.Contains(region))
            {
                button.interactable = true;
            }
            else
            {
                button.interactable = false;
            }
        }
    }
}