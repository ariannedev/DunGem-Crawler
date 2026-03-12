using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DunGemCrawler
{
    public class InputHandler : MonoBehaviour
    {
        public event Action<Vector2Int> OnCellPressed;
        public event Action<Vector2Int, Vector2Int> OnSwapRequested;

        private BoardManager _board;
        private Vector2Int _selectedCell;
        private bool _hasSelection;
        private Vector2 _pressWorldPos;
        private bool _pressing;

        private InputAction _clickAction;
        private InputAction _positionAction;

        private const float DragThreshold = 0.35f;

        public void Initialize(BoardManager board)
        {
            _board = board;
        }

        private void OnEnable()
        {
            _clickAction = new InputAction("Click", InputActionType.Button);
            _clickAction.AddBinding("<Mouse>/leftButton");
            _clickAction.AddBinding("<Touchscreen>/primaryTouch/tap");

            _positionAction = new InputAction("Position", InputActionType.Value,
                expectedControlType: "Vector2");
            _positionAction.AddBinding("<Mouse>/position");
            _positionAction.AddBinding("<Touchscreen>/primaryTouch/position");

            _clickAction.started  += OnClickStarted;
            _clickAction.canceled += OnClickCanceled;

            _clickAction.Enable();
            _positionAction.Enable();
        }

        private void OnDisable()
        {
            _clickAction.started  -= OnClickStarted;
            _clickAction.canceled -= OnClickCanceled;
            _clickAction.Disable();
            _positionAction.Disable();
        }

        private void OnClickStarted(InputAction.CallbackContext ctx)
        {
            if (_board == null) return;

            Vector2 screenPos = _positionAction.ReadValue<Vector2>();
            Vector2 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
            Vector2Int cell   = _board.WorldToGrid(worldPos);

            if (!_board.Data.InBounds(cell)) return;

            _pressWorldPos = worldPos;
            _pressing = true;

            if (!_hasSelection)
            {
                _selectedCell = cell;
                _hasSelection = true;
                OnCellPressed?.Invoke(cell);
            }
            else
            {
                if (GridUtils.IsAdjacent(_selectedCell, cell))
                    OnSwapRequested?.Invoke(_selectedCell, cell);
                _hasSelection = false;
                _pressing = false;
            }
        }

        private void OnClickCanceled(InputAction.CallbackContext ctx)
        {
            if (!_pressing) return;

            // Check for drag on release
            Vector2 screenPos   = _positionAction.ReadValue<Vector2>();
            Vector2 currentWorld = Camera.main.ScreenToWorldPoint(screenPos);
            Vector2 delta        = currentWorld - _pressWorldPos;

            if (_hasSelection && delta.magnitude >= DragThreshold)
            {
                Vector2Int dir;
                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                    dir = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
                else
                    dir = delta.y > 0 ? Vector2Int.up : Vector2Int.down;

                Vector2Int target = _selectedCell + dir;
                if (_board.Data.InBounds(target))
                    OnSwapRequested?.Invoke(_selectedCell, target);

                _hasSelection = false;
            }

            _pressing = false;
        }

        public void ClearSelection()
        {
            _hasSelection = false;
        }
    }
}
