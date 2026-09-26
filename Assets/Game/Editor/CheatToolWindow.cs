using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.App.SaveAndLoad;
using Game.Bootstrap;
using Game.Core.Economy;

namespace Game.Editor
{
    public class CheatToolWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private SaveLoadManager _saveLoad;
        private GameData _editData;

        // Custom input buffers
        private int _inputGold = 1000;
        private int _inputGem = 500;
        private int _inputRemove = 5;
        private int _inputUndo = 5;
        private int _inputMoreMoves = 5;
        private int _inputLevel = 1;

        private GameBootstrapper _cachedGameBootstrapper;

        [MenuItem("Tools/Cheat Tool")]
        [MenuItem("Window/Cheat Tool")]
        public static void Open()
        {
            var window = GetWindow<CheatToolWindow>("Cheat Tool");
            window.minSize = new Vector2(400, 560);
            window.Show();
        }

        private void OnEnable()
        {
            _saveLoad = new SaveLoadManager();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            LoadData();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnFocus()
        {
            LoadData();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            LoadData();
            Repaint();
        }

        private void LoadData()
        {
            if (_saveLoad == null)
            {
                _saveLoad = new SaveLoadManager();
            }

            _editData = _saveLoad.GetGameData();
            if (_editData.goldShopPurchaseCountToday == null || _editData.goldShopPurchaseCountToday.Length < 3)
            {
                _editData.goldShopPurchaseCountToday = new int[3];
            }

            if (Application.isPlaying)
            {
                _cachedGameBootstrapper = UnityEngine.Object.FindFirstObjectByType<GameBootstrapper>();
            }
            else
            {
                _cachedGameBootstrapper = null;
            }
        }

        private void SaveEditData()
        {
            if (_saveLoad != null)
            {
                _saveLoad.SaveGameData(_editData);
            }
        }

        private bool IsPlayingWithGameBootstrapper => Application.isPlaying && _cachedGameBootstrapper != null;

        private void OnGUI()
        {
            if (Application.isPlaying && _cachedGameBootstrapper == null)
            {
                _cachedGameBootstrapper = UnityEngine.Object.FindFirstObjectByType<GameBootstrapper>();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            EditorGUILayout.Space(6);

            DrawCurrenciesSection();
            EditorGUILayout.Space(6);

            DrawBoostersSection();
            EditorGUILayout.Space(6);

            DrawLoginAndRewardsSection();
            EditorGUILayout.Space(6);

            DrawDailyShopSection();
            EditorGUILayout.Space(6);

            DrawProgressionSection();
            EditorGUILayout.Space(6);

            DrawSaveManagementSection();
            EditorGUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        // ── Status Header ───────────────────────────────────

        private void DrawHeader()
        {
            Color defaultBg = GUI.backgroundColor;

            if (IsPlayingWithGameBootstrapper)
            {
                GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("MODE: PLAY MODE (Live GameBootstrapper)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Edits take effect instantly in-game, update UI, and persist to data.json.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("MODE: EDIT MODE (Direct data.json)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Edits are saved directly to data.json. Will be loaded on game start.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            GUI.backgroundColor = defaultBg;
        }

        // ── Currencies Section ──────────────────────────────

        private void DrawCurrenciesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Currencies (Gold & Gem)", EditorStyles.boldLabel);

            // Gold
            int currentGold = GetGold();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Gold: {currentGold}", GUILayout.Width(130));
            _inputGold = EditorGUILayout.IntField(_inputGold, GUILayout.Width(65));
            if (GUILayout.Button("Set", GUILayout.Width(40))) SetGold(_inputGold);
            if (GUILayout.Button("+100", GUILayout.Width(45))) SetGold(currentGold + 100);
            if (GUILayout.Button("+1K", GUILayout.Width(45))) SetGold(currentGold + 1000);
            if (GUILayout.Button("+10K", GUILayout.Width(45))) SetGold(currentGold + 10000);
            if (GUILayout.Button("0", GUILayout.Width(25))) SetGold(0);
            EditorGUILayout.EndHorizontal();

            // Gem
            int currentGem = GetGem();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Gem: {currentGem}", GUILayout.Width(130));
            _inputGem = EditorGUILayout.IntField(_inputGem, GUILayout.Width(65));
            if (GUILayout.Button("Set", GUILayout.Width(40))) SetGem(_inputGem);
            if (GUILayout.Button("+50", GUILayout.Width(45))) SetGem(currentGem + 50);
            if (GUILayout.Button("+500", GUILayout.Width(45))) SetGem(currentGem + 500);
            if (GUILayout.Button("+5K", GUILayout.Width(45))) SetGem(currentGem + 5000);
            if (GUILayout.Button("0", GUILayout.Width(25))) SetGem(0);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── Boosters Section ────────────────────────────────

        private void DrawBoostersSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Boosters", EditorStyles.boldLabel);

            // Remove
            int currentRemove = GetBooster(ItemType.Remove);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Remove: {currentRemove}", GUILayout.Width(130));
            _inputRemove = EditorGUILayout.IntField(_inputRemove, GUILayout.Width(65));
            if (GUILayout.Button("Set", GUILayout.Width(40))) SetBooster(ItemType.Remove, _inputRemove);
            if (GUILayout.Button("+1", GUILayout.Width(45))) SetBooster(ItemType.Remove, currentRemove + 1);
            if (GUILayout.Button("+5", GUILayout.Width(45))) SetBooster(ItemType.Remove, currentRemove + 5);
            if (GUILayout.Button("+10", GUILayout.Width(45))) SetBooster(ItemType.Remove, currentRemove + 10);
            if (GUILayout.Button("0", GUILayout.Width(25))) SetBooster(ItemType.Remove, 0);
            EditorGUILayout.EndHorizontal();

            // Undo
            int currentUndo = GetBooster(ItemType.Undo);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Undo: {currentUndo}", GUILayout.Width(130));
            _inputUndo = EditorGUILayout.IntField(_inputUndo, GUILayout.Width(65));
            if (GUILayout.Button("Set", GUILayout.Width(40))) SetBooster(ItemType.Undo, _inputUndo);
            if (GUILayout.Button("+1", GUILayout.Width(45))) SetBooster(ItemType.Undo, currentUndo + 1);
            if (GUILayout.Button("+5", GUILayout.Width(45))) SetBooster(ItemType.Undo, currentUndo + 5);
            if (GUILayout.Button("+10", GUILayout.Width(45))) SetBooster(ItemType.Undo, currentUndo + 10);
            if (GUILayout.Button("0", GUILayout.Width(25))) SetBooster(ItemType.Undo, 0);
            EditorGUILayout.EndHorizontal();

            // More Moves
            int currentMoreMoves = GetBooster(ItemType.MoreMoves);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"More Moves: {currentMoreMoves}", GUILayout.Width(130));
            _inputMoreMoves = EditorGUILayout.IntField(_inputMoreMoves, GUILayout.Width(65));
            if (GUILayout.Button("Set", GUILayout.Width(40))) SetBooster(ItemType.MoreMoves, _inputMoreMoves);
            if (GUILayout.Button("+1", GUILayout.Width(45))) SetBooster(ItemType.MoreMoves, currentMoreMoves + 1);
            if (GUILayout.Button("+5", GUILayout.Width(45))) SetBooster(ItemType.MoreMoves, currentMoreMoves + 5);
            if (GUILayout.Button("+10", GUILayout.Width(45))) SetBooster(ItemType.MoreMoves, currentMoreMoves + 10);
            if (GUILayout.Button("0", GUILayout.Width(25))) SetBooster(ItemType.MoreMoves, 0);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Presets
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Max Out All (99,999 / 99)", GUILayout.Height(24)))
            {
                SetGold(99999);
                SetGem(99999);
                SetBooster(ItemType.Remove, 99);
                SetBooster(ItemType.Undo, 99);
                SetBooster(ItemType.MoreMoves, 99);
            }
            if (GUILayout.Button("Clear All (0)", GUILayout.Height(24)))
            {
                SetGold(0);
                SetGem(0);
                SetBooster(ItemType.Remove, 0);
                SetBooster(ItemType.Undo, 0);
                SetBooster(ItemType.MoreMoves, 0);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── Login & Rewards Section ─────────────────────────

        private void DrawLoginAndRewardsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Login Streak & Claim States", EditorStyles.boldLabel);

            int currentDay = GetLoginDay();
            bool dailyClaimed = GetDailyClaimed();
            bool weeklyClaimed = GetWeeklyClaimed();

            // Day buttons (1-7)
            EditorGUILayout.LabelField($"Current Login Day: Day {currentDay + 1} (Index {currentDay})");
            EditorGUILayout.BeginHorizontal();
            Color defaultBg = GUI.backgroundColor;
            for (int i = 0; i < 7; i++)
            {
                bool isSelected = (i == currentDay);
                if (isSelected) GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
                if (GUILayout.Button($"Day {i + 1}", GUILayout.Height(24)))
                {
                    SetLoginDay(i);
                }
                if (isSelected) GUI.backgroundColor = defaultBg;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Claim Toggles
            EditorGUILayout.BeginHorizontal();
            bool newDaily = EditorGUILayout.ToggleLeft($"Daily Reward Claimed: {(dailyClaimed ? "YES" : "NO")}", dailyClaimed, GUILayout.Width(200));
            if (newDaily != dailyClaimed) SetDailyClaimed(newDaily);

            bool newWeekly = EditorGUILayout.ToggleLeft($"Weekly Reward Claimed: {(weeklyClaimed ? "YES" : "NO")}", weeklyClaimed, GUILayout.Width(200));
            if (newWeekly != weeklyClaimed) SetWeeklyClaimed(newWeekly);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Quick actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Today Claims", GUILayout.Height(24)))
            {
                ResetTodayClaims();
            }
            if (GUILayout.Button("Simulate Next Day (+1)", GUILayout.Height(24)))
            {
                SimulateNextDay();
            }
            if (GUILayout.Button("Reset Streak (Day 1)", GUILayout.Height(24)))
            {
                ResetLoginStreak();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── Daily Shop Limits Section ───────────────────────

        private void DrawDailyShopSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Daily Gold Shop (Limits: 5/day each)", EditorStyles.boldLabel);

            int count0 = GetShopPurchaseCount(0);
            int count1 = GetShopPurchaseCount(1);
            int count2 = GetShopPurchaseCount(2);

            EditorGUILayout.LabelField($"Slot 0 (Remove): {count0}/5  |  Slot 1 (Undo): {count1}/5  |  Slot 2 (More Moves): {count2}/5");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Shop Limits (0/5)", GUILayout.Height(24)))
            {
                ResetShopPurchases();
            }
            if (GUILayout.Button("Max Out Purchases (5/5)", GUILayout.Height(24)))
            {
                SetShopPurchaseCount(0, 5);
                SetShopPurchaseCount(1, 5);
                SetShopPurchaseCount(2, 5);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── Level Progression & Bypass Section ───────────────

        private void DrawProgressionSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Level Progression & Bypass", EditorStyles.boldLabel);

            int currentLevel = GetLevel();
            int totalLevels = IsPlayingWithGameBootstrapper ? _cachedGameBootstrapper.GameManager.TotalLevels : 0;
            string levelInfo = totalLevels > 0 ? $"Current Level: {currentLevel} / {totalLevels}" : $"Current Level: {currentLevel}";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(levelInfo, GUILayout.Width(150));
            _inputLevel = EditorGUILayout.IntField(_inputLevel, GUILayout.Width(60));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                SetLevel(_inputLevel);
            }
            if (IsPlayingWithGameBootstrapper)
            {
                if (GUILayout.Button("Set & Load", GUILayout.Width(80)))
                {
                    PlayLevel(_inputLevel);
                }
            }
            if (GUILayout.Button("+1", GUILayout.Width(35))) SetLevel(currentLevel + 1);
            if (GUILayout.Button("-1", GUILayout.Width(35))) SetLevel(Math.Max(1, currentLevel - 1));
            if (GUILayout.Button("Lvl 1", GUILayout.Width(45))) SetLevel(1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Bypass Controls
            EditorGUILayout.LabelField("Bypass Level Flow (Play Mode):", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            Color defaultBg = GUI.backgroundColor;
            GUI.enabled = IsPlayingWithGameBootstrapper;

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("★ Bypass Win (Trigger Win)", GUILayout.Height(26)))
            {
                _cachedGameBootstrapper.GameManager.ForceWin();
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.3f);
            if (GUILayout.Button("✕ Bypass Lose (Trigger Lose)", GUILayout.Height(26)))
            {
                _cachedGameBootstrapper.GameManager.ForceLose();
            }

            GUI.backgroundColor = defaultBg;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (IsPlayingWithGameBootstrapper)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Skip to Next Level (+1)", GUILayout.Height(22)))
                {
                    PlayLevel(currentLevel + 1);
                }
                if (GUILayout.Button("Restart Level", GUILayout.Height(22)))
                {
                    RestartLevel();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use Bypass Win / Lose and instant level loading.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // ── Save Management Section ─────────────────────────

        private void DrawSaveManagementSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Save File Management", EditorStyles.boldLabel);

            string path = Path.Combine(Application.persistentDataPath, "data.json");
            EditorGUILayout.LabelField($"Path: {path}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save to Disk", GUILayout.Height(24)))
            {
                if (IsPlayingWithGameBootstrapper)
                {
                    _cachedGameBootstrapper.GameManager.SaveGame();
                }
                else
                {
                    SaveEditData();
                }
                Debug.Log($"[CheatTool] Data saved to: {path}");
            }

            if (GUILayout.Button("Reload from Disk", GUILayout.Height(24)))
            {
                LoadData();
                Debug.Log("[CheatTool] Reloaded data from disk.");
            }

            if (GUILayout.Button("Open Save Folder", GUILayout.Height(24)))
            {
                if (File.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    EditorUtility.RevealInFinder(Application.persistentDataPath);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            Color defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete Save File (data.json)", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirm Reset",
                    "Are you sure you want to delete data.json? All progress and economy data will be reset.",
                    "Delete",
                    "Cancel"))
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        Debug.Log("[CheatTool] Deleted data.json.");
                    }
                    LoadData();
                    if (IsPlayingWithGameBootstrapper)
                    {
                        _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.Gold, 0);
                        _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.Gem, 0);
                        _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.Remove, 0);
                        _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.Undo, 0);
                        _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.MoreMoves, 0);
                        _cachedGameBootstrapper.GameManager.EconomyManager.ResetLoginStreak();
                        _cachedGameBootstrapper.GameManager.SetLevel(1);
                        _cachedGameBootstrapper.GameManager.SaveGame();
                        _cachedGameBootstrapper.GameManager.RefreshEconomyPresentation();
                    }
                }
            }
            GUI.backgroundColor = defaultBg;

