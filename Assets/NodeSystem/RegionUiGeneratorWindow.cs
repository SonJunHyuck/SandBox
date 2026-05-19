
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public class RegionUiGeneratorWindow : EditorWindow
{
    private float heightOffset = 1.8f;   // Region 위로 띄울 높이 오프셋
    private Vector2 buttonSize = new Vector2(140f, 45f); // 버튼 UI 사이즈

    [MenuItem("Tools/Region UI Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<RegionUiGeneratorWindow>("Region UI Generator");
        window.minSize = new Vector2(320, 180);
    }

    private void OnGUI()
    {
        GUILayout.Label("Region World UI Generator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("씬의 모든 Region 머리 위에 월드 UI 버튼을 생성합니다.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(10);

        heightOffset = EditorGUILayout.FloatField("Y축 높이 오프셋 (Offset Y)", heightOffset);
        buttonSize = EditorGUILayout.Vector2Field("버튼 크기 (Button Size)", buttonSize);

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.2f, 0.7f, 0.9f);
        if (GUILayout.Button("World UI 일괄 생성 실행", GUILayout.Height(40)))
        {
            GenerateWorldUis();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateWorldUis()
    {
        Region[] allRegions = FindObjectsOfType<Region>();
        if (allRegions.Length == 0)
        {
            EditorUtility.DisplayDialog("경고", "씬에 배치된 Region 컴포넌트가 하나도 없습니다!", "확인");
            return;
        }

        // 기존에 생성된 UI 그룹 오브젝트가 있다면 깔끔하게 지워 중복 생성을 막습니다.
        GameObject oldGroup = GameObject.Find("Region_World_UI_Group");
        if (oldGroup != null)
        {
            DestroyImmediate(oldGroup);
        }

        // 1. 새로운 UI 부모 그룹 생성
        GameObject uiGroup = new GameObject("Region_World_UI_Group");
        Undo.RegisterCreatedObjectUndo(uiGroup, "Create Region World UI Group");

        int successfullyCreated = 0;

        foreach (Region region in allRegions)
        {
            if (region == null) continue;

            // 2. 개별 월드 캔버스 월드 공간 생성
            GameObject canvasGo = new GameObject($"Canvas_{region.name}");
            canvasGo.transform.SetParent(uiGroup.transform);
            canvasGo.transform.position = region.transform.position + Vector3.up * heightOffset;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // UI 캔버스 사이즈 조절 및 리사이징 (월드 좌표이므로 스케일을 정밀하게 극도로 줄여야 적당히 보입니다)
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = buttonSize;
            canvasGo.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);

            // 3. 버튼 오브젝트 생성 및 레이아웃 배치
            GameObject btnGo = new GameObject("Btn_Node");
            btnGo.transform.SetParent(canvasGo.transform, false);

            Image btnImage = btnGo.AddComponent<Image>();
            // 깊이 있고 깔끔한 다크 사이언 계열 컬러 적용
            btnImage.color = new Color(0.12f, 0.16f, 0.22f, 0.9f);

            Button button = btnGo.AddComponent<Button>();
            
            // 버튼 상태에 따른 시각 피드백 컬러 세팅 (Navigator와 상호작용할 때 눈에 띔)
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.cyan;
            colors.pressedColor = Color.gray;
            colors.disabledColor = new Color(0.3f, 0.35f, 0.4f, 0.35f); // 비활성화 노드는 흐리게 처리
            button.colors = colors;

            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = buttonSize;

            // 4. 노드 이름을 표기해 줄 텍스트 생성
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.text = region.name;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;

            RectTransform txtRect = textGo.GetComponent<RectTransform>();
            txtRect.sizeDelta = buttonSize;

            // 5. 생성된 버튼을 해당 Region 스크립트의 worldSpaceButton 변수에 기록합니다.
            Undo.RecordObject(region, "Assign World Space Button");
            region.worldSpaceButton = button;
            PrefabUtility.RecordPrefabInstancePropertyModifications(region);

            successfullyCreated++;
        }

        // 씬 변경 사항 기록 저장
        EditorUtility.SetDirty(uiGroup);
        
        EditorUtility.DisplayDialog(
            "UI 생성 완료", 
            $"성공적으로 {successfullyCreated}개의 Region 노드 머리 위에 월드 공간(World Space) UI 버튼을 부착했습니다!", 
            "확인"
        );
    }
}
#endif