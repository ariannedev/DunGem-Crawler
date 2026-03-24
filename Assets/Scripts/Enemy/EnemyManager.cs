using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DunGemCrawler
{
    public class EnemyManager
    {
        private readonly BoardData     _data;
        private readonly EnemyConfig[] _pool;
        private readonly Dictionary<Vector2Int, EnemyData> _enemies = new();

        public event Action<int> OnEnemyKilled;

        public EnemyManager(BoardData data, EnemyConfig[] pool = null)
        {
            _data = data;
            _pool = pool;
        }

        public bool HasEnemy(Vector2Int cell) => _enemies.ContainsKey(cell);

        public EnemyData GetEnemy(Vector2Int cell)
        {
            _enemies.TryGetValue(cell, out var e);
            return e;
        }

        public IReadOnlyCollection<EnemyData> All => _enemies.Values;

        // ── Spawning ──────────────────────────────────────────────────────────────

        public void SpawnEnemy(int col, int row, Vector2 worldPos)
        {
            var cell = new Vector2Int(col, row);
            if (_enemies.ContainsKey(cell)) return;

            EnemyConfig config = PickConfig();

            EnemyView view = config?.Prefab != null
                ? UnityEngine.Object.Instantiate(config.Prefab, worldPos, Quaternion.identity)
                : new GameObject($"Enemy_{col}_{row}").AddComponent<EnemyView>();

            if (config?.Prefab == null)
                view.gameObject.transform.position = worldPos;

            var enemy = new EnemyData(col, row, config);
            enemy.View = view;
            view.SetHealth(enemy.Health, enemy.MaxHealth);

            _enemies[cell] = enemy;
            _data.ActiveEnemyCells.Add(cell);
        }

        private EnemyConfig PickConfig()
        {
            if (_pool == null || _pool.Length == 0) return null;

            float total = 0f;
            foreach (var c in _pool)
                if (c != null) total += Mathf.Max(0f, c.SpawnWeight);

            if (total <= 0f) return _pool[0];

            float roll = Random.Range(0f, total);
            foreach (var c in _pool)
            {
                if (c == null) continue;
                float w = Mathf.Max(0f, c.SpawnWeight);
                if (roll < w) return c;
                roll -= w;
            }
            return _pool[_pool.Length - 1];
        }

        // ── Damage / attack ───────────────────────────────────────────────────────

        public bool DamageEnemy(Vector2Int cell, int amount)
        {
            if (!_enemies.TryGetValue(cell, out var enemy)) return false;

            enemy.Health = Mathf.Max(0, enemy.Health - amount);
            enemy.View?.SetHealth(enemy.Health, enemy.MaxHealth);
            enemy.View?.FlashDamage();

            if (enemy.Health <= 0)
            {
                KillEnemy(cell, enemy);
                return true;
            }
            return false;
        }

        public int AttackPlayer(Vector2Int playerCell)
        {
            int totalDamage = 0;
            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var adj = playerCell + dir;
                if (_enemies.TryGetValue(adj, out var enemy))
                    totalDamage += enemy.AttackDamage;
            }
            return totalDamage;
        }

        // ── Skills ────────────────────────────────────────────────────────────────

        public IEnumerator ExecuteAllSkills(BoardManager board)
        {
            // Snapshot — skills may modify the dictionary (Charger moves)
            var snapshot = new List<EnemyData>(_enemies.Values);
            foreach (var enemy in snapshot)
            {
                if (!_enemies.ContainsKey(enemy.Cell)) continue; // killed this turn

                switch (enemy.Type)
                {
                    case EnemyType.Mixer:
                        yield return board.StartCoroutine(MixerSkill(enemy, board));
                        break;
                    case EnemyType.Freezer:
                        yield return board.StartCoroutine(FreezerSkill(enemy, board));
                        break;
                    case EnemyType.Charger:
                        yield return board.StartCoroutine(ChargerSkill(enemy, board));
                        break;
                }
            }
        }

        // Mixer — swaps MixSwapCount random gem pairs
        private IEnumerator MixerSkill(EnemyData enemy, BoardManager board)
        {
            int swaps = enemy.Config?.MixSwapCount ?? 3;
            var candidates = GetSwappableGemCells(board.Data);

            for (int i = 0; i < swaps && candidates.Count >= 2; i++)
            {
                int idxA = Random.Range(0, candidates.Count);
                Vector2Int cellA = candidates[idxA];
                candidates.RemoveAt(idxA);

                int idxB = Random.Range(0, candidates.Count);
                Vector2Int cellB = candidates[idxB];
                candidates.RemoveAt(idxB);

                board.Data.SwapGems(cellA, cellB);

                // Snap views to their new positions
                GemData gA = board.Data.GetGem(cellA);
                GemData gB = board.Data.GetGem(cellB);
                if (gA?.View != null) gA.View.transform.position = board.GridToWorld(cellA);
                if (gB?.View != null) gB.View.transform.position = board.GridToWorld(cellB);
            }

            yield break;
        }

        private List<Vector2Int> GetSwappableGemCells(BoardData data)
        {
            var list = new List<Vector2Int>();
            for (int col = 0; col < data.Columns; col++)
                for (int row = 0; row < data.Rows; row++)
                {
                    var cell = new Vector2Int(col, row);
                    if (data.GetGem(cell) != null && !data.IsFrozenCell(cell)
                        && !data.IsPlayerCell(cell) && !data.IsEnemyCell(cell))
                        list.Add(cell);
                }
            return list;
        }

        // Freezer — freezes FreezeCount random non-frozen gems
        private IEnumerator FreezerSkill(EnemyData enemy, BoardManager board)
        {
            int count  = enemy.Config?.FreezeCount     ?? 1;
            int layers = enemy.Config?.FreezeIceLayers ?? 1;

            var candidates = GetSwappableGemCells(board.Data); // non-frozen, non-null gems

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int idx  = Random.Range(0, candidates.Count);
                Vector2Int cell = candidates[idx];
                candidates.RemoveAt(idx);

                GemData gem = board.Data.GetGem(cell);
                if (gem == null) continue;

                gem.Modifier = new GemModifier(GemModifierType.Frozen, layers);
                board.Data.FrozenCells.Add(cell);
                gem.View?.SetFrozen(layers);
            }

            yield break;
        }

        // Charger — moves one step toward player; kills player on contact
        private IEnumerator ChargerSkill(EnemyData enemy, BoardManager board)
        {
            Vector2Int from   = enemy.Cell;
            Vector2Int? toOpt = GetChargerTarget(enemy, board);
            if (toOpt == null) yield break;

            Vector2Int to = toOpt.Value;

            // Contact kill
            if (to == board.Data.PlayerCell)
            {
                board.Player.TakeDamage(board.Player.Data.MaxHealth);
                yield break;
            }

            // Displace any gem in the target cell back to the charger's old cell
            GemData displaced = board.Data.GetGem(to);
            board.Data.SetGem(to, null);
            if (displaced != null)
            {
                board.Data.SetGem(from, displaced);
                if (displaced.View != null)
                    displaced.View.transform.position = board.GridToWorld(from);
            }
            else
            {
                board.Data.SetGem(from, null);
            }

            // Move enemy in data
            MoveEnemy(from, to);

            // Animate enemy view
            if (enemy.View != null)
                yield return board.StartCoroutine(
                    enemy.View.MoveTo(board.GridToWorld(to), 6f));
        }

        private Vector2Int? GetChargerTarget(EnemyData enemy, BoardManager board)
        {
            Vector2Int delta = board.Data.PlayerCell - enemy.Cell;

            Vector2Int primary, secondary;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                primary   = new Vector2Int((int)Mathf.Sign(delta.x), 0);
                secondary = delta.y != 0
                    ? new Vector2Int(0, (int)Mathf.Sign(delta.y))
                    : Vector2Int.zero;
            }
            else
            {
                primary   = new Vector2Int(0, (int)Mathf.Sign(delta.y));
                secondary = delta.x != 0
                    ? new Vector2Int((int)Mathf.Sign(delta.x), 0)
                    : Vector2Int.zero;
            }

            foreach (var dir in new[] { primary, secondary })
            {
                if (dir == Vector2Int.zero) continue;
                Vector2Int target = enemy.Cell + dir;
                if (!board.Data.InBounds(target))         continue;
                if (board.Data.IsEnemyCell(target))       continue; // blocked by another enemy
                if (board.Data.IsFrozenCell(target))      continue; // frozen gems block movement
                return target; // includes player cell (handled by caller)
            }
            return null;
        }

        private void MoveEnemy(Vector2Int from, Vector2Int to)
        {
            if (!_enemies.TryGetValue(from, out var enemy)) return;
            _enemies.Remove(from);
            enemy.Col = to.x;
            enemy.Row = to.y;
            _enemies[to] = enemy;
            _data.ActiveEnemyCells.Remove(from);
            _data.ActiveEnemyCells.Add(to);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────

        public void ClearAll()
        {
            foreach (var enemy in _enemies.Values)
                if (enemy.View != null)
                    UnityEngine.Object.Destroy(enemy.View.gameObject);

            _enemies.Clear();
            _data.ActiveEnemyCells.Clear();
        }

        private void KillEnemy(Vector2Int cell, EnemyData enemy)
        {
            if (enemy.View != null)
                UnityEngine.Object.Destroy(enemy.View.gameObject);

            _enemies.Remove(cell);
            _data.ActiveEnemyCells.Remove(cell);
            OnEnemyKilled?.Invoke(500);
        }
    }
}
