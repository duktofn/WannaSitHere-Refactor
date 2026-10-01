using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.People;
using Game.Data.Board;
using Game.Data.Levels;

namespace Game.Editor.Difficulty
{
    public sealed class DifficultyAnalysisJob
    {
        public const string CurrentModelId = "ConditionDependencyMove";
        public const int CurrentModelVersion = 2;
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly List<CellRuntimeData> _seats = new();
        private readonly List<PersonRuntimeData> _people = new();
        private readonly List<int> _sources = new();
        private readonly Dictionary<Vector2Int, int> _position2Seat = new();
        private readonly List<int[]> _neighbors = new();
        private readonly List<HashSet<Food>> _foods = new();
        private List<int>[] _checkAt;
        private bool[,] _allowed;
        private int[] _assignment, _initial, _destination;
        private bool[] _used;
        private IEnumerator<int> _search;
        public DifficultyAnalysisResult Result { get; }
        public bool IsRunning => Result.Status == DifficultyAnalysisStatus.Running;
        public string PhaseName => "Duyệt cách xếp cuối";
        // Fraction of node budget consumed, not fraction of the search completed.
        public float Progress => (float)Math.Min(1, (double)Result.Nodes / Result.Settings.MaxNodes);

        public DifficultyAnalysisJob(LevelDataSO level, DifficultyAnalysisSettings settings = null)
        {
            settings ??= new DifficultyAnalysisSettings();
            Result = new DifficultyAnalysisResult
            {
                Status = DifficultyAnalysisStatus.Running, Valid = true,
                ModelId = CurrentModelId, ModelVersion = CurrentModelVersion,
                Settings = JsonUtility.FromJson<DifficultyAnalysisSettings>(JsonUtility.ToJson(settings)),
                LevelName = level == null ? "" : level.name, M = level == null ? 0 : level.levelMove
            };
            if (!ValidSettings(Result.Settings))
            {
                Reject("Ngân sách node phải lớn hơn 0.");
                return;
            }
            if (level == null) { Reject("Chọn LevelDataSO."); return; }
            Result.InputFingerprint = ComputeInputFingerprint(level, Result.Settings);
            List<string> errors = new();
            if (!level.Validate(errors))
            {
                Result.Problems.AddRange(errors);
                Reject("Dữ liệu Level không hợp lệ.");
                return;
            }
            level.TryToRuntimeData(out var runtime, out _);
            ReadGrid(runtime.MainGrid, GridId.MainGrid);
            ReadGrid(runtime.WaitGrid, GridId.WaitGrid);
            // Preserve personConfigs order so report IDs match the level inspector.
            var people = _people.ToArray();
            var sources = _sources.ToArray();
            var rows = Result.People.ToArray();
            _people.Clear();
            _sources.Clear();
            Result.People.Clear();
            if (level.personConfigs != null)
                foreach (var config in level.personConfigs)
                    for (int i = 0; i < rows.Length; i++)
                        if (rows[i].Grid == config.gridId.ToString() && rows[i].X == config.position.x && rows[i].Y == config.position.y)
                        {
                            rows[i].Id = _people.Count;
                            _people.Add(people[i]);
                            _sources.Add(sources[i]);
                            Result.People.Add(rows[i]);
                            break;
                        }
            Result.S = _seats.Count;
            Result.N = _people.Count;
            Result.E = Result.S - Result.N;
            if (Result.N == 0 || Result.S == 0) { Reject("Level cần ít nhất một Person và một Seat MainGrid."); return; }
            _assignment = new int[Result.S];
            _initial = new int[Result.S];
            Array.Fill(_assignment, -1);
            Array.Fill(_initial, -1);
            for (int p = 0; p < _people.Count; p++)
                if (_sources[p] >= 0) _initial[_sources[p]] = p;
            BuildNeighbors(runtime.MainGrid);
            DescribePeople();
            CalculateConditions();
            CalculateDependencies();
            if (Result.N > Result.S) Result.Problems.Add("P > S: số Person lớn hơn Seat MainGrid.");
            if (Result.M < 0) Result.Problems.Add("LevelMove không được âm.");
            if (Result.Problems.Count > 0) { Complete(false); return; }
            if (!settings.UseSolver) { Complete(false); return; }
            _allowed = new bool[Result.N, Result.S];
            for (int p = 0; p < Result.N; p++)
                for (int s = 0; s < Result.S; s++)
                {
                    _allowed[p, s] = true;
                    foreach (var c in _people[p].Conditions)
                        if (c.Target == ConditionTarget.Food && !ConditionOK(c, s, _initial))
                            _allowed[p, s] = false;
                }
            _checkAt = new List<int>[Result.S];
            for (int s = 0; s < Result.S; s++) _checkAt[s] = new List<int>();
            for (int s = 0; s < Result.S; s++)
            {
                int last = s;
                foreach (int neighbor in _neighbors[s]) last = Math.Max(last, neighbor);
                _checkAt[last].Add(s);
            }
            _used = new bool[Result.N];
            _destination = new int[Result.N];
            Result.SolverRan = true;
            _search = Search(0, 0).GetEnumerator();
        }

