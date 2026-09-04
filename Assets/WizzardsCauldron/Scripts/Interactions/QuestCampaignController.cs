using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WizzardsCauldron.Core;

namespace WizzardsCauldron.Interactions
{
    public sealed class QuestRoundInfo
    {
        public QuestRoundInfo(
            int roundIndex,
            int roundCount,
            string title,
            PuzzleDefinition puzzle)
        {
            RoundIndex = roundIndex;
            RoundCount = roundCount;
            Title = title;
            Puzzle = puzzle;
        }

        public int RoundIndex { get; }
        public int RoundNumber => RoundIndex + 1;
        public int RoundCount { get; }
        public string Title { get; }
        public PuzzleDefinition Puzzle { get; }
    }

    public sealed class QuestRoundResult
    {
        public QuestRoundResult(
            QuestRoundInfo round,
            AttemptResult attempt,
            int stars,
            bool hintUsed,
            int bestStars,
            int totalStars)
        {
            Round = round;
            Attempt = attempt;
            Stars = stars;
            HintUsed = hintUsed;
            BestStars = bestStars;
            TotalStars = totalStars;
        }

        public QuestRoundInfo Round { get; }
        public AttemptResult Attempt { get; }
        public int Stars { get; }
        public bool HintUsed { get; }
        public int BestStars { get; }
        public int TotalStars { get; }
        public bool IsFinalRound =>
            Round.RoundNumber == Round.RoundCount;
    }

    public sealed class QuestCampaignSummary
    {
        private readonly IReadOnlyList<int> _roundStars;

        public QuestCampaignSummary(int[] roundStars)
        {
            if (roundStars == null)
            {
                throw new ArgumentNullException(nameof(roundStars));
            }

            int[] copiedStars = (int[])roundStars.Clone();
            _roundStars = Array.AsReadOnly(copiedStars);

            int total = 0;
            for (int index = 0;
                 index < copiedStars.Length;
                 index++)
            {
                total += copiedStars[index];
            }

            TotalStars = total;
            MaximumStars = copiedStars.Length * 3;
        }

        public IReadOnlyList<int> RoundStars => _roundStars;
        public int TotalStars { get; }
        public int MaximumStars { get; }
    }

    [DisallowMultipleComponent]
    public sealed class QuestCampaignController : MonoBehaviour
    {
        [Header("Existing gameplay sources")]
        [SerializeField]
        private GameSessionController _gameSession;

        [SerializeField]
        private RoomResetCoordinator _roomReset;

        [Header("Ordered quest rounds")]
        [SerializeField]
        private PuzzleDefinition[] _roundPuzzles =
            Array.Empty<PuzzleDefinition>();

        [SerializeField]
        private string[] _roundTitles =
            Array.Empty<string>();

        [Header("Potion instances")]
        [SerializeField]
        private PotionController[] _potions =
            Array.Empty<PotionController>();

        [Header("Selection return")]
        [SerializeField, Min(0.5f)]
        private float _selectionReturnDelay = 4f;

        private int[] _roundStars = Array.Empty<int>();
        private bool[] _roundCompleted = Array.Empty<bool>();
        private int _currentRoundIndex = -1;
        private bool _hintUsedThisRound;
        private bool _roundResolved;
        private bool _campaignComplete;
        private bool _started;
        private bool _isSelectingDifficulty;
        private bool _isChangingRound;
        private Coroutine _selectionRoutine;
        private QuestRoundInfo _currentRound;
        private QuestRoundResult _latestRoundResult;

        public bool HasActiveRound =>
            _started &&
            _currentRoundIndex >= 0 &&
            _currentRoundIndex < RoundCount;

        public int CurrentRoundIndex => _currentRoundIndex;
        public int CurrentRoundNumber => HasActiveRound
            ? _currentRoundIndex + 1
            : 0;
        public int RoundCount =>
            _roundPuzzles != null
                ? _roundPuzzles.Length
                : 0;
        public string CurrentRoundTitle =>
            _currentRound != null
                ? _currentRound.Title
                : string.Empty;
        public bool HintUsedThisRound => _hintUsedThisRound;
        public bool IsCampaignComplete => _campaignComplete;
        public bool IsSelectingDifficulty =>
            _started && _isSelectingDifficulty;
        public float SelectionReturnDelay =>
            _selectionReturnDelay;
        public QuestRoundInfo CurrentRound => _currentRound;
        public QuestRoundResult LatestRoundResult =>
            _latestRoundResult;