            EditorGUILayout.EndVertical();
        }

        // ── Data Getters & Setters ──────────────────────────

        private int GetGold()
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.Inventory.Gold;
            return _editData.currentGold;
        }

        private void SetGold(int amount)
        {
            amount = Math.Max(0, amount);
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.Gold, amount);
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.currentGold = amount;
                SaveEditData();
            }
        }

        private int GetGem()
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.Inventory.Gem;
            return _editData.currentGem;
        }

        private void SetGem(int amount)
        {
            amount = Math.Max(0, amount);
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.Inventory.SetAmount(ItemType.Gem, amount);
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.currentGem = amount;
                SaveEditData();
            }
        }

        private int GetBooster(ItemType type)
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.Inventory.GetAmount(type);
            return type switch
            {
                ItemType.Remove => _editData.currentRemove,
                ItemType.Undo => _editData.currentUndo,
                ItemType.MoreMoves => _editData.currentMoreMoves,
                _ => 0
            };
        }

        private void SetBooster(ItemType type, int amount)
        {
            amount = Math.Max(0, amount);
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.Inventory.SetAmount(type, amount);
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                switch (type)
                {
                    case ItemType.Remove: _editData.currentRemove = amount; break;
                    case ItemType.Undo: _editData.currentUndo = amount; break;
                    case ItemType.MoreMoves: _editData.currentMoreMoves = amount; break;
                }
                SaveEditData();
            }
        }

        private int GetLoginDay()
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.EconomyManager.CurrentLoginDay;
            return _editData.currentLoginDay;
        }

        private void SetLoginDay(int day)
        {
            day = Math.Max(0, day % 7);
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.SetLoginDay(day);
                _cachedGameBootstrapper.GameManager.RefreshEconomyPresentation();
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.currentLoginDay = day;
                SaveEditData();
            }
        }

        private bool GetDailyClaimed()
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.EconomyManager.IsDailyRewardClaimed;
            return _editData.isDailyRewardClaimed;
        }

        private void SetDailyClaimed(bool claimed)
        {
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.SetDailyRewardClaimed(claimed);
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.isDailyRewardClaimed = claimed;
                SaveEditData();
            }
        }

        private bool GetWeeklyClaimed()
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.EconomyManager.IsWeeklyRewardClaimed;
            return _editData.isWeeklyRewardClaimed;
        }

        private void SetWeeklyClaimed(bool claimed)
        {
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.SetWeeklyRewardClaimed(claimed);
                _cachedGameBootstrapper.GameManager.RefreshEconomyPresentation();
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.isWeeklyRewardClaimed = claimed;
                SaveEditData();
            }
        }

        private void ResetTodayClaims()
        {
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.SetDailyRewardClaimed(false);
                _cachedGameBootstrapper.GameManager.EconomyManager.SetWeeklyRewardClaimed(false);
                _cachedGameBootstrapper.GameManager.EconomyManager.ResetShopPurchases();
                _cachedGameBootstrapper.GameManager.RefreshEconomyPresentation();
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.isDailyRewardClaimed = false;
                _editData.isWeeklyRewardClaimed = false;
                if (_editData.goldShopPurchaseCountToday != null)
                {
                    for (int i = 0; i < _editData.goldShopPurchaseCountToday.Length; i++)
                        _editData.goldShopPurchaseCountToday[i] = 0;
                }
                SaveEditData();
            }
        }

        private void SimulateNextDay()
        {
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.SimulateNextDay();
                _cachedGameBootstrapper.GameManager.RefreshEconomyPresentation();
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.currentLoginDay = (_editData.currentLoginDay + 1) % 7;
                _editData.isDailyRewardClaimed = false;
                _editData.isWeeklyRewardClaimed = false;
                if (_editData.goldShopPurchaseCountToday != null)
                {
                    for (int i = 0; i < _editData.goldShopPurchaseCountToday.Length; i++)
                        _editData.goldShopPurchaseCountToday[i] = 0;
                }
                SaveEditData();
            }
        }

        private void ResetLoginStreak()
        {
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.ResetLoginStreak();
                _cachedGameBootstrapper.GameManager.RefreshEconomyPresentation();
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                _editData.currentLoginDay = 0;
                _editData.isDailyRewardClaimed = false;
                _editData.isWeeklyRewardClaimed = false;
                if (_editData.goldShopPurchaseCountToday != null)
                {
                    for (int i = 0; i < _editData.goldShopPurchaseCountToday.Length; i++)
                        _editData.goldShopPurchaseCountToday[i] = 0;
                }
                SaveEditData();
            }
        }

        private int GetShopPurchaseCount(int slot)
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.EconomyManager.GetGoldShopPurchaseCount(slot);
            if (_editData.goldShopPurchaseCountToday != null && slot >= 0 && slot < _editData.goldShopPurchaseCountToday.Length)
                return _editData.goldShopPurchaseCountToday[slot];
            return 0;
        }

        private void ResetShopPurchases()
        {
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.ResetShopPurchases();
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                if (_editData.goldShopPurchaseCountToday != null)
                {
                    for (int i = 0; i < _editData.goldShopPurchaseCountToday.Length; i++)
                        _editData.goldShopPurchaseCountToday[i] = 0;
                }
                SaveEditData();
            }
        }

        private void SetShopPurchaseCount(int slot, int count)
        {
            count = Math.Max(0, count);
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.EconomyManager.SetShopPurchaseCount(slot, count);
                _cachedGameBootstrapper.GameManager.SaveGame();
            }
            else
            {
                if (_editData.goldShopPurchaseCountToday == null || _editData.goldShopPurchaseCountToday.Length < 3)
                    _editData.goldShopPurchaseCountToday = new int[3];

                if (slot >= 0 && slot < _editData.goldShopPurchaseCountToday.Length)
                    _editData.goldShopPurchaseCountToday[slot] = count;

                SaveEditData();
            }
        }

        private int GetLevel()
        {
            if (IsPlayingWithGameBootstrapper) return _cachedGameBootstrapper.GameManager.CurrentLevel;
            return _editData.currentLevel > 0 ? _editData.currentLevel : 1;
        }

        private async void PlayLevel(int level)
        {
            try
            {
                await _cachedGameBootstrapper.GameManager.PlayLevelAsync(level);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async void RestartLevel()
        {
            try
            {
                await _cachedGameBootstrapper.GameManager.RestartLevelAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void SetLevel(int level)
        {
            level = Math.Max(1, level);
            if (IsPlayingWithGameBootstrapper)
            {
                _cachedGameBootstrapper.GameManager.SetLevel(level);
            }
            else
            {
                _editData.currentLevel = level;
                SaveEditData();
            }
        }
    }
}