        private static bool ValidSettings(DifficultyAnalysisSettings s) => s.MaxNodes > 0;

        private void ReadGrid(Grid<CellRuntimeData> grid, GridId id)
        {
            if (grid == null) return;
            AddCount(id + ".Width", grid.GridSize.x);
            AddCount(id + ".Height", grid.GridSize.y);
            Dictionary<string, int> counts = new()
            {
                { "Seat", 0 }, { "Food", 0 }, { "Block", 0 }, { "Empty", 0 }
            };
            for (int y = 0; y < grid.GridSize.y; y++)
                for (int x = 0; x < grid.GridSize.x; x++)
                {
                    var cell = grid.Get(x, y);
                    string type = cell == null ? "Empty" : cell.Type.ToString();
                    counts.TryGetValue(type, out int count);
                    counts[type] = count + 1;
                    Result.Cells.Add(new DifficultyCellRow
                    {
                        Grid = id.ToString(), X = x, Y = y, Type = type,
                        Food = cell != null && cell.Type == CellType.Food ? cell.Food.ToString() : "",
                        Person = cell?.CurrentPerson?.PersonName ?? ""
                    });
                    if (cell == null) continue;
                    if (cell.Type == CellType.Food)
                    {
                        string key = "Food." + cell.Food;
                        counts.TryGetValue(key, out count);
                        counts[key] = count + 1;
                    }
                    if (cell.Type == CellType.Seat)
                    {
                        if (id == GridId.MainGrid)
                        {
                            _position2Seat.Add(cell.Index, _seats.Count);
                            _seats.Add(cell);
                        }
                        else Result.W++;
                    }
                    if (cell.CurrentPerson == null) continue;
                    int source = id == GridId.MainGrid ? _seats.Count - 1 : -1;
                    _sources.Add(source);
                    _people.Add(cell.CurrentPerson);
                    Result.People.Add(new DifficultyPersonRow
                    {
                        Id = _people.Count - 1, Name = cell.CurrentPerson.PersonName,
                        Trait = cell.CurrentPerson.Trait.ToString(), Grid = id.ToString(), X = x, Y = y,
                        ConditionCount = cell.CurrentPerson.Conditions.Count
                    });
                    if (id == GridId.MainGrid) Result.MainPeople++; else Result.WaitPeople++;
                    string traitKey = "Trait." + cell.CurrentPerson.Trait;
                    counts.TryGetValue(traitKey, out count);
                    counts[traitKey] = count + 1;
                }
            foreach (var pair in counts) AddCount(id + "." + pair.Key, pair.Value);
        }

        private void AddCount(string name, int count) => Result.Counts.Add(new DifficultyCountRow { Name = name, Count = count });

        private void BuildNeighbors(Grid<CellRuntimeData> grid)
        {
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            foreach (var seat in _seats)
            {
                List<int> seats = new();
                HashSet<Food> foods = new();
                int foodCount = 0;
                List<string> labels = new();
                foreach (var dir in dirs)
                {
                    Vector2Int pos = seat.Index + dir;
                    var neighbor = grid.Get(pos.x, pos.y);
                    if (neighbor == null) continue;
                    if (_position2Seat.TryGetValue(pos, out int index)) seats.Add(index);
                    if (neighbor.Type == CellType.Food) { foods.Add(neighbor.Food); foodCount++; }
                    labels.Add($"({pos.x},{pos.y}) {neighbor.Type}" + (neighbor.Type == CellType.Food ? " " + neighbor.Food : ""));
                }
                _neighbors.Add(seats.ToArray());
                _foods.Add(foods);
                Result.MeanSeatDegree += seats.Count;
                foreach (var row in Result.Cells)
                    if (row.Grid == "MainGrid" && row.X == seat.Index.x && row.Y == seat.Index.y)
                    {
                        row.AdjacentSeats = seats.Count;
                        row.AdjacentFoods = foodCount;
                        row.Neighbors = string.Join("; ", labels);
                        break;
                    }
            }
            Result.MeanSeatDegree /= Result.S;
            AddCount("MainGrid.InitialEmptySeats", Result.S - Result.MainPeople);
            AddCount("WaitGrid.InitialEmptySeats", Result.W - Result.WaitPeople);
        }

