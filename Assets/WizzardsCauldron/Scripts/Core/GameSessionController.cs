using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizzardsCauldron.Core
{
    public sealed class GameSessionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private PuzzleDefinition _puzzleDefinition;

        [SerializeField]
        private CauldronController _cauldron;

        [Header("Runtime State")]
        [SerializeField]
        private GameSessionState _state =
            GameSessionState.Playing;

        [SerializeField]
        private int _optimalHealth;

        [SerializeField]
        private int _optimalUsedCapacity;

        private PuzzleSolution _optimalSolution;
        private AttemptResult _latestResult;

        public GameSessionState State => _state;
        public bool IsPlaying =>
            _state == GameSessionState.Playing;

        public PuzzleSolution OptimalSolution =>
            _optimalSolution;

        public PuzzleDefinition PuzzleDefinition =>
            _puzzleDefinition;

        public AttemptResult LatestResult =>
            _latestResult;

        public event Action<AttemptResult>
            AttemptFinished;

        public event Action SessionReset;

        private void Awake()
        {
            InitializeSession();
        }

        public bool TryFinishAttempt(
            out AttemptResult result)
        {
            if (!IsPlaying ||
                _optimalSolution == null ||
                _cauldron == null)
            {
                result = _latestResult;
                return false;
            }

            _cauldron.Lock();

            result = AttemptResult.Create(
                _cauldron.TotalHealth,
                _cauldron.UsedCapacity,
                _cauldron.MaximumCapacity,
                _optimalSolution);

            _latestResult = result;
            _state = GameSessionState.Finished;

            AttemptFinished?.Invoke(result);
            return true;
        }

        public bool TryResetSession()
        {
            if (_optimalSolution == null ||
                _cauldron == null)
            {
                return false;
            }

            _latestResult = null;
            _state = GameSessionState.Playing;

            _cauldron.ResetCauldron();

            SessionReset?.Invoke();
            return true;
        }

        public bool TryStartSession(
            PuzzleDefinition puzzleDefinition)
        {
            if (!TryPreparePuzzle(
                puzzleDefinition,
                out PuzzleSolution solution))
            {
                return false;
            }

            _puzzleDefinition = puzzleDefinition;
            _optimalSolution = solution;
            _optimalHealth = solution.MaximumHealth;
            _optimalUsedCapacity = solution.UsedCapacity;
            _latestResult = null;
            _state = GameSessionState.Playing;

            _cauldron.Configure(puzzleDefinition);
            SessionReset?.Invoke();
            return true;
        }

        public bool TryEnterSelectionMode()
        {
            if (_cauldron == null)
            {
                return false;
            }

            _latestResult = null;
            _state = GameSessionState.AwaitingSelection;
            _cauldron.ResetCauldron();
            _cauldron.Lock();
            SessionReset?.Invoke();
            return true;
        }

        private void InitializeSession()
        {
            if (!TryPreparePuzzle(
                _puzzleDefinition,
                out _optimalSolution))
            {
                enabled = false;
                return;
            }

            _optimalHealth =
                _optimalSolution.MaximumHealth;

            _optimalUsedCapacity =
                _optimalSolution.UsedCapacity;

            _latestResult = null;
            _state = GameSessionState.Playing;

            _cauldron.Configure(_puzzleDefinition);
        }

        private bool TryPreparePuzzle(
            PuzzleDefinition puzzleDefinition,
            out PuzzleSolution solution)
        {
            solution = null;

            if (puzzleDefinition == null)
            {
                Debug.LogError(
                    "GameSessionController has no " +
                    "PuzzleDefinition.",
                    this);
                return false;
            }

            if (_cauldron == null)
            {
                Debug.LogError(
                    "GameSessionController has no " +
                    "CauldronController.",
                    this);
                return false;
            }

            if (!puzzleDefinition.TryValidate(
                out string validationError))
            {
                Debug.LogError(
                    $"Cannot start invalid puzzle: " +
                    $"{validationError}",
                    this);
                return false;
            }

            solution = SolvePuzzle(puzzleDefinition);
            return true;
        }

        private static PuzzleSolution SolvePuzzle(
            PuzzleDefinition puzzleDefinition)
        {
            IReadOnlyList<PotionDefinition>
                potionDefinitions =
                    puzzleDefinition.Potions;

            var items =
                new KnapsackItem[
                    potionDefinitions.Count];

            for (int index = 0;
                 index < potionDefinitions.Count;
                 index++)
            {
                PotionDefinition potion =
                    potionDefinitions[index];

                items[index] = new KnapsackItem(
                    potion.StableId,
                    potion.HealthValue,
                    potion.FillValue);
            }

            return PuzzleSolver.Solve(
                puzzleDefinition.CauldronCapacity,
                items);
        }

        [ContextMenu("Finish Attempt (Play Mode)")]
        private void FinishAttemptFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.Log(
                    "Finish Attempt is only available " +
                    "during Play Mode.",
                    this);

                return;
            }

            if (!TryFinishAttempt(
                out AttemptResult result))
            {
                Debug.Log(
                    "Finish request ignored because the " +
                    "attempt is already finished or the " +
                    "session is invalid.",
                    this);

                return;
            }

            Debug.Log(
                $"Attempt finished. " +
                $"Outcome: {result.Outcome}, " +
                $"player health: {result.PlayerHealth}, " +
                $"best possible: " +
                $"{result.BestPossibleHealth}, " +
                $"capacity: {result.UsedCapacity}/" +
                $"{result.MaximumCapacity}.",
                this);
        }
    }
}