        public int TotalStars
        {
            get
            {
                int total = 0;
                for (int index = 0;
                     index < _roundStars.Length;
                     index++)
                {
                    total += _roundStars[index];
                }

                return total;
            }
        }

        public event Action<QuestRoundInfo> RoundStarted;
        public event Action<QuestRoundResult> RoundFinished;
        public event Action<PotionController> HintRevealed;
        public event Action<string> HintUnavailable;
        public event Action<QuestCampaignSummary> CampaignFinished;
        public event Action DifficultySelectionRequested;

        public void Configure(
            GameSessionController gameSession,
            RoomResetCoordinator roomReset,
            PuzzleDefinition[] roundPuzzles,
            string[] roundTitles,
            PotionController[] potions,
            float selectionReturnDelay)
        {
            _gameSession = gameSession;
            _roomReset = roomReset;
            _roundPuzzles = roundPuzzles != null
                ? (PuzzleDefinition[])roundPuzzles.Clone()
                : Array.Empty<PuzzleDefinition>();
            _roundTitles = roundTitles != null
                ? (string[])roundTitles.Clone()
                : Array.Empty<string>();
            _potions = potions != null
                ? (PotionController[])potions.Clone()
                : Array.Empty<PotionController>();
            _selectionReturnDelay = Mathf.Max(
                0.5f,
                selectionReturnDelay);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying ||
                _gameSession == null)
            {
                return;
            }

            _gameSession.AttemptFinished +=
                HandleAttemptFinished;
            _gameSession.SessionReset +=
                HandleExternalSessionReset;
        }

        private void Start()
        {
            if (!ValidateConfiguration(out string error))
            {
                Debug.LogError(
                    "QuestCampaignController cannot start: " + error,
                    this);
                enabled = false;
                return;
            }

            _roundStars = new int[RoundCount];
            _roundCompleted = new bool[RoundCount];
            _started = true;
            EnterDifficultySelection();
        }

        private void OnDisable()
        {
            if (_gameSession != null)
            {
                _gameSession.AttemptFinished -=
                    HandleAttemptFinished;
                _gameSession.SessionReset -=
                    HandleExternalSessionReset;
            }

            StopPendingRoutines();
        }

        private void OnValidate()
        {
            _selectionReturnDelay = Mathf.Max(
                0.5f,
                _selectionReturnDelay);
        }

        public bool TrySelectRound(int roundIndex)
        {
            if (!_started ||
                !_isSelectingDifficulty ||
                roundIndex < 0 ||
                roundIndex >= RoundCount)
            {
                return false;
            }

            StartRound(roundIndex);
            return HasActiveRound &&
                _currentRoundIndex == roundIndex;
        }

        public int GetBestStars(int roundIndex)
        {
            return roundIndex >= 0 &&
                roundIndex < _roundStars.Length
                    ? _roundStars[roundIndex]
                    : 0;
        }

        public bool IsRoundCompleted(int roundIndex)
        {
            return roundIndex >= 0 &&
                roundIndex < _roundCompleted.Length &&
                _roundCompleted[roundIndex];
        }

        public bool TryUseHint(
            out PotionController hintedPotion)
        {
            hintedPotion = null;

            if (!HasActiveRound ||
                _roundResolved ||
                !_gameSession.IsPlaying)
            {
                HintUnavailable?.Invoke(
                    "Hint unavailable while the order is finished");
                return false;
            }

            if (_hintUsedThisRound)
            {
                HintUnavailable?.Invoke(
                    "Hint already used for this order");
                return false;
            }

            hintedPotion = FindAvailableOptimalPotion();
            if (hintedPotion == null)
            {
                HintUnavailable?.Invoke(
                    "No remaining optimal potion to reveal");
                return false;
            }

            _hintUsedThisRound = true;
            HintRevealed?.Invoke(hintedPotion);
            return true;
        }

