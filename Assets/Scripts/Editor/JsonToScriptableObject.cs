#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VampireSurvivors.Editor
{
    /// <summary>
    /// JSON 파일을 ScriptableObject로 변환하는 에디터 윈도우.
    /// Unity 에디터 메뉴 Tools > Data Pipeline > JSON to ScriptableObject로 접근합니다.
    /// </summary>
    /// <remarks>
    /// 사용법:
    /// 1. JSON 파일을 드래그하여 JSON File 필드에 할당
    /// 2. ScriptableObject Class에 변환할 SO 클래스 이름 입력 (예: CharacterDataSO)
    /// 3. Output Path에 저장할 경로 지정
    /// 4. "Convert to ScriptableObject" 버튼 클릭 (단일 객체)
    ///    또는 "Convert JSON Array to Multiple SOs" 버튼 클릭 (배열 → 다중 SO)
    /// </remarks>
    public class JsonToScriptableObject : EditorWindow
    {
        /// <summary>변환할 JSON 파일 (TextAsset)</summary>
        private TextAsset _jsonFile;

        /// <summary>ScriptableObject 저장 경로</summary>
        private string _outputPath = "Assets/ScriptableObjects";

        /// <summary>대상 ScriptableObject 클래스 이름</summary>
        private string _soClassName = "";

        /// <summary>JSON 미리보기 스크롤 위치</summary>
        private Vector2 _scrollPosition;

        /// <summary>포맷된 JSON 미리보기 텍스트</summary>
        private string _previewText = "";

        /// <summary>
        /// 에디터 메뉴에 윈도우 항목을 추가합니다.
        /// Tools > Data Pipeline > JSON to ScriptableObject
        /// </summary>
        [MenuItem("Tools/Data Pipeline/JSON to ScriptableObject")]
        public static void ShowWindow()
        {
            var window = GetWindow<JsonToScriptableObject>("JSON to SO");
            window.minSize = new Vector2(400, 300);
        }

        /// <summary>
        /// 에디터 윈도우 GUI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("JSON to ScriptableObject Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // JSON 파일 필드 - 파일 변경 시 미리보기 업데이트
            EditorGUI.BeginChangeCheck();
            _jsonFile = (TextAsset)EditorGUILayout.ObjectField("JSON File", _jsonFile, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck() && _jsonFile != null)
            {
                _previewText = FormatJson(_jsonFile.text);
            }

            EditorGUILayout.Space(5);
            _soClassName = EditorGUILayout.TextField("ScriptableObject Class", _soClassName);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            EditorGUILayout.Space(10);

            // JSON 미리보기 영역
            if (_jsonFile != null)
            {
                EditorGUILayout.LabelField("JSON Preview:", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
                EditorGUILayout.TextArea(_previewText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            // 단일 객체 변환 버튼
            EditorGUI.BeginDisabledGroup(_jsonFile == null || string.IsNullOrEmpty(_soClassName));
            if (GUILayout.Button("Convert to ScriptableObject", GUILayout.Height(30)))
            {
                ConvertJsonToSO();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // 배열 → 다중 SO 변환 버튼
            if (GUILayout.Button("Convert JSON Array to Multiple SOs", GUILayout.Height(30)))
            {
                ConvertJsonArrayToMultipleSOs();
            }
        }

        /// <summary>
        /// 단일 JSON 객체를 하나의 ScriptableObject로 변환합니다.
        /// JSON 구조가 SO 필드와 일치해야 합니다.
        /// </summary>
        private void ConvertJsonToSO()
        {
            if (_jsonFile == null || string.IsNullOrEmpty(_soClassName))
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file and specify SO class name.", "OK");
                return;
            }

            // ScriptableObject 타입 검색
            Type soType = GetScriptableObjectType(_soClassName);
            if (soType == null)
            {
                EditorUtility.DisplayDialog("Error", $"ScriptableObject class '{_soClassName}' not found.", "OK");
                return;
            }

            try
            {
                // 출력 디렉토리 생성
                EnsureDirectoryExists(_outputPath);

                // SO 인스턴스 생성 및 JSON 데이터 적용
                ScriptableObject so = ScriptableObject.CreateInstance(soType);
                JsonUtility.FromJsonOverwrite(_jsonFile.text, so);

                // 에셋으로 저장
                string assetPath = $"{_outputPath}/{_jsonFile.name}.asset";
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success", $"Created: {assetPath}", "OK");
                Selection.activeObject = so; // 생성된 에셋 선택
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to convert: {e.Message}", "OK");
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// JSON 배열을 여러 개의 ScriptableObject로 변환합니다.
        /// JSON이 배열 형태([{...}, {...}])여야 합니다.
        /// </summary>
        private void ConvertJsonArrayToMultipleSOs()
        {
            if (_jsonFile == null || string.IsNullOrEmpty(_soClassName))
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file and specify SO class name.", "OK");
                return;
            }

            Type soType = GetScriptableObjectType(_soClassName);
            if (soType == null)
            {
                EditorUtility.DisplayDialog("Error", $"ScriptableObject class '{_soClassName}' not found.", "OK");
                return;
            }

            try
            {
                EnsureDirectoryExists(_outputPath);

                // JSON 배열을 래퍼 객체로 감싸서 파싱
                // Unity JsonUtility는 최상위 배열을 직접 파싱할 수 없음
                string wrappedJson = $"{{\"items\":{_jsonFile.text}}}";
                var wrapper = JsonUtility.FromJson<JsonArrayWrapper>(wrappedJson);

                if (wrapper == null || wrapper.items == null)
                {
                    EditorUtility.DisplayDialog("Error", "JSON is not a valid array.", "OK");
                    return;
                }

                int count = 0;
                foreach (var item in wrapper.items)
                {
                    // 각 배열 요소를 개별 SO로 변환
                    ScriptableObject so = ScriptableObject.CreateInstance(soType);
                    string itemJson = JsonUtility.ToJson(item);
                    JsonUtility.FromJsonOverwrite(itemJson, so);

                    // id 또는 name 필드로 에셋 이름 결정
                    string assetName = GetAssetName(so, count);
                    string assetPath = $"{_outputPath}/{assetName}.asset";
                    AssetDatabase.CreateAsset(so, assetPath);
                    count++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success", $"Created {count} ScriptableObjects in {_outputPath}", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to convert array: {e.Message}", "OK");
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 클래스 이름으로 ScriptableObject 타입을 검색합니다.
        /// VampireSurvivors.Data 네임스페이스도 자동으로 검색합니다.
        /// </summary>
        /// <param name="className">검색할 클래스 이름</param>
        /// <returns>찾은 Type 또는 null</returns>
        private Type GetScriptableObjectType(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 전체 이름으로 검색
                Type type = assembly.GetType(className);
                if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
                    return type;

                // VampireSurvivors.Data 네임스페이스에서 검색
                type = assembly.GetType($"VampireSurvivors.Data.{className}");
                if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
                    return type;
            }
            return null;
        }

        /// <summary>
        /// SO의 id 또는 name 필드 값으로 에셋 이름을 결정합니다.
        /// 해당 필드가 없으면 "클래스명_인덱스" 형식을 사용합니다.
        /// </summary>
        /// <param name="so">대상 ScriptableObject</param>
        /// <param name="index">배열 인덱스 (폴백용)</param>
        /// <returns>에셋 파일 이름</returns>
        private string GetAssetName(ScriptableObject so, int index)
        {
            // id 필드 우선, 없으면 name 필드 사용
            var nameField = so.GetType().GetField("id") ?? so.GetType().GetField("name");
            if (nameField != null)
            {
                var value = nameField.GetValue(so);
                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                    return value.ToString();
            }
            return $"{_soClassName}_{index}";
        }

        /// <summary>
        /// 지정된 경로의 디렉토리가 없으면 생성합니다.
        /// 중첩된 경로도 순차적으로 생성합니다.
        /// </summary>
        /// <param name="path">생성할 디렉토리 경로</param>
        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];

                // 경로의 각 폴더를 순차적으로 생성
                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
        }

        /// <summary>
        /// JSON 문자열을 가독성 좋게 포맷합니다.
        /// 들여쓰기와 줄바꿈을 적용합니다.
        /// </summary>
        /// <param name="json">원본 JSON 문자열</param>
        /// <returns>포맷된 JSON 문자열</returns>
        private string FormatJson(string json)
        {
            try
            {
                int indent = 0;
                bool inString = false;
                var result = new System.Text.StringBuilder();

                foreach (char c in json)
                {
                    // 문자열 내부 여부 추적 (따옴표 토글)
                    if (c == '"' && (result.Length == 0 || result[result.Length - 1] != '\\'))
                        inString = !inString;

                    if (!inString)
                    {
                        // 구조 문자에 따라 들여쓰기 조정
                        if (c == '{' || c == '[')
                        {
                            result.Append(c);
                            result.AppendLine();
                            indent++;
                            result.Append(new string(' ', indent * 2));
                        }
                        else if (c == '}' || c == ']')
                        {
                            result.AppendLine();
                            indent--;
                            result.Append(new string(' ', indent * 2));
                            result.Append(c);
                        }
                        else if (c == ',')
                        {
                            result.Append(c);
                            result.AppendLine();
                            result.Append(new string(' ', indent * 2));
                        }
                        else if (c == ':')
                        {
                            result.Append(": ");
                        }
                        else if (!char.IsWhiteSpace(c))
                        {
                            result.Append(c);
                        }
                    }
                    else
                    {
                        result.Append(c);
                    }
                }

                return result.ToString();
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// JSON 배열 파싱을 위한 래퍼 클래스.
        /// Unity JsonUtility는 최상위 배열을 직접 파싱할 수 없어서 필요합니다.
        /// </summary>
        [Serializable]
        private class JsonArrayWrapper
        {
            public List<object> items;
        }
    }
}
#endif
