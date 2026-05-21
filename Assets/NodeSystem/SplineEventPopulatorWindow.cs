using UnityEngine;
using UnityEngine.Splines;
using UnityEditor;

public class SplineEventPopulatorWindow : EditorWindow
{
    private SplineContainer targetContainer;
    private int targetSplineIndex = 0;
    private GameObject eventPrefab;
    
    private float intervalDistance = 15f; // X미터 간격 자동 설치
    private float startOffset = 5f;       // 시작 거점 안전 마진
    private float endOffset = 5f;         // 종착 거점 안전 마진
    
    [MenuItem("Tools/Spline Event Populator")]
    public static void ShowWindow()
    {
        var window = GetWindow<SplineEventPopulatorWindow>("Event Populator");
        window.minSize = new Vector2(380, 280);
    }

    private void OnGUI()
    {
        GUILayout.Label("Spline Event Populator Tool (Clean Ver.)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("이동형 본진 주행 레일을 따라 보이지 않는 물리적 위협 트리거들을 자동 배치합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        targetContainer = (SplineContainer)EditorGUILayout.ObjectField(
            "Target Spline Container", 
            targetContainer, 
            typeof(SplineContainer), 
            true
        );

        if (targetContainer != null)
        {
            int maxIndex = targetContainer.Splines.Count - 1;
            if (maxIndex < 0)
            {
                EditorGUILayout.HelpBox("해당 컨테이너 내에 스플라인 궤도가 감지되지 않습니다.", MessageType.Warning);
            }
            else
            {
                targetSplineIndex = EditorGUILayout.IntSlider("Target Spline Index", targetSplineIndex, 0, maxIndex);
            }
        }

        // 배치할 위협 트리거 프리팹 (SplineEventTrigger 컴포넌트가 무조건 붙어있어야 함)
        eventPrefab = (GameObject)EditorGUILayout.ObjectField("Event Trigger Prefab", eventPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(10);
        GUILayout.Label("미터 단위 간격 상세 피드백", EditorStyles.boldLabel);
        intervalDistance = EditorGUILayout.FloatField("스폰 간격 (Distance)", intervalDistance);
        startOffset = EditorGUILayout.FloatField("시작점 마진 (Start Margin)", startOffset);
        endOffset = EditorGUILayout.FloatField("끝점 마진 (End Margin)", endOffset);

        intervalDistance = Mathf.Max(1.0f, intervalDistance);
        startOffset = Mathf.Max(0f, startOffset);
        endOffset = Mathf.Max(0f, endOffset);

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f); // 경고 및 위협 성격에 맞춘 딥 레드 버튼
        if (GUILayout.Button("독립형 위협 트리거 자동 배치 실행", GUILayout.Height(42)))
        {
            PopulateCleanEventsOnSpline();
        }
        GUI.backgroundColor = Color.white;
    }

    private void PopulateCleanEventsOnSpline()
    {
        if (targetContainer == null)
        {
            EditorUtility.DisplayDialog("경고", "대상 Spline Container를 인스펙터에 먼저 드래그해 주세요!", "확인");
            return;
        }

        if (eventPrefab == null)
        {
            EditorUtility.DisplayDialog("경고", "배치할 위협 트리거 프리팹(Prefab)을 지정해 주세요!", "확인");
            return;
        }

        if (targetSplineIndex < 0 || targetSplineIndex >= targetContainer.Splines.Count)
        {
            EditorUtility.DisplayDialog("경고", "스플라인 인덱스 설정 오류입니다.", "확인");
            return;
        }

        var spline = targetContainer.Splines[targetSplineIndex];
        float splineLength = spline.GetLength();

        if (splineLength <= (startOffset + endOffset))
        {
            EditorUtility.DisplayDialog("중단", "스플라인의 총 수평 길이가 너무 짧아 시작/끝 마진 영역을 채우지 못합니다.", "확인");
            return;
        }

        // 해당 스플라인을 가리키는 기존 생성 그룹 청소
        GameObject oldGroup = GameObject.Find($"Populated_Triggers_Spline_{targetSplineIndex}");
        if (oldGroup != null)
        {
            DestroyImmediate(oldGroup);
        }
        
        GameObject rootGo = new GameObject($"Populated_Triggers_Spline_{targetSplineIndex}");
        Undo.RegisterCreatedObjectUndo(rootGo, "Create Populated Triggers Group");

        int spawnedCount = 0;

        for (float currentDistance = startOffset; currentDistance <= (splineLength - endOffset); currentDistance += intervalDistance)
        {
            // 거리 데이터를 정규화 진행률(t: 0~1)로 보간 변환
            float t = SplineUtility.ConvertIndexUnit(spline, currentDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

            Vector3 localPos = spline.EvaluatePosition(t);
            Vector3 localTangent = spline.EvaluateTangent(t);

            Vector3 worldPos = targetContainer.transform.TransformPoint(localPos);
            Vector3 worldDir = targetContainer.transform.TransformDirection(localTangent).normalized;

            Quaternion worldRot = Quaternion.identity;
            if (worldDir != Vector3.zero)
            {
                worldRot = Quaternion.LookRotation(worldDir);
            }

            GameObject spawnedObj = (GameObject)PrefabUtility.InstantiatePrefab(eventPrefab);
            if (spawnedObj != null)
            {
                spawnedObj.transform.position = worldPos;
                spawnedObj.transform.rotation = worldRot;
                spawnedObj.transform.SetParent(rootGo.transform);
                spawnedObj.name = $"{eventPrefab.name}_Trig_{targetSplineIndex}_{spawnedCount:D2}";

                // [클린 보강]: 이 프리팹에는 Region 컴포넌트가 전혀 들어갈 필요가 없습니다!
                // 만약 없다면 이벤트 발생을 처리해줄 독립형 트리거 스크립트만 런타임 검사 후 부착해 줍니다.
                SplineEventTrigger triggerComp = spawnedObj.GetComponent<SplineEventTrigger>();
                if (triggerComp == null)
                {
                    triggerComp = spawnedObj.AddComponent<SplineEventTrigger>();
                }

                Undo.RegisterCreatedObjectUndo(spawnedObj, "Spawn Event Trigger on Spline");
                spawnedCount++;
            }
        }

        EditorUtility.SetDirty(rootGo);
        
        EditorUtility.DisplayDialog(
            "배치 자동 완수", 
            $"성공적으로 {targetSplineIndex}번 노선을 추적하여 총 {spawnedCount}개의 순수 독립형 위협 트리거를 칼정렬 배치했습니다!", 
            "확인"
        );
    }
}