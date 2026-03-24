using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DunGemCrawler
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private LevelProgression progression;

        [Header("Prefabs")]
        [SerializeField] private DungeonTileView dungeonTileViewPrefab;
        [SerializeField] private PlayerView      playerViewPrefab;

        [Header("References")]
        [SerializeField] private GemPool gemPool;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private GameUI gameUI;

        // Public for states
        public LevelProgression Progression  => progression;
        public BoardData       Data          { get; private set; }
        public LevelConfig     CurrentLevel  { get; private set; }
        public LevelConfig     Config        => CurrentLevel;  // convenience alias used by states
        public Vector2         BoardOrigin   { get; private set; }
        public GemPool         Pool          => gemPool;
        public InputHandler    Input         => inputHandler;
        public MatchDetector   Detector      { get; private set; }
        public GravityResolver Gravity       { get; private set; }
        public DungeonLayer    Dungeon       { get; private set; }
        public PlayerController Player       { get; private set; }
        public EnemyManager    Enemies       { get; private set; }

        // Exposed for RoomExitState
        public Transform       TileParent       { get; private set; }
        public DungeonTileView DungeonTilePrefab => dungeonTileViewPrefab;
        public int             FloorNumber       { get; set; } = 1;
        public int             Score             { get; set; } = 0;

        // State machine blackboard
        public Vector2Int          PendingSwapA;
        public Vector2Int          PendingSwapB;
        public List<Vector2Int>    PendingRemovals    = new();
        public bool                HasPendingPlayerMove;
        public Vector2Int          PendingPlayerMove;
        public int                 CascadeDepth;
        public HashSet<Vector2Int> LineClearBothCells = new();

        private GameStateMachine _fsm;
        private Action<int>      _onEnemyKilledHandler;

        private void Awake()
        {
            if (progression == null)
            {
                Debug.LogError("[BoardManager] LevelProgression is not assigned. Assign it in the Inspector.", this);
                return;
            }
            if (gemPool == null || inputHandler == null || dungeonTileViewPrefab == null || playerViewPrefab == null)
            {
                Debug.LogError("[BoardManager] One or more required Inspector references are missing.", this);
                return;
            }

            CurrentLevel = progression.Get(FloorNumber);

            BoardOrigin = CalcBoardOrigin(CurrentLevel);
            gemPool.EnsureInitialized();

            Data = new BoardData(CurrentLevel.Columns, CurrentLevel.Rows);

            var playerData       = new PlayerData(new Vector2Int(CurrentLevel.Columns / 2, CurrentLevel.Rows - 1));
            Vector2 playerStart  = GridToWorld(playerData.Cell);
            var playerViewGO     = Instantiate(playerViewPrefab, playerStart, Quaternion.identity);
            Player = new PlayerController(playerData, playerViewGO);
            Player.OnDeath += HandlePlayerDeath;

            Detector = new MatchDetector();
            Gravity  = new GravityResolver();

            _onEnemyKilledHandler = bonus => { Score += bonus; RefreshUI(); };

            Dungeon = new DungeonLayer(Data, CurrentLevel.FlashDuration, this);
            Dungeon.OnTileRevealed += HandleTileRevealed;

            Enemies = new EnemyManager(Data, CurrentLevel.EnemyPool);
            Enemies.OnEnemyKilled += _onEnemyKilledHandler;

            TileParent = new GameObject("DungeonTiles").transform;
            var initializer = new BoardInitializer();
            initializer.Initialize(Data, CurrentLevel, gemPool, dungeonTileViewPrefab,
                TileParent, BoardOrigin, playerData);

            inputHandler.Initialize(this);

            _fsm = new GameStateMachine(this);
            _fsm.Register(new IdleState());
            _fsm.Register(new SwapState());
            _fsm.Register(new MatchCheckState());
            _fsm.Register(new RemovalState());
            _fsm.Register(new GravityState());
            _fsm.Register(new CascadeCheckState());
            _fsm.Register(new PlayerMoveState());
            _fsm.Register(new RoomExitState());
            _fsm.Register(new EnemyAttackState());
            _fsm.Register(new ObjectiveCheckState());
        }

        private void Start()
        {
            RefreshUI();
            _fsm.Enter<IdleState>();
        }

        private void Update()
        {
            _fsm?.Tick();
        }

        // ── Level loading ─────────────────────────────────────────────────────────

        // Called by RoomExitState to rebuild systems for a new level.
        public void LoadLevel(LevelConfig level)
        {
            CurrentLevel = level;
            BoardOrigin  = CalcBoardOrigin(level);

            // Unsubscribe from old systems before replacing them
            if (Dungeon  != null) Dungeon.OnTileRevealed  -= HandleTileRevealed;
            if (Enemies  != null) Enemies.OnEnemyKilled   -= _onEnemyKilledHandler;

            Data    = new BoardData(level.Columns, level.Rows);
            Dungeon = new DungeonLayer(Data, level.FlashDuration, this);
            Dungeon.OnTileRevealed += HandleTileRevealed;

            Enemies = new EnemyManager(Data, CurrentLevel.EnemyPool);
            Enemies.OnEnemyKilled += _onEnemyKilledHandler;
        }

        private static Vector2 CalcBoardOrigin(LevelConfig level) =>
            new Vector2(
                -(level.Columns * level.CellSize) / 2f,
                -(level.Rows    * level.CellSize) / 2f);

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleTileRevealed(TileType type, int col, int row)
        {
            switch (type)
            {
                case TileType.Enemy:
                    Enemies.SpawnEnemy(col, row, GridToWorld(col, row));
                    break;
                case TileType.Treasure:
                    Player.Heal(2);
                    Score += 100;
                    RefreshUI();
                    break;
                case TileType.Goal:
                    // Visible from start; objective tracked in ObjectiveCheckState
                    break;
            }
        }

        private void HandlePlayerDeath()
        {
            Debug.Log("[DunGemCrawler] Game Over — reloading scene");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ── UI / Grid helpers ─────────────────────────────────────────────────────

        public void RefreshUI()
        {
            if (gameUI == null) return;
            gameUI.UpdateHealth(Player.Data.Health, Player.Data.MaxHealth);
            gameUI.UpdateFloor(FloorNumber);
            gameUI.UpdateScore(Score);
        }

        public Vector2 GridToWorld(int col, int row) =>
            GridUtils.GridToWorld(col, row, BoardOrigin, CurrentLevel.CellSize);

        public Vector2 GridToWorld(Vector2Int cell) =>
            GridToWorld(cell.x, cell.y);

        public Vector2Int WorldToGrid(Vector2 worldPos) =>
            GridUtils.WorldToGrid(worldPos, BoardOrigin, CurrentLevel.CellSize);
    }
}