        private void HandleAttemptFinished(
            AttemptResult attempt)
        {
            if (!HasActiveRound ||
                _roundResolved ||
                attempt == null)
            {
                return;
            }

            _roundResolved = true;
            int stars = AttemptStarRating.Calculate(
                attempt,
                _hintUsedThisRound);
            _roundStars[_currentRoundIndex] = Mathf.Max(
                _roundStars[_currentRoundIndex],
                stars);
            _roundCompleted[_currentRoundIndex] = true;

            _latestRoundResult = new QuestRoundResult(
                _currentRound,
                attempt,
                stars,
                _hintUsedThisRound,
                _roundStars[_currentRoundIndex],
                TotalStars);

            RoundFinished?.Invoke(_latestRoundResult);

            if (!_campaignComplete &&
                AreAllDifficultiesCompleted())
            {
                _campaignComplete = true;
                CampaignFinished?.Invoke(
                    new QuestCampaignSummary(_roundStars));
            }

            _selectionRoutine = StartCoroutine(
                ReturnToSelectionAfterDelay());
        }

        private IEnumerator ReturnToSelectionAfterDelay()
        {
            yield return new WaitForSecondsRealtime(
                _selectionReturnDelay);

            _selectionRoutine = null;
            EnterDifficultySelection();
        }

        private void StartRound(int roundIndex)
        {
            StopPendingRoutines();

            if (roundIndex < 0 ||
                roundIndex >= RoundCount)
            {
                Debug.LogError(
                    "Quest round index is outside the configured range.",
                    this);
                return;
            }

            _isSelectingDifficulty = false;
            _currentRoundIndex = roundIndex;
            _currentRound = new QuestRoundInfo(
                roundIndex,
                RoundCount,
                _roundTitles[roundIndex],
                _roundPuzzles[roundIndex]);
            _hintUsedThisRound = false;
            _roundResolved = false;
            _latestRoundResult = null;

            _isChangingRound = true;
            bool roomReady = _roomReset.TryResetRoom(
                _roundPuzzles[roundIndex]);
            _isChangingRound = false;

            if (!roomReady)
            {
                Debug.LogError(
                    "Quest round could not prepare the room.",
                    this);
                enabled = false;
                return;
            }

            SetPotionAvailability(
                _roundPuzzles[roundIndex]);
            RoundStarted?.Invoke(_currentRound);
        }

        private void HandleExternalSessionReset()
        {
            if (_isChangingRound ||
                !_started)
            {
                return;
            }

            StopPendingRoutines();

            if (_isSelectingDifficulty ||
                !HasActiveRound)
            {
                EnterDifficultySelection();
                return;
            }

            _hintUsedThisRound = false;
            _roundResolved = false;
            _latestRoundResult = null;
            SetPotionAvailability(
                _roundPuzzles[_currentRoundIndex]);
            RoundStarted?.Invoke(_currentRound);
        }

        private void EnterDifficultySelection()
        {
            StopPendingRoutines();

            _isSelectingDifficulty = true;
            _currentRoundIndex = -1;
            _currentRound = null;
            _hintUsedThisRound = false;
            _roundResolved = false;
            _latestRoundResult = null;

            _isChangingRound = true;
            bool selectionReady =
                _gameSession.TryEnterSelectionMode();
            _isChangingRound = false;

            if (!selectionReady)
            {
                Debug.LogError(
                    "Difficulty selection could not lock the cauldron.",
                    this);
                enabled = false;
                return;
            }

            SetAllPotionsInactive();
            DifficultySelectionRequested?.Invoke();
        }

        private bool AreAllDifficultiesCompleted()
        {
            if (_roundCompleted.Length != RoundCount)
            {
                return false;
            }

            for (int index = 0;
                 index < _roundCompleted.Length;
                 index++)
            {
                if (!_roundCompleted[index])
                {
                    return false;
                }
            }

            return true;
        }

