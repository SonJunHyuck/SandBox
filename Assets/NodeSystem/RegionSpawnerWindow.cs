using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class RegionSpawnerWindow : EditorWindow
{
    // 배치할 Region 프리팹 리스트
    private List<Region> regionPrefabs = new List<Region>();
    
    // 배치 설정 변수들
    private Vector3 spawnCenter = Vector3.zero;
    private float areaWidth = 30f;   // 가로 (X축)
    private float areaHeight = 5f;   // 높이 (Y축)
    private float areaDepth = 30f;   // 세로 (Z축)
    private int spawnCount = 10;     // 스폰할 개수

    // 에디터 UI 스크롤용
    private Vector2 scrollPos;
    private bool showPrefabList = true;

    [MenuItem("Tools/Region Spawner")]
    public static void ShowWindow()
    {
        var window = GetWindow<RegionSpawnerWindow>("Region Spawner");
        window.minSize = new Vector2(380, 450);
    }

    private void OnEnable()
    {
        // 씬 뷰에 영역 상자를 실시간으로 그려주기 위해 델리게이트 등록
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Region Random Spawner", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("입력한 영역(Box) 안에 Region 프리팹을 랜덤하게 배치합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 1. 프리팹 리스트 영역
        showPrefabList = EditorGUILayout.BeginFoldoutHeaderGroup(showPrefabList, $"Region 프리팹 목록 ({regionPrefabs.Count}개)");
        if (showPrefabList)
        {
            int newSize = EditorGUILayout.IntField("리스트 크기", regionPrefabs.Count);
            while (newSize > regionPrefabs.Count) regionPrefabs.Add(null);
            while (newSize < regionPrefabs.Count) regionPrefabs.RemoveAt(regionPrefabs.Count - 1);

            EditorGUI.indentLevel++;
            for (int i = 0; i < regionPrefabs.Count; i++)
            {
                regionPrefabs[i] = (Region)EditorGUILayout.ObjectField($"슬롯 {i}", regionPrefabs[i], typeof(Region), false);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(15);

        // 2. 랜덤 영역 경계값 설정
        GUILayout.Label("배치 영역 및 수량 설정", EditorStyles.boldLabel);
        spawnCenter = EditorGUILayout.Vector3Field("영역 중심 좌표 (Center)", spawnCenter);
        
        EditorGUILayout.Space(5);
        areaWidth = EditorGUILayout.FloatField("가로 폭 (Width - X)", areaWidth);
        areaHeight = EditorGUILayout.FloatField("높이 상한 (Height - Y)", areaHeight);
        areaDepth = EditorGUILayout.FloatField("세로 폭 (Depth - Z)", areaDepth);
        
        // 마이너스 값 방지 예외 처리
        areaWidth = Mathf.Max(0.1f, areaWidth);
        areaHeight = Mathf.Max(0.1f, areaHeight);
        areaDepth = Mathf.Max(0.1f, areaDepth);

        EditorGUILayout.Space(5);
        spawnCount = EditorGUILayout.IntField("스폰 생성 개수", spawnCount);
        spawnCount = Mathf.Max(1, spawnCount);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(15);

        // 3. 실행 버튼
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("랜덤 배치 실행", GUILayout.Height(45)))
        {
            ExecuteRandomSpawn();
        }
        GUI.backgroundColor = Color.white;
    }

    // 씬 뷰가 활성화되어 있을 때 영역 경계선을 시각적으로 그려줍니다.
    private void OnSceneGUI(SceneView sceneView)
    {
        Handles.color = new Color(0f, 0.8f, 1f, 0.3f);
        // 내부가 채워진 투명 박스 그리기
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[] {
                spawnCenter + new Vector3(-areaWidth/2, -areaHeight/2, -areaDepth/2),
                spawnCenter + new Vector3(areaWidth/2, -areaHeight/2, -areaDepth/2),
                spawnCenter + new Vector3(areaWidth/2, -areaHeight/2, areaDepth/2),
                spawnCenter + new Vector3(-areaWidth/2, -areaHeight/2, areaDepth/2)
            },
            new Color(0f, 0.8f, 1f, 0.05f),
            new Color(0f, 0.8f, 1f, 0.8f)
        );

        // 와이어프레임 박스 그리기
        Handles.color = Color.cyan;
        Handles.DrawWireCube(spawnCenter, new Vector3(areaWidth, areaHeight, areaDepth));
        
        // 씬 뷰에 가이드 텍스트 표시
        Handles.Label(spawnCenter + Vector3.up * (areaHeight / 2 + 1f), "Region Spawning Bounds", new GUIStyle() {
            normal = { textColor = Color.cyan },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        });
    }

    private void ExecuteRandomSpawn()
    {
        // 예외 검사 1: 프리팹 리스트가 비어있거나 다 null인지 확인
        List<Region> validPrefabs = regionPrefabs.FindAll(p => p != null);
        if (validPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("경고", "배치할 Region 프리팹을 최소 한 개 이상 등록해주세요!", "확인");
            return;
        }

        // 깔끔한 정리를 위해 부모 Group 오브젝트 생성 및 찾기
        GameObject rootGo = GameObject.Find("Spawned_Regions_Group");
        if (rootGo == null)
        {
            rootGo = new GameObject("Spawned_Regions_Group");
            Undo.RegisterCreatedObjectUndo(rootGo, "Create Regions Group");
        }

        int successfullySpawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            // 리스트에서 무작위로 프리팹 하나 선정
            Region selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];

            // 범위 안에서 랜덤한 로컬 오프셋 좌표 계산
            float randomX = Random.Range(-areaWidth / 2f, areaWidth / 2f);
            float randomY = Random.Range(-areaHeight / 2f, areaHeight / 2f);
            float randomZ = Random.Range(-areaDepth / 2f, areaDepth / 2f);

            Vector3 spawnPosition = spawnCenter + new Vector3(randomX, randomY, randomZ);

            // 오브젝트 인스턴스화
            GameObject spawnedObj = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab.gameObject);
            if (spawnedObj != null)
            {
                spawnedObj.transform.position = spawnPosition;
                spawnedObj.transform.SetParent(rootGo.transform);
                spawnedObj.name = $"{selectedPrefab.name}_Rand_{i:D2}";

                // 실행 취소(Undo) 스택에 등록
                Undo.RegisterCreatedObjectUndo(spawnedObj, "Spawn Random Region");
                successfullySpawned++;
            }
        }

        // 변경점 저장
        EditorUtility.SetDirty(rootGo);

        EditorUtility.DisplayDialog(
            "배치 완료", 
            $"성공적으로 {successfullySpawned}개의 Region 노드를 랜덤 좌표 영역에 생성 및 배치했습니다!", 
            "확인"
        );
    }
}
#endif