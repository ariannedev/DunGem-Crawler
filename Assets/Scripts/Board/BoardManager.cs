using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BoardConfig config;

        [Header("Prefabs")]
        [SerializeField] private DungeonTileView dungeonTileViewPrefab;
        [SerializeField] private PlayerView playerViewPrefab;

        [Header("References")]
        [SerializeField] private GemPool gemPool;
        [SerializeField] private InputHandler inputHandler;

        // Public for states
        public BoardData Data { get; private set; }
        public BoardConfig Config => config;
        public Vector2 BoardOrigin { get; private set; }
        public GemPool Pool => gemPool;
        public InputHandler Input => inputHandler;
        public MatchDetector Detector { get; private set; }
        public GravityResolver Gravity { get; private set; }
        public DungeonLayer Dungeon { get; private set; }
        public PlayerController Player { get; private set; }

        // State machine blackboard
        public Vector2Int PendingSwapA;
        public Vector2Int PendingSwapB;
        public List<Vector2Int> PendingRemovals = new();
        public bool HasPendingPlayerMove;
        public Vector2Int PendingPlayerMove;
        public int CascadeDepth;

        private GameStateMachine _fsm;

        private void Awake()
        {
            BoardOrigin = new Vector2(
                -(config.Columns * config.CellSize) / 2f,
                -(config.Rows * config.CellSize) / 2f);

            gemPool.EnsureInitialized();

            Data = new BoardData(config.Columns, config.Rows);

            var playerData = new PlayerData(new Vector2Int(config.Columns / 2, config.Rows - 1));
            Vector2 playerStartWorld = GridToWorld(playerData.Cell);
            var playerViewGO = Instantiate(playerViewPrefab, playerStartWorld, Quaternion.identity);
            Player = new PlayerController(playerData, playerViewGO);

            Detector = new MatchDetector();
            Gravity = new GravityResolver();
            Dungeon = new DungeonLayer(Data, config.FlashDuration, this);

            var initializer = new BoardInitializer();
            Transform tileParent = new GameObject("DungeonTiles").transform;
            initializer.Initialize(Data, config, gemPool, dungeonTileViewPrefab,
                tileParent, BoardOrigin, playerData);

            inputHandler.Initialize(this);

            _fsm = new GameStateMachine(this);
            _fsm.Register(new IdleState());
            _fsm.Register(new SwapState());
            _fsm.Register(new MatchCheckState());
            _fsm.Register(new RemovalState());
            _fsm.Register(new GravityState());
            _fsm.Register(new CascadeCheckState());
            _fsm.Register(new PlayerMoveState());
        }

        private void Start()
        {
            _fsm.Enter<IdleState>();
        }

        private void Update()
        {
            _fsm.Tick();
        }

        public Vector2 GridToWorld(int col, int row) =>
            GridUtils.GridToWorld(col, row, BoardOrigin, config.CellSize);

        public Vector2 GridToWorld(Vector2Int cell) =>
            GridToWorld(cell.x, cell.y);

        public Vector2Int WorldToGrid(Vector2 worldPos) =>
            GridUtils.WorldToGrid(worldPos, BoardOrigin, config.CellSize);
    }
}