        private static bool Ignored(ConditionRuntimeData c) => c.IsCanSitAnywhere;

        private bool ConditionOK(ConditionRuntimeData c, int seat, int[] occupants)
        {
            if (Ignored(c)) return true;
            bool has = false;
            if (c.Target == ConditionTarget.Food) has = c.FoodTarget == Food.Any ? _foods[seat].Count > 0 : _foods[seat].Contains(c.FoodTarget);
            else
                foreach (int neighbor in _neighbors[seat])
                {
                    int p = occupants[neighbor];
                    if (p >= 0 && _people[p].Trait == c.TargetTrait) { has = true; break; }
                }
            return has == (c.Type == ConditionType.Like);
        }

        private void DescribePeople()
        {
            for (int p = 0; p < Result.N; p++)
            {
                if (_sources[p] < 0) continue;
                var row = Result.People[p];
                foreach (var c in _people[p].Conditions)
                    if (ConditionOK(c, _sources[p], _initial)) row.SatisfiedConditions++;
                row.InitiallyHappy = row.SatisfiedConditions == row.ConditionCount;
                if (!row.InitiallyHappy) Result.AngryMain++;
            }
            AddCount("MainGrid.InitialHappyPeople", Result.MainPeople - Result.AngryMain);
            AddCount("MainGrid.InitialAngryPeople", Result.AngryMain);
        }

        private void CalculateConditions()
        {
            for (int p = 0; p < Result.N; p++)
            {
                var person = _people[p];
                List<DifficultyConditionRow> rows = new();
                foreach (var c in person.Conditions)
                {
                    var row = new DifficultyConditionRow
                    {
                        PersonId = p, Index = rows.Count, Person = person.PersonName, Trait = person.Trait.ToString(),
                        Description = c.Description, Label = c.Type + " " + c.Target + " " +
                            (c.Target == ConditionTarget.Food ? c.FoodTarget.ToString() : c.TargetTrait.ToString()),
                        Ignored = Ignored(c),
                        InitiallySatisfied = _sources[p] >= 0 && ConditionOK(c, _sources[p], _initial)
                    };
                    Result.TotalConditions++;
                    if (row.Ignored) Result.IgnoredConditions++;
                    else if (c.Target == ConditionTarget.Person)
                    {
                        Result.PersonConditions++;
                        for (int q = 0; q < Result.N; q++)
                            if (q != p && _people[q].Trait == c.TargetTrait) row.MatchingOthers++;
                        if (c.Type == ConditionType.Like)
                        {
                            Result.LikePersonConditions++;
                            if (row.MatchingOthers == 0)
                                Result.Problems.Add($"#{p} {person.PersonName}: Like {c.TargetTrait} không có Person khác phù hợp.");
                        }
                    }
                    else Result.FoodConditions++;
                    rows.Add(row);
                    Result.Conditions.Add(row);
                }
                // Duplicate requirements are the same event; Like/Hate of the same target contradict.
                bool contradiction = false;
                for (int i = 0; i < person.Conditions.Count; i++)
                    for (int j = 0; j < i; j++)
                        if (SameTarget(person.Conditions[i], person.Conditions[j]) &&
                            person.Conditions[i].Type != person.Conditions[j].Type) contradiction = true;
                if (contradiction) Result.Problems.Add($"#{p} {person.PersonName}: Like và Hate cùng một mục tiêu.");
                for (int seat = 0; seat < Result.S; seat++)
                {
                    var detail = new DifficultySeatProbabilityRow
                    {
                        PersonId = p, X = _seats[seat].Index.x, Y = _seats[seat].Index.y,
                        AdjacentSeats = _neighbors[seat].Length, JointProbability = contradiction ? 0 : 1
                    };
                    for (int i = 0; i < person.Conditions.Count; i++)
                    {
                        var c = person.Conditions[i];
                        double probability;
                        if (Ignored(c)) probability = 1;
                        else if (c.Target == ConditionTarget.Food) probability = ConditionOK(c, seat, _initial) ? 1 : 0;
                        else
                        {
                            double none = NoAdjacentProbability(Result.S, _neighbors[seat].Length, rows[i].MatchingOthers);
                            probability = c.Type == ConditionType.Like ? 1 - none : none;
                        }
                        detail.ConditionProbabilities.Add(probability);
                        rows[i].Probability += probability / Result.S;
                        bool duplicate = false;
                        for (int j = 0; j < i; j++)
                            if (SameTarget(c, person.Conditions[j]) && c.Type == person.Conditions[j].Type) duplicate = true;
                        if (!duplicate) detail.JointProbability *= probability;
                    }
                    Result.People[p].ExpectedSeatRatio += detail.JointProbability / Result.S;
                    Result.SeatProbabilities.Add(detail);
                }
                Result.People[p].Tightness = 1 - Result.People[p].ExpectedSeatRatio;
                Result.Tightness += Result.People[p].Tightness / Result.N;
                if (Result.People[p].ExpectedSeatRatio <= 0)
                    Result.Problems.Add($"#{p} {person.PersonName}: không có Seat nào có thể thỏa Conditions.");
            }
            Result.Occupancy = Clamp((double)Result.N / Result.S);
            Result.ConditionComplexity = 0.8 * Result.Tightness + 0.2 * Result.Occupancy;
        }

