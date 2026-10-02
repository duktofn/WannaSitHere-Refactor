using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Game.Data.Levels;

namespace Game.Editor.Difficulty
{
    internal sealed class DifficultyAnalyzerWindow : EditorWindow
    {
        [SerializeField] private LevelDataSO _level;
        [SerializeField] private DifficultyAnalysisSettings _settings = new();
        [SerializeField] private bool _showSettings;
        [SerializeField] private int _tab;
        private DifficultyAnalysisJob _job;
        private DifficultyAnalysisResult _result;
        private Vector2 _scrollPosition;
        private string _report;
        private bool _stale;
        private double _nextFingerprintCheck;
        private int _selectedPerson;
        private GUIStyle _titleStyle, _scoreStyle, _wrapStyle, _cellStyle, _reportStyle;
        private static readonly Color ConditionColor = new(0.25f, 0.65f, 0.90f);
        private static readonly Color DependencyColor = new(0.70f, 0.50f, 0.90f);
        private static readonly Color MoveColor = new(0.95f, 0.65f, 0.25f);
        private const float MaxBoardCellSize = 36f;
        private const float MaxBoardWidth = 360f;
        private const float MaxBoardHeight = 240f;

        [MenuItem("Tools/Wanna Sit Here/Difficulty Analyzer")]
        private static void Open() => GetWindow<DifficultyAnalyzerWindow>("Difficulty Analyzer");

        private void OnEnable()
        {
            _settings ??= new DifficultyAnalysisSettings();
            minSize = new Vector2(540, 420);
            EditorApplication.update += UpdateAnalysis;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateAnalysis;
            _job?.Cancel();
            _job = null;
        }

