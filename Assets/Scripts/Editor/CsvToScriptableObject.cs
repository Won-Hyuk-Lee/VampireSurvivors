#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VampireSurvivors.Editor
{
    /// <summary>
    /// CSV 파일을 ScriptableObject로 변환하는 에디터 윈도우.
    /// Unity 에디터 메뉴 Tools > Data Pipeline > CSV to ScriptableObject로 접근합니다.
    /// </summary>
    /// <remarks>
    /// 사용법:
    /// 1. CSV 파일을 드래그하여 CSV File 필드에 할당
    /// 2. ScriptableObject Class에 변환할 SO 클래스 이름 입력
    /// 3. "Convert to ScriptableObjects" 클릭 (CSV 각 행 → 개별 SO)
    ///    또는 "Generate SO Class from CSV" 클릭 (CSV 헤더 기반 SO 클래스 자동 생성)
    ///
    /// CSV 형식:
    /// - 첫 번째 행은 헤더 (SO 필드명과 일치해야 함)
    /// - 구분자: 쉼표(,) 기본, 설정 가능
    /// - Vector2: (x;y) 형식
    /// - Vector3: (x;y;z) 형식
    /// </remarks>
    public class CsvToScriptableObject : EditorWindow
    {
        /// <summary>변환할 CSV 파일 (TextAsset)</summary>
        private TextAsset _csvFile;

        /// <summary>ScriptableObject 저장 경로</summary>
        private string _outputPath = "Assets/ScriptableObjects";

        /// <summary>대상 ScriptableObject 클래스 이름</summary>
        private string _soClassName = "";

        /// <summary>CSV 구분자 문자</summary>
        private char _delimiter = ',';

        /// <summary>첫 번째 행이 헤더인지 여부</summary>
        private bool _hasHeader = true;

        /// <summary>테이블 미리보기 스크롤 위치</summary>
        private Vector2 _scrollPosition;

        /// <summary>CSV 미리보기 텍스트</summary>
        private string _previewText = "";

        /// <summary>파싱된 CSV 데이터 (헤더 제외)</summary>
        private List<string[]> _parsedData = new List<string[]>();

        /// <summary>CSV 헤더 배열</summary>
        private string[] _headers;

        /// <summary>
        /// 에디터 메뉴에 윈도우 항목을 추가합니다.
        /// Tools > Data Pipeline > CSV to ScriptableObject
        /// </summary>
        [MenuItem("Tools/Data Pipeline/CSV to ScriptableObject")]
        public static void ShowWindow()
        {
            var window = GetWindow<CsvToScriptableObject>("CSV to SO");
            window.minSize = new Vector2(450, 400);
        }

        /// <summary>
        /// 에디터 윈도우 GUI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("CSV to ScriptableObject Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // CSV 파일 필드 - 파일 변경 시 파싱 수행
            EditorGUI.BeginChangeCheck();
            _csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", _csvFile, typeof(TextAsset), false);
            if (EditorGUI.EndChangeCheck() && _csvFile != null)
            {
                ParseCsv();
            }

            EditorGUILayout.Space(5);
            _soClassName = EditorGUILayout.TextField("ScriptableObject Class", _soClassName);
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            // 구분자 및 헤더 설정
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Delimiter", GUILayout.Width(80));
            string delimiterStr = EditorGUILayout.TextField(_delimiter.ToString(), GUILayout.Width(30));
            if (!string.IsNullOrEmpty(delimiterStr))
                _delimiter = delimiterStr[0];
            _hasHeader = EditorGUILayout.ToggleLeft("First Row is Header", _hasHeader);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // CSV 테이블 미리보기
            if (_csvFile != null && _parsedData.Count > 0)
            {
                EditorGUILayout.LabelField($"CSV Preview ({_parsedData.Count} rows):", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(180));

                // 헤더 표시
                if (_headers != null && _headers.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    foreach (var header in _headers)
                    {
                        EditorGUILayout.LabelField(header, EditorStyles.boldLabel, GUILayout.Width(100));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }

                // 데이터 행 표시 (최대 10개)
                int displayCount = Mathf.Min(_parsedData.Count, 10);
                for (int i = 0; i < displayCount; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    foreach (var cell in _parsedData[i])
                    {
                        EditorGUILayout.LabelField(cell, GUILayout.Width(100));
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (_parsedData.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {_parsedData.Count - 10} more rows");
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            // 변환 버튼
            EditorGUI.BeginDisabledGroup(_csvFile == null || string.IsNullOrEmpty(_soClassName));
            if (GUILayout.Button("Convert to ScriptableObjects", GUILayout.Height(30)))
            {
                ConvertCsvToSOs();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            // SO 클래스 자동 생성 버튼
            if (GUILayout.Button("Generate SO Class from CSV", GUILayout.Height(25)))
            {
                GenerateSOClass();
            }
        }

        /// <summary>
        /// CSV 파일을 파싱하여 헤더와 데이터를 추출합니다.
        /// </summary>
        private void ParseCsv()
        {
            _parsedData.Clear();
            _headers = null;

            if (_csvFile == null) return;

            // 줄 단위로 분리
            string[] lines = _csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0) return;

            int startIndex = 0;
            if (_hasHeader)
            {
                // 첫 번째 행을 헤더로 사용
                _headers = ParseCsvLine(lines[0]);
                startIndex = 1;
            }

            // 나머지 행을 데이터로 파싱
            for (int i = startIndex; i < lines.Length; i++)
            {
                string[] values = ParseCsvLine(lines[i]);
                _parsedData.Add(values);
            }
        }

        /// <summary>
        /// CSV 한 줄을 파싱하여 셀 배열로 반환합니다.
        /// 따옴표로 감싸진 셀 내 구분자를 올바르게 처리합니다.
        /// </summary>
        /// <param name="line">CSV 한 줄</param>
        /// <returns>셀 값 배열</returns>
        private string[] ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;  // 따옴표 내부 여부
            string currentValue = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // 따옴표 토글
                    inQuotes = !inQuotes;
                }
                else if (c == _delimiter && !inQuotes)
                {
                    // 따옴표 밖의 구분자 = 셀 구분
                    values.Add(currentValue.Trim());
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }

            // 마지막 셀 추가
            values.Add(currentValue.Trim());
            return values.ToArray();
        }

        /// <summary>
        /// CSV 데이터를 ScriptableObject들로 변환합니다.
        /// 각 행이 하나의 SO가 됩니다.
        /// </summary>
        private void ConvertCsvToSOs()
        {
            if (_csvFile == null || string.IsNullOrEmpty(_soClassName))
            {
                EditorUtility.DisplayDialog("Error", "Please select a CSV file and specify SO class name.", "OK");
                return;
            }

            if (_headers == null || _headers.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "CSV must have headers.", "OK");
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
                EnsureDirectoryExists(_outputPath);

                int count = 0;
                foreach (var row in _parsedData)
                {
                    // SO 인스턴스 생성
                    ScriptableObject so = ScriptableObject.CreateInstance(soType);

                    // 각 열의 값을 해당 필드에 설정
                    for (int i = 0; i < _headers.Length && i < row.Length; i++)
                    {
                        string fieldName = _headers[i];
                        string value = row[i];

                        SetFieldValue(so, fieldName, value);
                    }

                    // 에셋으로 저장
                    string assetName = GetAssetName(so, row, count);
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
                EditorUtility.DisplayDialog("Error", $"Failed to convert: {e.Message}", "OK");
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 문자열 값을 SO 필드에 설정합니다.
        /// 필드 타입에 맞게 자동으로 변환합니다.
        /// </summary>
        /// <param name="so">대상 ScriptableObject</param>
        /// <param name="fieldName">필드 이름</param>
        /// <param name="value">설정할 문자열 값</param>
        private void SetFieldValue(ScriptableObject so, string fieldName, string value)
        {
            // public 및 private 필드 모두 검색
            FieldInfo field = so.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null) return;

            try
            {
                Type fieldType = field.FieldType;

                // 타입별 변환 처리
                if (fieldType == typeof(int))
                    field.SetValue(so, int.Parse(value));
                else if (fieldType == typeof(float))
                    field.SetValue(so, float.Parse(value));
                else if (fieldType == typeof(double))
                    field.SetValue(so, double.Parse(value));
                else if (fieldType == typeof(bool))
                    field.SetValue(so, bool.Parse(value));
                else if (fieldType == typeof(string))
                    field.SetValue(so, value);
                else if (fieldType.IsEnum)
                    field.SetValue(so, Enum.Parse(fieldType, value));
                else if (fieldType == typeof(Vector2))
                    field.SetValue(so, ParseVector2(value));
                else if (fieldType == typeof(Vector3))
                    field.SetValue(so, ParseVector3(value));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to set field '{fieldName}' with value '{value}': {e.Message}");
            }
        }

        /// <summary>
        /// "(x;y)" 형식의 문자열을 Vector2로 파싱합니다.
        /// </summary>
        /// <param name="value">파싱할 문자열</param>
        /// <returns>파싱된 Vector2</returns>
        private Vector2 ParseVector2(string value)
        {
            string[] parts = value.Trim('(', ')').Split(';');
            return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
        }

        /// <summary>
        /// "(x;y;z)" 형식의 문자열을 Vector3로 파싱합니다.
        /// </summary>
        /// <param name="value">파싱할 문자열</param>
        /// <returns>파싱된 Vector3</returns>
        private Vector3 ParseVector3(string value)
        {
            string[] parts = value.Trim('(', ')').Split(';');
            return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        }

        /// <summary>
        /// CSV 헤더를 기반으로 ScriptableObject 클래스 스크립트를 생성합니다.
        /// 데이터 첫 행의 값을 분석하여 타입을 추론합니다.
        /// </summary>
        private void GenerateSOClass()
        {
            if (_csvFile == null || _headers == null || _headers.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select a CSV file with headers.", "OK");
                return;
            }

            string className = string.IsNullOrEmpty(_soClassName) ? _csvFile.name + "SO" : _soClassName;

            // 클래스 코드 생성
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace VampireSurvivors.Data");
            sb.AppendLine("{");
            sb.AppendLine($"    [CreateAssetMenu(fileName = \"{className}\", menuName = \"VampireSurvivors/Data/{className}\")]");
            sb.AppendLine($"    public class {className} : ScriptableObject");
            sb.AppendLine("    {");

            // 각 헤더를 필드로 변환
            foreach (var header in _headers)
            {
                string fieldType = InferFieldType(header);
                sb.AppendLine($"        public {fieldType} {header};");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 스크립트 파일 저장
            string scriptPath = $"Assets/Scripts/Data/{className}.cs";
            EnsureDirectoryExists("Assets/Scripts/Data");
            File.WriteAllText(scriptPath, sb.ToString());
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Generated class at {scriptPath}", "OK");
        }

        /// <summary>
        /// 헤더에 해당하는 데이터 값을 분석하여 C# 타입을 추론합니다.
        /// </summary>
        /// <param name="header">헤더(열) 이름</param>
        /// <returns>추론된 C# 타입 이름</returns>
        private string InferFieldType(string header)
        {
            if (_parsedData.Count == 0) return "string";

            int headerIndex = Array.IndexOf(_headers, header);
            if (headerIndex < 0) return "string";

            // 첫 번째 데이터 행의 값으로 타입 추론
            string sampleValue = _parsedData[0].Length > headerIndex ? _parsedData[0][headerIndex] : "";

            if (int.TryParse(sampleValue, out _)) return "int";
            if (float.TryParse(sampleValue, out _)) return "float";
            if (bool.TryParse(sampleValue, out _)) return "bool";

            return "string";
        }

        /// <summary>
        /// SO의 id 또는 name 열 값으로 에셋 이름을 결정합니다.
        /// </summary>
        /// <param name="so">대상 ScriptableObject</param>
        /// <param name="row">CSV 행 데이터</param>
        /// <param name="index">행 인덱스 (폴백용)</param>
        /// <returns>에셋 파일 이름</returns>
        private string GetAssetName(ScriptableObject so, string[] row, int index)
        {
            if (_headers != null && _headers.Length > 0)
            {
                // id 또는 name 열 찾기
                int idIndex = Array.FindIndex(_headers, h => h.ToLower() == "id" || h.ToLower() == "name");
                if (idIndex >= 0 && row.Length > idIndex && !string.IsNullOrEmpty(row[idIndex]))
                {
                    return row[idIndex];
                }
            }
            return $"{_soClassName}_{index}";
        }

        /// <summary>
        /// 클래스 이름으로 ScriptableObject 타입을 검색합니다.
        /// </summary>
        /// <param name="className">검색할 클래스 이름</param>
        /// <returns>찾은 Type 또는 null</returns>
        private Type GetScriptableObjectType(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(className);
                if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
                    return type;

                type = assembly.GetType($"VampireSurvivors.Data.{className}");
                if (type != null && type.IsSubclassOf(typeof(ScriptableObject)))
                    return type;
            }
            return null;
        }

        /// <summary>
        /// 지정된 경로의 디렉토리가 없으면 생성합니다.
        /// </summary>
        /// <param name="path">생성할 디렉토리 경로</param>
        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];

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
    }
}
#endif
