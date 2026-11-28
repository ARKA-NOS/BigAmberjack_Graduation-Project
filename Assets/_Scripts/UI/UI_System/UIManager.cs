using System;
using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.UI.MVVM.Ticking;
using UnityEngine;

namespace _Scripts.UI.UI_System
{
    public class UIManager : AbstractManager, IUITicker, IUIWindowState, IUIWindowLock
    {
        [SerializeField] private PlayerInputSo inputSo;

        public override void Initialize()
        {
            EventBus<UIOpenCloseEvent>.Subscribe(OpenClose);
            ServiceLocator.Register<IUITicker>(this);
            ServiceLocator.Register<IUIWindowState>(this);
            ServiceLocator.Register<IUIWindowLock>(this);
        }

        public override void GameEnd()
        {
            EventBus<UIOpenCloseEvent>.UnSubscribe(OpenClose);
            _tickables.Clear();
            ServiceLocator.Register<IUITicker>(new NullUITicker());
            ServiceLocator.Register<IUIWindowState>(new NullUIWindowState());
            ServiceLocator.Register<IUIWindowLock>(new NullUIWindowLock());
        }

        private IWindow _currentWindow;
        private bool _isLocked;

        #region Window State

        public bool HasOpenWindow => _currentWindow != null;
        public int LastCloseFrame { get; private set; } = -1;

        #endregion

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (locked) CloseCurrentWindow();
        }

        private void CloseCurrentWindow()
        {
            if (_currentWindow == null) return;

            inputSo.SetPlayerInputActive(true);
            _currentWindow.Close();
            _currentWindow = null;
            LastCloseFrame = Time.frameCount;
        }

        private void OpenClose(UIOpenCloseEvent evt)
        {
            if (_isLocked) return;

            IWindow prevWindow = _currentWindow;
            CloseCurrentWindow();

            if (evt.Window == null || evt.Window == prevWindow) return;

            inputSo.SetPlayerInputActive(false);
            _currentWindow = evt.Window;
            _currentWindow.Open(evt.Data);
        }
        
        #region ViewModel Tick

        private readonly List<ITickableViewModel> _tickables = new();
        private readonly List<ITickableViewModel> _tickBuffer = new();

        public void Register(ITickableViewModel viewModel)
        {
            if (viewModel != null && !_tickables.Contains(viewModel))
                _tickables.Add(viewModel);
        }

        public void Unregister(ITickableViewModel viewModel)
            => _tickables.Remove(viewModel);

        private void Update()
        {
            if (_tickables.Count == 0) return;

            // Tick 안에서 등록/해제가 일어나도 순회가 깨지지 않게 복사본을 돈다.
            _tickBuffer.Clear();
            _tickBuffer.AddRange(_tickables);

            foreach (ITickableViewModel viewModel in _tickBuffer)
            {
                // 이번 프레임에 먼저 Tick된 ViewModel이 해제시킨 ViewModel은 건너뛴다.
                if (!_tickables.Contains(viewModel)) continue;

                try
                {
                    viewModel.Tick();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        #endregion

        
    }
}