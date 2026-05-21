using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

public class SplineEventPopulatorWindow : EditorWindow
{
    private SplineContainer targetContainer;
    private int targetSplineIndex = 0;
    private GameObject eventPrefab;
    
    [Header("배치 설정")]
    private float intervalDistance = 10f; // X미터 간격 배치
    private float startOffset = 3f;       // 출발 거점 근처 스폰 방지 마진
    private float endOffset = 3f;         // 종착 거점 근처 스폰 방지 마진
    
    [MenuItem("Tools/Spline Event Populator")]
    public static void ShowWindow()
    {
        var window = GetWindow<SplineEventPopulatorWindow>("Event Populator");
        window.minSize = new Vector2(380, 280);
    }

    private void OnGUI()
    {
        GUILayout.Label("Spline Event Node Populator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("선택한 스플라인 경로를 추적하여, 일정 거리(X미터)마다 이벤트 트리거 프리팹을 정밀하게 자동 배치합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        // 1. 타겟 스플라인 컨테이너 슬롯
        targetContainer = (SplineContainer)EditorGUILayout.ObjectField(
            "Target Spline Container", 
            targetContainer, 
            typeof(SplineContainer), 
            true
        );

        if (targetContainer != null)
        {
            // 스플라인 개수에 따라 인덱스를 고를 수 있는 슬라이더 동적 노출
            int maxIndex = targetContainer.Splines.Count - 1;
            if (maxIndex < 0)
            {
                EditorGUILayout.HelpBox("선택된 컨테이너에 배치 가능한 스플라인 궤적이 존재하지 않습니다.", MessageType.Warning);
            }
            else
            {
                targetSplineIndex = EditorGUILayout.IntSlider("Target Spline Index", targetSplineIndex, 0, maxIndex);
            }
        }

        // 2. 스폰할 이벤트 프리팹
        eventPrefab = (GameObject)EditorGUILayout.ObjectField("Event Trigger Prefab", eventPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10);
        GUILayout.Label("배치 세부 간격 설정 (미터 단위)", EditorStyles.boldLabel);
        intervalDistance = EditorGUILayout.FloatField("배치 간격 (Interval)", intervalDistance);
        startOffset = EditorGUILayout.FloatField("시작점 마진 (Start Margin)", startOffset);
        endOffset = EditorGUILayout.FloatField("끝점 마진 (End Margin)", endOffset);

        // 데이터 무결성 보장 예외 처리
        intervalDistance = Mathf.Max(0.5f, intervalDistance);
        startOffset = Mathf.Max(0f, startOffset);
        endOffset = Mathf.Max(0f, endOffset);

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("이벤트 트리거 생성 실행", GUILayout.Height(42)))
        {
            PopulateEventsOnSpline();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("일괄 생성 실행", GUILayout.Height(42)))
        {
            for (int i = 0; i <= targetContainer.Splines.Count; i++)
            {
                targetSplineIndex = i;
                PopulateEventsOnSpline();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    /// <summary>
    /// 수학적 거리를 비율(t)로 변환하고 월드 좌표 및 레일 진행 방향 회전까지 자동 연산하여 배치합니다.
    /// </summary>
    private void PopulateEventsOnSpline()
    {
        if (targetContainer == null)
        {
            EditorUtility.DisplayDialog("오류", "대상 Spline Container를 먼저 지정해 주세요!", "확인");
            return;
        }

        if (eventPrefab == null)
        {
            EditorUtility.DisplayDialog("오류", "배치할 Event Trigger 프리팹을 지정해 주세요!", "확인");
            return;
        }

        if (targetSplineIndex < 0 || targetSplineIndex >= targetContainer.Splines.Count)
        {
            EditorUtility.DisplayDialog("오류", "유효하지 않은 Spline Index입니다.", "확인");
            return;
        }

        var spline = targetContainer.Splines[targetSplineIndex];
        
        // 스플라인의 로컬 총 길이 구하기
        float splineLength = spline.GetLength();

        if (splineLength <= (startOffset + endOffset))
            {
            EditorUtility.DisplayDialog("경고", "스플라인의 총 길이가 '시작점 마진 + 끝점 마진'보다 짧아 배치할 수 없습니다.", "확인");
            return;
        }

        // 깔끔한 정리를 위해 기존 동일 노선에 생성된 이벤트 묶음이 있다면 청소 후 재생성
        GameObject oldGroup = GameObject.Find($"Populated_Events_Spline_{targetSplineIndex}");
        if (oldGroup != null)
        {
            DestroyImmediate(oldGroup);
        }
        
        GameObject rootGo = new GameObject($"Populated_Events_Spline_{targetSplineIndex}");
        Undo.RegisterCreatedObjectUndo(rootGo, "Create Populated Events Group");

        int spawnedCount = 0;

        // 시작 오프셋부터 길이 범위 전까지 intervalDistance 간격으로 루프 순회
        for (float currentDistance = startOffset; currentDistance <= (splineLength - endOffset); currentDistance += intervalDistance)
        {
            // 💡 핵심 공식: 미터(Distance) 단위를 스플라인의 정규화 비율(Normalized t: 0~1)로 변환
            float t = SplineUtility.ConvertIndexUnit(spline, currentDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

            // 로컬 위치 및 진행방향 탄젠트 벡터 계산
            Vector3 localPos = spline.EvaluatePosition(t);
            Vector3 localTangent = spline.EvaluateTangent(t);

            // 로컬 좌표계를 월드 공간 좌표계로 정밀 변환
            Vector3 worldPos = targetContainer.transform.TransformPoint(localPos);
            Vector3 worldDir = targetContainer.transform.TransformDirection(localTangent).normalized;

            // 이동 경로가 바라보는 뱡향에 맞춰 트리거의 Forward 각도 자동 정렬
            Quaternion worldRot = Quaternion.identity;
            if (worldDir != Vector3.zero)
            {
                worldRot = Quaternion.LookRotation(worldDir);
            }

            // 프리팹 인스턴스 스폰
            GameObject spawnedObj = (GameObject)PrefabUtility.InstantiatePrefab(eventPrefab);
            if (spawnedObj != null)
            {
                spawnedObj.transform.position = worldPos;
                spawnedObj.transform.rotation = worldRot;
                spawnedObj.transform.SetParent(rootGo.transform);
                spawnedObj.name = $"{eventPrefab.name}_Spline{targetSplineIndex}_{spawnedCount:D2}";

                // 런타임에 완벽하게 동작하도록 필요한 컴포넌트 자동 자가 검사 마운팅
                Region regionComp = spawnedObj.GetComponent<Region>();
                if (regionComp == null)
                {
                    regionComp = spawnedObj.AddComponent<Region>();
                }
                regionComp.regionType = RegionType.Event; // 이벤트 타입 노드로 강제 지정하여 버튼 UI 생성 방지

                if (spawnedObj.GetComponent<RegionEventTrigger>() == null)
                {
                    spawnedObj.AddComponent<RegionEventTrigger>();
                }

                Undo.RegisterCreatedObjectUndo(spawnedObj, "Spawn Event Node on Spline");
                spawnedCount++;
            }
        }

        EditorUtility.SetDirty(rootGo);
        
        EditorUtility.DisplayDialog(
            "배치 완료", 
            $"성공적으로 {targetSplineIndex}번 스플라인 레일을 따라 총 {spawnedCount}개의 이벤트 노드를 균일 배치했습니다!", 
            "확인"
        );
    }
}
#endif