        private void EnsureStyles()
        {
            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 };
            _scoreStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 32, alignment = TextAnchor.MiddleLeft };
            _wrapStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true };
            _cellStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _cellStyle.normal.textColor = Color.white;
            _reportStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        }

        private void OnGUI()
        {
            EnsureStyles();
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("LEVEL DIFFICULTY", _titleStyle, GUILayout.Height(28));
            EditorGUILayout.LabelField("Condition 50%  /  Dependency 30%  /  Move 20%", EditorStyles.miniLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (new EditorGUI.DisabledScope(_job != null && _job.IsRunning))
            {
                _level = (LevelDataSO)EditorGUILayout.ObjectField("Level", _level, typeof(LevelDataSO), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Dùng Level đang chọn"))
                    {
                        if (Selection.activeObject is LevelDataSO selected) _level = selected;
                    }
                    using (new EditorGUI.DisabledScope(_level == null))
                        if (GUILayout.Button("Tính độ khó", GUILayout.Height(26))) StartAnalysis();
                }
                _showSettings = EditorGUILayout.Foldout(_showSettings, "Thiết lập bộ giải", true);
                if (_showSettings)
                {
                    _settings.UseSolver = EditorGUILayout.Toggle("Chạy bộ giải Move/Swap", _settings.UseSolver);
                    _settings.MaxNodes = EditorGUILayout.IntField("Ngân sách node", _settings.MaxNodes);
                    EditorGUILayout.LabelField("Trọng số cố định theo mô hình V1. Kề 4 hướng, không chéo.", _wrapStyle);
                }
            }
            if (_job != null && _job.IsRunning)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 22), _job.Progress,
                    $"Ngân sách node: {_job.Result.Nodes:N0} / {_settings.MaxNodes:N0}");
                EditorGUILayout.LabelField($"Cách xếp đã tìm: {_job.Result.SolutionsFound:N0} | Best moves: {_job.Result.BestFoundMoves}");
                if (GUILayout.Button("Hủy phân tích")) { _job.Cancel(); PublishResult(); }
            }
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (_result != null) DrawResult();
            else if (_job == null) EditorGUILayout.HelpBox("Chọn Level để xem nguyên nhân tạo độ khó, Dependency và số Move tối thiểu.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void StartAnalysis()
        {
            _job = new DifficultyAnalysisJob(_level, _settings);
            _result = null;
            _report = null;
            _selectedPerson = 0;
            if (!_job.IsRunning) PublishResult();
        }

        private void UpdateAnalysis()
        {
            if (_job != null && _job.IsRunning)
            {
                _job.Tick();
                if (!_job.IsRunning) PublishResult();
                Repaint();
            }
            if (_result != null && EditorApplication.timeSinceStartup >= _nextFingerprintCheck)
            {
                _nextFingerprintCheck = EditorApplication.timeSinceStartup + 0.5;
                bool stale = _result.InputFingerprint != DifficultyAnalysisJob.ComputeInputFingerprint(_level, _settings);
                if (_stale != stale) { _stale = stale; Repaint(); }
            }
        }

        private void PublishResult()
        {
            _result = _job.Result;
            _report = DifficultyAnalysisReport.Text(_result);
            _stale = false;
            _job = null;
        }

        private void DrawResult()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(_result.LevelName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_result.HasScore ? $"{_result.DifficultyScore:F1} / 100" : "Chưa có điểm tổng", _scoreStyle, GUILayout.Height(42));
                EditorGUILayout.LabelField(_result.Status.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(_result.Message ?? "", _result.Valid ? MessageType.Info : MessageType.Error);
                if (_stale) EditorGUILayout.HelpBox("Dữ liệu đã thay đổi. Tính lại để xem và xuất kết quả mới.", MessageType.Warning);
            }
            _tab = GUILayout.Toolbar(_tab, new[] { "Tổng quan", "Person / Condition", "Bàn / Dependency", "Báo cáo" });
            EditorGUILayout.Space(6);
            if (_tab == 0) DrawOverview();
            else if (_tab == 1) DrawPeople();
            else if (_tab == 2) { DrawBoard("MainGrid"); DrawBoard("WaitGrid"); DrawDependencies(); }
            else DrawReport();
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField($"Person: {_result.N}  |  Main seats: {_result.S}  |  Wait people: {_result.WaitPeople}  |  LevelMove: {_result.M}", _wrapStyle);
            DrawMetric("Condition C", _result.ConditionComplexity, ConditionColor, $"50% → {_result.ConditionContribution:F2} điểm");
            DrawMetric("Độ chặt T", _result.Tightness, ConditionColor, "80% của Condition");
            DrawMetric("Lấp đầy O = P/S", _result.Occupancy, ConditionColor, "20% của Condition");
            EditorGUILayout.Space(6);
            DrawMetric("Dependency Dp", _result.DependencyComplexity, DependencyColor, $"30% → {_result.DependencyContribution:F2} điểm");
            DrawMetric("Like density L", _result.LikeDensity, DependencyColor, $"25% | {_result.LikePersonConditions} / (2 × {_result.N})");
            DrawMetric("Độ sâu H", _result.DependencyDepth, DependencyColor, $"30% | tầng lớn nhất {_result.MaxDependencyDepth}");
            DrawMetric("Chu trình Y", _result.CycleRatio, DependencyColor, $"45% | {_result.CyclePeople} / {_result.N} Person");
            EditorGUILayout.Space(6);
            if (_result.HasExactMoves)
            {
                DrawMetric("Move M", _result.MovePressure, MoveColor, $"20% → {_result.MoveContribution:F2} điểm");
                DrawMetric("Budget Bt = M*/B", _result.BudgetTightness, MoveColor, "70% của Move");
                DrawMetric("Sắp xếp lại R", _result.Rearrangement, MoveColor, "30% của Move");
                EditorGUILayout.LabelField($"M* = {_result.MinMoves} | Move dư = {_result.Slack}", EditorStyles.boldLabel);
            }
            else EditorGUILayout.HelpBox("Move: chưa có M* chính xác. Chạy bộ giải hoặc tăng ngân sách node.", MessageType.Warning);
            foreach (string problem in _result.Problems) EditorGUILayout.HelpBox(problem, MessageType.Error);
            foreach (string warning in _result.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            EditorGUILayout.LabelField($"{_result.Nodes:N0} nodes | {_result.SolutionsFound:N0} cách xếp | {_result.ElapsedSeconds:F3}s | Duyệt hết: {_result.SolverExact}", _wrapStyle);
        }

        private void DrawMetric(string label, double value, Color color, string detail)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 50);
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 70, 18), label, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + rect.width - 70, rect.y, 70, 18), $"{value:F3}", EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x, rect.y + 18, rect.width, 18), detail, EditorStyles.miniLabel);
            Rect bar = new(rect.x, rect.y + 38, rect.width, 6);
            EditorGUI.DrawRect(bar, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.8f, 0.8f, 0.8f));
            bar.width *= (float)Math.Max(0, Math.Min(1, value));
            EditorGUI.DrawRect(bar, color);
        }

        private void DrawPeople()
        {
            foreach (var person in _result.People)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"#{person.Id} {person.Name} · {person.Trait}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{person.Grid} ({person.X},{person.Y}) | Ri={person.ExpectedSeatRatio:F4} | Ti={person.Tightness:F4}", _wrapStyle);
                    EditorGUILayout.LabelField($"Ban đầu: {(person.Grid == "WaitGrid" ? "Wait" : person.InitiallyHappy ? "Happy" : "Angry")} | Tầng: {LayerLabel(person)}", _wrapStyle);
                    foreach (var c in _result.Conditions)
                        if (c.PersonId == person.Id)
                        {
                            EditorGUILayout.LabelField($"{c.Label} | q trung bình={c.Probability:F4} | N_target={c.MatchingOthers}", _wrapStyle);
                            if (!string.IsNullOrEmpty(c.Description)) EditorGUILayout.LabelField(c.Description, EditorStyles.miniLabel);
                        }
                    if (GUILayout.Button("Xem Q(x) trên bàn")) { _selectedPerson = person.Id; _tab = 2; }
                }
            }
        }

        private void DrawBoard(string grid)
        {
            int width = 0, height = 0;
            foreach (var row in _result.Counts)
            {
                if (row.Name == grid + ".Width") width = row.Count;
                if (row.Name == grid + ".Height") height = row.Count;
            }
            if (width == 0 || height == 0) return;
            EditorGUILayout.LabelField(grid, EditorStyles.boldLabel);
            if (grid == "MainGrid" && _result.People.Count > 0)
            {
                string[] choices = new string[_result.People.Count];
                for (int i = 0; i < choices.Length; i++) choices[i] = $"#{i} {_result.People[i].Name}";
                _selectedPerson = EditorGUILayout.Popup("Q(x) của Person", Math.Clamp(_selectedPerson, 0, choices.Length - 1), choices);
            }
            float availableWidth = Math.Max(1f, position.width - 44f);
            float cellSize = Math.Min(MaxBoardCellSize,
                Math.Min(availableWidth / width, Math.Min(MaxBoardWidth / width, MaxBoardHeight / height)));
            Rect area = GUILayoutUtility.GetRect(cellSize * width, cellSize * height);
            foreach (var cell in _result.Cells)
            {
                if (cell.Grid != grid) continue;
                Rect rect = new(area.x + cell.X * cellSize, area.y + (height - 1 - cell.Y) * cellSize, cellSize - 2, cellSize - 2);
                string text = cell.Type == "Food" ? cell.Food : cell.Type == "Block" ? "Block" : cell.Type == "Empty" ? "·" : "Seat";
                Color color = cell.Type == "Food" ? new Color(0.42f, 0.29f, 0.12f) : cell.Type == "Seat" ? new Color(0.13f, 0.27f, 0.37f) : new Color(0.25f, 0.25f, 0.25f);
                string tooltip = $"{grid} ({cell.X},{cell.Y}) {cell.Type} {cell.Food}\nPerson: {cell.Person}\n{cell.Neighbors}";
                if (grid == "MainGrid" && cell.Type == "Seat")
                    foreach (var probability in _result.SeatProbabilities)
                        if (probability.PersonId == _selectedPerson && probability.X == cell.X && probability.Y == cell.Y)
                        {
                            color = Color.Lerp(new Color(0.6f, 0.25f, 0.25f), new Color(0.2f, 0.65f, 0.4f), (float)probability.JointProbability);
                            text = $"Q={probability.JointProbability:F2}";
                            tooltip += $"\nK={probability.AdjacentSeats}; Q={probability.JointProbability:G6}";
                            for (int i = 0; i < probability.ConditionProbabilities.Count; i++) tooltip += $"\nq[{i}]={probability.ConditionProbabilities[i]:G6}";
                            break;
                        }
                if (!string.IsNullOrEmpty(cell.Person)) text += "\n" + cell.Person;
                EditorGUI.DrawRect(rect, color);
                GUI.Label(rect, new GUIContent(text, tooltip), _cellStyle);
            }
            EditorGUILayout.LabelField("Tọa độ gốc 0, Y hướng lên. Hover để xem q từng Condition và các ô kề.", EditorStyles.miniLabel);
        }

        private static string LayerLabel(DifficultyPersonRow p) => p.DependencyLayer >= 0 ? p.DependencyLayer.ToString() :
            p.InDependencyCycle ? "Trong chu trình" : "Phụ thuộc chu trình / chưa phân tầng";

        private void DrawDependencies()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Dependency: VÀ giữa TargetTrait, HOẶC giữa Person cùng trait", _wrapStyle);
            foreach (var p in _result.People)
            {
                EditorGUILayout.LabelField($"#{p.Id} {p.Name}: {LayerLabel(p)}", EditorStyles.boldLabel);
                foreach (var c in _result.Conditions)
                    if (c.PersonId == p.Id && c.Label.StartsWith("Like Person ", StringComparison.Ordinal))
                    {
                        string trait = c.Label.Substring("Like Person ".Length);
                        StringBuilder providers = new();
                        foreach (var q in _result.People)
                            if (q.Id != p.Id && q.Trait == trait)
                            {
                                if (providers.Length > 0) providers.Append(" HOẶC ");
                                providers.Append('#').Append(q.Id).Append(' ').Append(q.Name);
                            }
                        EditorGUILayout.LabelField($"→ {trait}: {providers}", _wrapStyle);
                    }
            }
            EditorGUILayout.LabelField("Cách xếp có Move thấp nhất đã tìm", EditorStyles.boldLabel);
            foreach (string assignment in _result.BestAssignment) EditorGUILayout.LabelField(assignment, _wrapStyle);
        }

        private void DrawReport()
        {
            _stale = _result.InputFingerprint != DifficultyAnalysisJob.ComputeInputFingerprint(_level, _settings);
            using (new EditorGUI.DisabledScope(_stale || _result.Status == DifficultyAnalysisStatus.Cancelled))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy")) EditorGUIUtility.systemCopyBuffer = _report;
                if (GUILayout.Button("TXT")) Export("txt", _report);
                if (GUILayout.Button("JSON")) Export("json", JsonUtility.ToJson(_result, true));
                if (GUILayout.Button("CSV")) Export("csv", DifficultyAnalysisReport.Csv(_result));
            }
            float height = _reportStyle.CalcHeight(new GUIContent(_report), Math.Max(200, position.width - 35));
            EditorGUILayout.SelectableLabel(_report, _reportStyle, GUILayout.Height(height));
        }

        private void Export(string extension, string content)
        {
            string path = EditorUtility.SaveFilePanel("Xuất báo cáo độ khó", "", _result.LevelName + "-difficulty", extension);
            if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, content, new UTF8Encoding(true));
        }
    }

    public static class DifficultyAnalysisReport
    {
        private static string Number(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        public static string Text(DifficultyAnalysisResult r)
        {
            StringBuilder b = new();
            b.AppendLine(r.LevelName + " — " + r.ModelId + " v" + r.ModelVersion);
            b.AppendLine($"Status={r.Status}; HasScore={r.HasScore}; Valid={r.Valid}; {r.Message}");
            b.AppendLine($"P={r.N}, S={r.S}, WaitPeople={r.WaitPeople}, WaitSeats={r.W}, LevelMove={r.M}");
            b.AppendLine("C=0.80T+0.20O; Dp=0.25L+0.30H+0.45Y; M=0.70Bt+0.30R");
            b.AppendLine($"T={Number(r.Tightness)}, O={Number(r.Occupancy)}, L={Number(r.LikeDensity)}, H={Number(r.DependencyDepth)}, Y={Number(r.CycleRatio)}");
            b.AppendLine($"C={Number(r.ConditionComplexity)}, Dp={Number(r.DependencyComplexity)}; dmax={r.MaxDependencyDepth}; cyclePeople={r.CyclePeople}; blockedPeople={r.BlockedPeople}");
            b.AppendLine($"Solver ran={r.SolverRan}, exhaustive={r.SolverExact}, nodes={r.Nodes}, solutionsFound={r.SolutionsFound}, bestFoundMoves={r.BestFoundMoves}");
            b.AppendLine($"M*={(r.HasExactMoves ? r.MinMoves.ToString() : "chưa xác định")}; {r.MoveSource}");
            if (r.HasExactMoves) b.AppendLine($"Bt={Number(r.BudgetTightness)}, R={Number(r.Rearrangement)}, M={Number(r.MovePressure)}, slack={r.Slack}");
            b.AppendLine(r.HasScore ? $"Difficulty=100*(0.50C+0.30Dp+0.20M)={Number(r.DifficultyScore)}" : "Không có điểm tổng.");
            foreach (string problem in r.Problems) b.AppendLine("LỖI: " + problem);
            foreach (string warning in r.Warnings) b.AppendLine("CHÚ Ý: " + warning);
            b.AppendLine("\nPERSON / CONDITION / SEAT");
            foreach (var p in r.People)
            {
                b.AppendLine($"#{p.Id} {p.Name} ({p.Trait}), {p.Grid} ({p.X},{p.Y}), Ri={Number(p.ExpectedSeatRatio)}, Ti={Number(p.Tightness)}, layer={p.DependencyLayer}, cycle={p.InDependencyCycle}");
                foreach (var c in r.Conditions)
                    if (c.PersonId == p.Id) b.AppendLine($"  [{c.Index}] {c.Label}: {c.Description}; N_target={c.MatchingOthers}; mean q={Number(c.Probability)}; initiallySatisfied={c.InitiallySatisfied}");
                foreach (var q in r.SeatProbabilities)
                    if (q.PersonId == p.Id)
                    {
                        b.Append($"  Seat ({q.X},{q.Y}), K={q.AdjacentSeats}, Q={Number(q.JointProbability)}, q=[");
                        for (int i = 0; i < q.ConditionProbabilities.Count; i++) { if (i > 0) b.Append(", "); b.Append(Number(q.ConditionProbabilities[i])); }
                        b.AppendLine("]");
                    }
            }
            b.AppendLine("\nCELLS / COUNTS");
            foreach (var c in r.Cells) b.AppendLine($"{c.Grid} ({c.X},{c.Y}) {c.Type} {c.Food} | Person={c.Person} | {c.Neighbors}");
            foreach (var c in r.Counts) b.AppendLine(c.Name + "=" + c.Count);
            b.AppendLine("\nCách xếp tốt nhất (không phải chuỗi thao tác):");
            foreach (string assignment in r.BestAssignment) b.AppendLine(assignment);
            b.AppendLine($"UseSolver={r.Settings.UseSolver}; MaxNodes={r.Settings.MaxNodes}; elapsed={Number(r.ElapsedSeconds)}s");
            b.AppendLine("Fingerprint=" + r.InputFingerprint);
            return b.ToString();
        }

        // Flatten every serializable field, including indexed cell/person/condition details.
        public static string Csv(DifficultyAnalysisResult r)
        {
            StringBuilder b = new("key,value\n");
            AppendCsv(b, "", r);
            return b.ToString();
        }

        private static void AppendCsv(StringBuilder b, string key, object value)
        {
            if (value is System.Collections.IList list)
            {
                for (int i = 0; i < list.Count; i++) AppendCsv(b, key + "[" + i + "]", list[i]);
                return;
            }
            if (value != null && !value.GetType().IsPrimitive && !value.GetType().IsEnum && !(value is string))
            {
                foreach (var field in value.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    AppendCsv(b, key.Length == 0 ? field.Name : key + "." + field.Name, field.GetValue(value));
                return;
            }
            string text = value is double number ? Number(number) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            b.Append('"').Append(key.Replace("\"", "\"\"")).Append("\",\"").Append(text.Replace("\"", "\"\"")).AppendLine("\"");
        }
    }
}
