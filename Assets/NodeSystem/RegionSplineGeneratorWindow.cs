using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;

public class RegionSplineGeneratorWindow : EditorWindow
{
    private SplineContainer targetContainer;
    private float curveStrength = 0.25f; // 곡률 핸들 장력 (0이면 각진 철로, 높을수록 둥글둥글해짐)
    private bool preventBidirectionalDuplicates = true; // 양방향 궤도 중복생성 자동 방지 플래그

    [MenuItem("Tools/Region Spline Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<RegionSplineGeneratorWindow>("Region Spline Tool");
        window.minSize = new Vector2(360, 220);
    }

    private void OnGUI()
    {
        GUILayout.Label("Region Spline Chain Generator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("거점과 중간 이벤트 노드 체인을 분석해 단일 스플라인 트랙을 자동 구성합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        targetContainer = (SplineContainer)EditorGUILayout.ObjectField(
            "Target Spline Container", 
            targetContainer, 
            typeof(SplineContainer), 
            true
        );

        curveStrength = EditorGUILayout.Slider(
            new GUIContent("Curve Tension", "곡률의 탄젠트 강도입니다. 0이면 선형 직선 레일이 됩니다."), 
            curveStrength, 
            0f, 
            1f
        );

        preventBidirectionalDuplicates = EditorGUILayout.Toggle(
            new GUIContent("A <-> B 중복 자동 제어", "양방향 노선일 때 스플라인 개수를 단 1개만 생성하여 리소스를 보존합니다."), 
            preventBidirectionalDuplicates
        );

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.12f, 0.75f, 0.45f);
        if (GUILayout.Button("자동 Spline 생성 실행", GUILayout.Height(42)))
        {
            GenerateChainSplines();
        }
        GUI.backgroundColor = Color.white;
    }

    /// <summary>
    /// 그래프 연결망을 탐색해 Foothold -> (N개의 Event) -> Foothold 체인을 찾아 단일 스플라인으로 가공합니다.
    /// </summary>
    private void GenerateChainSplines()
    {
        if (targetContainer == null)
        {
            GameObject containerGo = new GameObject("Generated_Spline_Container");
            targetContainer = containerGo.AddComponent<SplineContainer>();
            Undo.RegisterCreatedObjectUndo(containerGo, "Create Spline Container");
        }

        Undo.RegisterCompleteObjectUndo(targetContainer, "Generate Regions Splines");
        
        // 기존 찌꺼기 스플라인 청소
        var existingSplines = targetContainer.Splines;
        for (int i = existingSplines.Count - 1; i >= 0; i--)
        {
            targetContainer.RemoveSpline(existingSplines[i]);
        }

        Region[] allRegions = FindObjectsOfType<Region>();
        if (allRegions.Length == 0)
        {
            EditorUtility.DisplayDialog("경고", "씬에 배치된 Region 노드가 하나도 없습니다!", "확인");
            return;
        }

        List<List<Region>> pathsToBuild = new List<List<Region>>();
        HashSet<string> uniquePaths = new HashSet<string>();

        // 1단계: 모든 거점(Foothold)에서 시작하여 중간에 Event 노드를 거쳐 다른 거점에 닿는 체인을 탐색합니다.
        foreach (Region startRegion in allRegions)
        {
            if (startRegion.regionType != RegionType.Foothold) continue;

            foreach (Region neighbor in startRegion.connectedRegions)
            {
                if (neighbor == null) continue;

                List<Region> currentChain = new List<Region> { startRegion, neighbor };
                HashSet<Region> visited = new HashSet<Region> { startRegion, neighbor };

                List<List<Region>> chainsFromThisNeighbor = new List<List<Region>>();
                FindChainsRecursively(neighbor, currentChain, visited, chainsFromThisNeighbor);

                foreach (var chain in chainsFromThisNeighbor)
                {
                    Region endRegion = chain[chain.Count - 1];
                    string pathKey1 = $"{startRegion.GetInstanceID()}_{endRegion.GetInstanceID()}";
                    string pathKey2 = $"{endRegion.GetInstanceID()}_{startRegion.GetInstanceID()}";

                    if (preventBidirectionalDuplicates && (uniquePaths.Contains(pathKey1) || uniquePaths.Contains(pathKey2)))
                    {
                        continue;
                    }

                    pathsToBuild.Add(chain);
                    uniquePaths.Add(pathKey1);
                }
            }
        }

        // 2단계: 선별된 완결성 체인 리스트를 유니티 실제 베지어 스플라인 노드로 구축합니다.
        int createdSplineCount = 0;
        foreach (var path in pathsToBuild)
        {
            Spline newSpline = targetContainer.AddSpline();

            // 위치 정보 Knot 리스트에 적재
            for (int k = 0; k < path.Count; k++)
            {
                Vector3 localPos = targetContainer.transform.InverseTransformPoint(path[k].transform.position);
                BezierKnot knot = new BezierKnot();
                knot.Position = localPos;
                newSpline.Add(knot);
            }

            // 3단계: 멀티 노드 곡선이 부드럽게 유지되도록 양방향 탄젠트 방향 보간 연산 수행
            if (curveStrength > 0)
            {
                for (int k = 0; k < newSpline.Count; k++)
                {
                    BezierKnot knot = newSpline[k];
                    Vector3 tangentIn = Vector3.zero;
                    Vector3 tangentOut = Vector3.zero;

                    if (k == 0) // 출발 노드
                    {
                        Vector3 dir = ((Vector3)newSpline[1].Position - (Vector3)newSpline[0].Position);
                        tangentOut = dir.normalized * (dir.magnitude * curveStrength);
                    }
                    else if (k == newSpline.Count - 1) // 종착 노드
                    {
                        Vector3 dir = ((Vector3)newSpline[k].Position - (Vector3)newSpline[k - 1].Position);
                        tangentIn = -dir.normalized * (dir.magnitude * curveStrength);
                    }
                    else // 중간에 꺾이는 지점 (Event 노드들) - 앞뒤 벡터 합 연산으로 부드러운 핸들 산출
                    {
                        Vector3 toNext = ((Vector3)newSpline[k + 1].Position - (Vector3)newSpline[k].Position);
                        Vector3 fromPrev = ((Vector3)newSpline[k].Position - (Vector3)newSpline[k - 1].Position);
                        Vector3 smoothDir = (toNext.normalized + fromPrev.normalized).normalized;

                        float magnitude = Mathf.Min(toNext.magnitude, fromPrev.magnitude) * curveStrength;
                        tangentIn = -smoothDir * magnitude;
                        tangentOut = smoothDir * magnitude;
                    }

                    knot.TangentIn = tangentIn;
                    knot.TangentOut = tangentOut;
                    newSpline[k] = knot;
                }
            }

            createdSplineCount++;
        }

        EditorUtility.SetDirty(targetContainer);
        SceneView.RepaintAll();

        EditorUtility.DisplayDialog(
            "빌드 완수", 
            $"성공적으로 {pathsToBuild.Count}개의 체인을 추출하고 정밀하게 설계된 스플라인 트랙을 드로잉 완료했습니다!", 
            "확인"
        );
    }

    /// <summary>
    /// DFS(깊이 우선 탐색)를 활용하여 Foothold와 Foothold 사이의 중간 Event 노선 경로들을 재귀 수집합니다.
    /// </summary>
    private void FindChainsRecursively(Region current, List<Region> currentChain, HashSet<Region> visited, List<List<Region>> allChains)
    {
        // 베이스 캠프 도달: 중간 노드가 Foothold(거점)에 무사 도달 시 탐색 성공 처리
        if (current.regionType == RegionType.Foothold && currentChain.Count > 1)
        {
            allChains.Add(new List<Region>(currentChain));
            return;
        }

        foreach (Region neighbor in current.connectedRegions)
        {
            if (neighbor == null) continue;
            
            // 바로 직전에 지나온 노드로 다시 빽하는 비정상 역루프 필터링
            if (currentChain.Count > 1 && neighbor == currentChain[currentChain.Count - 2]) continue;
            
            // 무한 순환 루프 방지용 백트래킹
            if (visited.Contains(neighbor)) continue;

            visited.Add(neighbor);
            currentChain.Add(neighbor);

            FindChainsRecursively(neighbor, currentChain, visited, allChains);

            currentChain.RemoveAt(currentChain.Count - 1);
            visited.Remove(neighbor);
        }
    }
}
#endif