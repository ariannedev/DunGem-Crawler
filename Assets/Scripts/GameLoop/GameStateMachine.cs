using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGemCrawler
{
    public class GameStateMachine
    {
        private readonly BoardManager _board;
        private readonly Dictionary<Type, GameStateBase> _states = new();
        private GameStateBase _current;

        public GameStateMachine(BoardManager board)
        {
            _board = board;
        }

        public void Register(GameStateBase state)
        {
            state.Setup(this, _board);
            _states[state.GetType()] = state;
        }

        public void Enter<T>() where T : GameStateBase
        {
            _current?.OnExit();
            _current = _states[typeof(T)];
            _current.OnEnter();
        }

        public void Tick() => _current?.Tick();
    }
}