        private static bool SameTarget(ConditionRuntimeData a, ConditionRuntimeData b) =>
            !Ignored(a) && !Ignored(b) && a.Target == b.Target &&
            (a.Target == ConditionTarget.Food ? a.FoodTarget == b.FoodTarget : a.TargetTrait == b.TargetTrait);

        // C(S-1-K, N) / C(S-1, N), evaluated as a bounded product without factorial overflow.
        public static double NoAdjacentProbability(int seats, int adjacentSeats, int matchingOthers)
        {
            int available = seats - 1;
            if (matchingOthers == 0 || adjacentSeats == 0) return 1;
            if (matchingOthers > available - adjacentSeats) return 0;
            double probability = 1;
            for (int i = 0; i < matchingOthers; i++)
                probability *= (double)(available - adjacentSeats - i) / (available - i);
            return probability;
        }

        private void CalculateDependencies()
        {
            // Resolve AND between target traits, OR between providers of each trait, one layer at a time.
            for (int layer = 0; layer < Result.N; layer++)
            {
                List<int> ready = new();
                for (int p = 0; p < Result.N; p++)
                {
                    if (Result.People[p].DependencyLayer >= 0) continue;
                    bool satisfied = true;
                    foreach (var c in _people[p].Conditions)
                    {
                        if (c.Target != ConditionTarget.Person || c.Type != ConditionType.Like) continue;
                        bool found = false;
                        for (int q = 0; q < Result.N; q++)
                            if (q != p && _people[q].Trait == c.TargetTrait && Result.People[q].DependencyLayer >= 0)
                                found = true;
                        if (!found) satisfied = false;
                    }
                    if (satisfied) ready.Add(p);
                }
                if (ready.Count == 0) break;
                foreach (int p in ready) Result.People[p].DependencyLayer = layer;
                Result.MaxDependencyDepth = layer;
            }
            // Reachability on unresolved requirements identifies actual SCC members, excluding tails.
            bool[,] reach = new bool[Result.N, Result.N];
            for (int p = 0; p < Result.N; p++)
            {
                if (Result.People[p].DependencyLayer >= 0) continue;
                Result.BlockedPeople++;
                foreach (var c in _people[p].Conditions)
                {
                    if (c.Target != ConditionTarget.Person || c.Type != ConditionType.Like) continue;
                    bool resolved = false;
                    for (int q = 0; q < Result.N; q++)
                        if (q != p && _people[q].Trait == c.TargetTrait && Result.People[q].DependencyLayer >= 0) resolved = true;
                    if (resolved) continue;
                    for (int q = 0; q < Result.N; q++)
                        if (q != p && _people[q].Trait == c.TargetTrait && Result.People[q].DependencyLayer < 0) reach[p, q] = true;
                }
            }
            for (int k = 0; k < Result.N; k++)
                for (int i = 0; i < Result.N; i++)
                    for (int j = 0; j < Result.N; j++) reach[i, j] |= reach[i, k] && reach[k, j];
            for (int p = 0; p < Result.N; p++)
                if (reach[p, p]) { Result.People[p].InDependencyCycle = true; Result.CyclePeople++; }
            Result.LikeDensity = (double)Result.LikePersonConditions / (2 * Result.N);
            Result.DependencyDepth = (double)Result.MaxDependencyDepth / (Result.MaxDependencyDepth + 1);
            Result.CycleRatio = (double)Result.CyclePeople / Result.N;
            Result.DependencyComplexity = 0.25 * Result.LikeDensity + 0.30 * Result.DependencyDepth + 0.45 * Result.CycleRatio;
        }

