using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==========================================================================
// 런타임 월드 공간 UI 제어용 컨트롤러 (Navigator의 이벤트 전적으로 위임 및 처리)
// ==========================================================================
[DisallowMultipleComponent]
public class RegionWorldUiController : MonoBehaviour
{
    [Header("타겟 내비게이터 연결")]
    [Tooltip("월드를 주행할 Navigator 오브젝트를 연결해 주세요.")]
    public RegionNavigator navigator;

    private void Start()
    {
        if (navigator == null)
        {
            navigator = FindObjectOfType<RegionNavigator>();
            if (navigator == null)
            {
                Debug.LogError("[RegionWorldUiController] 씬 내에 RegionNavigator가 존재하지 않습니다!");
                return;
            }
        }

        // 1. 내비게이터에 장착된 이벤트 델리게이트들을 동적으로 구독(Bind)합니다.
        navigator.OnMovementStarted += OnNavigatorStarted;
        navigator.OnRegionReached += OnNavigatorReached;

        // 2. 초기 상태 갱신 (시작 위치 기준으로 갈 수 있는 버튼만 켜기)
        if (navigator.currentRegion != null)
        {
            UpdateButtonStates(navigator.currentRegion);
        }
    }

    /// <summary>
    /// 무버가 주행을 시작할 때 호출되는 콜백. 사고 방지를 위해 모든 월드 UI 버튼 비활성화.
    /// </summary>
    private void OnNavigatorStarted()
    {
        DisableAllButtons();
    }

    /// <summary>
    /// 무버가 특정 노드에 안착했을 때 호출되는 콜백. 새로운 노드 기준으로 주변 버튼 상태 갱신.
    /// </summary>
    private void OnNavigatorReached(Region reachedRegion)
    {
        UpdateButtonStates(reachedRegion);
    }

    /// <summary>
    /// 씬 내의 모든 World UI 버튼들을 안전하게 전부 꺼버립니다.
    /// </summary>
    private void DisableAllButtons()
    {
        Region[] allRegions = FindObjectsOfType<Region>();
        foreach (Region r in allRegions)
        {
            if (r != null && r.worldSpaceButton != null)
            {
                r.worldSpaceButton.interactable = false;
                r.worldSpaceButton.onClick.RemoveAllListeners(); // 리스너도 초기화
            }
        }
    }

    /// <summary>
    /// 현재 상주하고 있는 노드를 기준으로 인접한(connectedRegions) 노드들의 월드 UI 버튼들만 클릭 가능하도록 세팅합니다.
    /// </summary>
    private void UpdateButtonStates(Region current)
    {
        if (current == null) return;

        Region[] allRegions = FindObjectsOfType<Region>();

        foreach (Region targetRegion in allRegions)
        {
            if (targetRegion == null || targetRegion.worldSpaceButton == null) continue;

            // 버튼의 이전 이벤트 리스너 제거
            targetRegion.worldSpaceButton.onClick.RemoveAllListeners();

            // 1. 현재 밟고 있는 자기 자신 노드의 버튼은 비활성화
            if (targetRegion == current)
            {
                targetRegion.worldSpaceButton.interactable = false;
                continue;
            }

            // 2. 현재 노드와 간선이 이어진 이웃 노드(connectedRegions)인지 판별
            if (current.connectedRegions != null && current.connectedRegions.Contains(targetRegion))
            {
                // 클릭 가능하도록 활성화
                targetRegion.worldSpaceButton.interactable = true;

                // 클릭하면 Navigator가 해당 타겟을 향해 주행하도록 바인딩
                targetRegion.worldSpaceButton.onClick.AddListener(() => {
                    navigator.MoveTo(targetRegion);
                });
            }
            else
            {
                // 연결되어 있지 않은 먼 곳은 전부 비활성화
                targetRegion.worldSpaceButton.interactable = false;
            }
        }
    }
}