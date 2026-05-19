using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ==========================================================================
// 2. 에디터 자동 생성 툴 윈도우 스크립트
// ==========================================================================
#if UNITY_EDITOR
public class RegionSplineGeneratorWindow : EditorWindow
{
private SplineContainer targetContainer;
private float curveStrength = 0.3f; // Bezier 곡선 강도 (0이면 완전 직선 탄젠트)
private bool preventBidirectionalDuplicates = true; // A->B와 B->A가 둘 다 있을 때 1개 스플라인만 생성할 것인지 여부

[MenuItem("Tools/Region Spline Generator")]
public static void ShowWindow()
{
    var window = GetWindow<RegionSplineGeneratorWindow>("Region Spline Tool");
    window.minSize = new Vector2(350, 220);
}

private void OnGUI()
{
    GUILayout.Label("Region Spline Generator", EditorStyles.boldLabel);
    EditorGUILayout.LabelField("씬에 배치된 Region 컴포넌트들을 분석하여 스플라인을 빌드합니다.", EditorStyles.wordWrappedLabel);
    EditorGUILayout.Space(10);

    // 1. 타겟 오브젝트 지정
    targetContainer = (SplineContainer)EditorGUILayout.ObjectField(
        "Target Spline Container", 
        targetContainer, 
        typeof(SplineContainer), 
        true
    );

    // 2. 곡률 강도 설정
    curveStrength = EditorGUILayout.Slider(
        new GUIContent("Curve Strength", "값이 클수록 둥글게 휘어지고, 0이면 직선(Linear) 철로가 됩니다."), 
        curveStrength, 
        0f, 
        1f
    );

    // 3. 중복 생성 방지 여부
    preventBidirectionalDuplicates = EditorGUILayout.Toggle(
        new GUIContent("A <-> B 중복 방지", "양방향 연결 시 하나의 스플라인만 생성해 리소스를 절약합니다."), 
        preventBidirectionalDuplicates
    );

    EditorGUILayout.Space(15);

    // 실행 버튼
    GUI.backgroundColor = new Color(0.1f, 0.8f, 0.4f);
    if (GUILayout.Button("자동 Spline 생성 실행", GUILayout.Height(40)))
    {
        GenerateSplines();
    }
    GUI.backgroundColor = Color.white;
}

private void GenerateSplines()
{
    // 대상 컨테이너가 지정되어 있지 않다면 새로 자동 생성
    if (targetContainer == null)
    {
        GameObject containerGo = new GameObject("Generated_Spline_Container");
        targetContainer = containerGo.AddComponent<SplineContainer>();
        Undo.RegisterCreatedObjectUndo(containerGo, "Create Spline Container");
    }

    // 언두(Undo) 등록 및 기존 스플라인 모두 초기화
    Undo.RegisterCompleteObjectUndo(targetContainer, "Generate Regions Splines");
    
    // 안전하게 기존 내장 스플라인 컬렉션 비우기
    var existingSplines = targetContainer.Splines;
    for (int i = existingSplines.Count - 1; i >= 0; i--)
    {
        targetContainer.RemoveSpline(existingSplines[i]);
    }

    // 씬 내의 모든 Region 컴포넌트 검색
    Region[] allRegions = FindObjectsOfType<Region>();
    if (allRegions.Length == 0)
    {
        EditorUtility.DisplayDialog("경고", "씬에 배치된 Region 컴포넌트가 하나도 없습니다!", "확인");
        return;
    }

    // 중복 생성을 막기 위한 해시셋 키셋 구성
    HashSet<string> processedEdges = new HashSet<string>();
    int createdSplineCount = 0;

    foreach (Region parent in allRegions)
    {
        if (parent.connectedRegions == null) continue;

        foreach (Region child in parent.connectedRegions)
        {
            if (child == null || parent == child) continue;

            // 고유 에지 키 빌드 (A_B 형태)
            string edgeKey1 = $"{parent.GetInstanceID()}_{child.GetInstanceID()}";
            string edgeKey2 = $"{child.GetInstanceID()}_{parent.GetInstanceID()}";

            // 중복 체크 작동 중이고 이미 반대 방향이 생성되었다면 통과
            if (preventBidirectionalDuplicates && 
                (processedEdges.Contains(edgeKey1) || processedEdges.Contains(edgeKey2)))
            {
                continue;
            }

            // 새로운 스플라인 추가 생성
            Spline newSpline = targetContainer.AddSpline();

            // 스플라인 컨테이너의 로컬 좌표계 기준으로 월드 좌표 변환 대입
            Vector3 localPosStart = targetContainer.transform.InverseTransformPoint(parent.transform.position);
            Vector3 localPosEnd = targetContainer.transform.InverseTransformPoint(child.transform.position);

            // BezierKnot 생성 (시작 노드 A)
            BezierKnot knotA = new BezierKnot();
            knotA.Position = localPosStart;

            // BezierKnot 생성 (도착 노드 B)
            BezierKnot knotB = new BezierKnot();
            knotB.Position = localPosEnd;

            // -------------------------------------------------------------
            // 💡 곡률 핸들(Tangent) 자동 연산 공식 적용
            // 두 노드의 방향성 및 거리에 비례하여 부드러운 핸들을 자동 분배합니다.
            // -------------------------------------------------------------
            Vector3 direction = localPosEnd - localPosStart;
            float distance = direction.magnitude;
            Vector3 normalizedDir = direction.normalized;

            if (curveStrength > 0)
            {
                // 시작점의 출구 탄젠트는 나가는 방향(normalizedDir)으로 연장
                knotA.TangentOut = normalizedDir * (distance * curveStrength);
                // 도착점의 입구 탄젠트는 들어오는 반대 방향(-normalizedDir)으로 연장
                knotB.TangentIn = -normalizedDir * (distance * curveStrength);
            }
            else
            {
                // 곡률이 0이면 완벽한 직선(Linear) 모드로 처리
                knotA.TangentOut = Vector3.zero;
                knotB.TangentIn = Vector3.zero;
            }

            newSpline.Add(knotA);
            newSpline.Add(knotB);

            // 처리 이력 기록
            processedEdges.Add(edgeKey1);
            createdSplineCount++;
        }
    }

    // 변경사항 저장 및 뷰 갱신
    EditorUtility.SetDirty(targetContainer);
    SceneView.RepaintAll();

    EditorUtility.DisplayDialog(
        "생성 완료", 
        $"성공적으로 {allRegions.Length}개의 Region 노드를 분석하여 총 {createdSplineCount}개의 Spline 연결 간선을 자동 생성했습니다!", 
        "확인"
    );
}


}
#endif