        private IEnumerable<int> Search(int seat, int empties)
        {
            Result.Nodes++;
            yield return 0;
            if (seat == Result.S)
            {
                Result.SolutionsFound++;
                for (int s = 0; s < Result.S; s++) if (_assignment[s] >= 0) _destination[_assignment[s]] = s;
                int moves = MinimumMoveCost(_sources.ToArray(), _destination);
                if (Result.BestFoundMoves < 0 || moves < Result.BestFoundMoves)
                {
                    Result.BestFoundMoves = moves;
                    Result.BestAssignment.Clear();
                    for (int p = 0; p < Result.N; p++)
                        Result.BestAssignment.Add($"#{p} {_people[p].PersonName} -> {_seats[_destination[p]].Index}");
                }
                yield break;
            }
            if (empties < Result.E)
            {
                _assignment[seat] = -1;
                if (Check(seat)) foreach (int step in Search(seat + 1, empties + 1)) yield return step;
            }
            for (int p = 0; p < Result.N; p++)
            {
                if (_used[p] || !_allowed[p, seat]) continue;
                _used[p] = true;
                _assignment[seat] = p;
                if (Check(seat)) foreach (int step in Search(seat + 1, empties)) yield return step;
                _used[p] = false;
            }
            _assignment[seat] = -1;
        }

        private bool Check(int last)
        {
            foreach (int seat in _checkAt[last])
            {
                int p = _assignment[seat];
                if (p < 0) continue;
                foreach (var c in _people[p].Conditions)
                    if (c.Target == ConditionTarget.Person && !ConditionOK(c, seat, _assignment)) return false;
            }
            return true;
        }

        // Every misplaced person costs one move, except one saved move per occupied cycle.
        public static int MinimumMoveCost(int[] sources, int[] destinations)
        {
            Dictionary<int, int> source2Person = new();
            for (int p = 0; p < sources.Length; p++) if (sources[p] >= 0) source2Person.Add(sources[p], p);
            bool[] misplaced = new bool[sources.Length], seen = new bool[sources.Length];
            int count = 0, cycles = 0;
            for (int p = 0; p < sources.Length; p++)
            {
                misplaced[p] = sources[p] != destinations[p];
                if (misplaced[p]) count++;
            }
            for (int p = 0; p < sources.Length; p++)
            {
                if (!misplaced[p] || seen[p]) continue;
                int current = p;
                while (current >= 0 && misplaced[current] && !seen[current])
                {
                    seen[current] = true;
                    current = source2Person.TryGetValue(destinations[current], out int next) ? next : -1;
                }
                if (current == p) cycles++;
            }
            return count - cycles;
        }