        private PotionController FindAvailableOptimalPotion()
        {
            PuzzleSolution solution =
                _gameSession.OptimalSolution;
            if (solution == null)
            {
                return null;
            }

            IReadOnlyList<string> optimalIds =
                solution.SelectedPotionIds;

            for (int idIndex = 0;
                 idIndex < optimalIds.Count;
                 idIndex++)
            {
                string stableId = optimalIds[idIndex];

                for (int potionIndex = 0;
                     potionIndex < _potions.Length;
                     potionIndex++)
                {
                    PotionController potion =
                        _potions[potionIndex];
                    if (potion == null ||
                        !potion.gameObject.activeInHierarchy ||
                        !potion.IsAvailable ||
                        potion.Definition == null)
                    {
                        continue;
                    }

                    if (string.Equals(
                        potion.Definition.StableId,
                        stableId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return potion;
                    }
                }
            }

            return null;
        }

        private void SetPotionAvailability(
            PuzzleDefinition puzzle)
        {
            var availableDefinitions =
                new HashSet<PotionDefinition>(
                    puzzle.Potions);

            for (int index = 0;
                 index < _potions.Length;
                 index++)
            {
                PotionController potion = _potions[index];
                if (potion == null)
                {
                    continue;
                }

                bool shouldBeActive =
                    potion.Definition != null &&
                    availableDefinitions.Contains(
                        potion.Definition);

                if (potion.gameObject.activeSelf !=
                    shouldBeActive)
                {
                    potion.gameObject.SetActive(
                        shouldBeActive);
                }
            }
        }

        private void SetAllPotionsInactive()
        {
            for (int index = 0;
                 index < _potions.Length;
                 index++)
            {
                PotionController potion = _potions[index];
                if (potion != null &&
                    potion.gameObject.activeSelf)
                {
                    potion.gameObject.SetActive(false);
                }
            }
        }

        private bool ValidateConfiguration(
            out string error)
        {
            if (_gameSession == null || _roomReset == null)
            {
                error = "game session or room reset reference is missing.";
                return false;
            }

            if (_roundPuzzles == null ||
                _roundPuzzles.Length != 3)
            {
                error = "exactly three quest puzzles are required.";
                return false;
            }

            if (_roundTitles == null ||
                _roundTitles.Length != _roundPuzzles.Length)
            {
                error = "every quest puzzle needs a title.";
                return false;
            }

            if (_potions == null || _potions.Length == 0)
            {
                error = "no scene potions are configured.";
                return false;
            }

            var availableDefinitions =
                new HashSet<PotionDefinition>();
            for (int index = 0;
                 index < _potions.Length;
                 index++)
            {
                PotionController potion = _potions[index];
                if (potion == null || potion.Definition == null)
                {
                    error = "a configured potion has no definition.";
                    return false;
                }

                availableDefinitions.Add(potion.Definition);
            }

            for (int roundIndex = 0;
                 roundIndex < _roundPuzzles.Length;
                 roundIndex++)
            {
                PuzzleDefinition puzzle =
                    _roundPuzzles[roundIndex];
                if (puzzle == null)
                {
                    error = "quest puzzle " + roundIndex +
                        " is missing.";
                    return false;
                }

                if (!puzzle.TryValidate(
                    out string puzzleError))
                {
                    error = "quest puzzle " + roundIndex +
                        " is invalid: " + puzzleError;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                    _roundTitles[roundIndex]))
                {
                    error = "quest title " + roundIndex +
                        " is empty.";
                    return false;
                }

                for (int potionIndex = 0;
                     potionIndex < puzzle.Potions.Count;
                     potionIndex++)
                {
                    if (!availableDefinitions.Contains(
                        puzzle.Potions[potionIndex]))
                    {
                        error = "quest puzzle " + roundIndex +
                            " references a potion missing from the scene.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private void StopPendingRoutines()
        {
            if (_selectionRoutine != null)
            {
                StopCoroutine(_selectionRoutine);
                _selectionRoutine = null;
            }
        }
    }
}