        public void Tick()
        {
            if (!IsRunning) return;
            long start = Stopwatch.GetTimestamp();
            do
            {
                if (!_search.MoveNext()) { Complete(true); return; }
                if (Result.Nodes > Result.Settings.MaxNodes) { Complete(false); return; }
            }
            while ((Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency < 8);
        }

        public void Cancel()
        {
            if (!IsRunning) return;
            _search?.Dispose();
            Result.Status = DifficultyAnalysisStatus.Cancelled;
            Result.Message = "Đã hủy; không tính điểm từ dữ liệu duyệt dở.";
            Result.ElapsedSeconds = _watch.Elapsed.TotalSeconds;
        }

        private void Reject(string message)
        {
            Result.Problems.Add(message);
            Result.Valid = false;
            Result.Status = DifficultyAnalysisStatus.InvalidInput;
            Result.Message = message;
            Result.ElapsedSeconds = _watch.Elapsed.TotalSeconds;
        }

        private void Complete(bool exact)
        {
            var r = Result;
            r.SolverExact = exact && r.SolverRan;
            r.HasExactMoves = r.SolutionsFound > 0 && (r.SolverExact || r.BestFoundMoves == r.WaitPeople);
            if (r.SolverExact && r.SolutionsFound == 0) r.Problems.Add("Không tồn tại state Win thỏa mọi Conditions.");
            // WaitPeople is a proven lower bound. Reaching it certifies M* even before enumeration ends.
            if (r.HasExactMoves)
            {
                r.MinMoves = r.BestFoundMoves;
                r.Slack = r.M - r.MinMoves;
                r.MoveSource = "M* đã được chứng minh bởi bộ giải Move/Swap";
                if (r.MinMoves > r.M) r.Problems.Add("M* > LevelMove: không đủ lượt để hoàn thành.");
                r.BudgetTightness = r.M > 0 ? (double)r.MinMoves / r.M : 0;
                r.Rearrangement = Clamp((double)(r.MinMoves - r.WaitPeople) / r.N);
                r.MovePressure = 0.70 * r.BudgetTightness + 0.30 * r.Rearrangement;
            }
            else
            {
                r.MoveSource = "Chưa xác định M*; không dùng ước lượng để chấm điểm";
                if (r.WaitPeople > r.M) r.Problems.Add("Số Person WaitGrid > LevelMove: không đủ lượt tối thiểu.");
            }
            r.ConditionContribution = 50 * r.ConditionComplexity;
            r.DependencyContribution = 30 * r.DependencyComplexity;
            r.MoveContribution = 20 * r.MovePressure;
            r.Valid = r.Problems.Count == 0;
            r.HasScore = r.Valid && r.HasExactMoves;
            if (r.HasScore) r.DifficultyScore = r.ConditionContribution + r.DependencyContribution + r.MoveContribution;
            if (r.SolverRan && !r.SolverExact)
                r.Warnings.Add("Hết ngân sách node: số cách xếp là cận dưới; best_found là cận trên M*. M* chỉ chính xác nếu đạt cận dưới WaitPeople.");
            r.Status = !r.Valid ? DifficultyAnalysisStatus.Unsolvable : r.HasScore ? DifficultyAnalysisStatus.Complete :
                r.SolverRan ? DifficultyAnalysisStatus.LimitExceeded : DifficultyAnalysisStatus.Partial;
            r.Message = !r.Valid ? "Level vô nghiệm hoặc không đủ lượt. Không có điểm độ khó." :
                r.HasScore ? "Điểm theo mô hình Condition 50% / Dependency 30% / Move 20%." :
                "Đã tính Condition và Dependency. Cần M* chính xác để tính Move và điểm tổng.";
            r.ElapsedSeconds = _watch.Elapsed.TotalSeconds;
            _search?.Dispose();
        }

        private static double Clamp(double x) => Math.Max(0, Math.Min(1, x));

        public static string ComputeInputFingerprint(LevelDataSO level, DifficultyAnalysisSettings settings)
        {
            if (level == null) return "";
            StringBuilder content = new();
            content.Append(CurrentModelId).Append(CurrentModelVersion).Append(JsonUtility.ToJson(settings));
            content.Append(EditorJsonUtility.ToJson(level));
            AppendCells(content, level.mainGrid);
            AppendCells(content, level.waitGrid);
            if (level.personConfigs != null)
                foreach (var p in level.personConfigs)
                {
                    if (p == null) continue;
                    if (p.definition != null) content.Append(EditorJsonUtility.ToJson(p.definition));
                    if (p.conditions != null)
                        foreach (var c in p.conditions) if (c != null) content.Append(EditorJsonUtility.ToJson(c));
                }
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void AppendCells(StringBuilder content, Grid<CellDataSO> grid)
        {
            if (grid == null) return;
            foreach (var cell in grid.GridContent) if (cell != null) content.Append(EditorJsonUtility.ToJson(cell));
        }
    }
}
