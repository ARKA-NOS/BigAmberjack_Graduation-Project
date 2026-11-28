using System.Collections.Generic;
using _Scripts._Core._Manager;
using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Managers
{
    [DefaultExecutionOrder(-15), DisallowMultipleComponent]
    public sealed class AttackTokenManager : AbstractManager
    {
        [SerializeField, Min(0)] private int maxToken = 2;
        [Tooltip("큐에 머무를 수 있는 게임 시간입니다. 시간 정지·일시정지의 영향을 받습니다.")]
        [SerializeField, Min(0.01f)] private float waitTimeout = 2f;

        private sealed class Waiter
        {
            public EnemyAttackTokenModule Requester;
            public float ExpiresAt;
        }

        private readonly HashSet<EnemyAttackTokenModule> _holders = new HashSet<EnemyAttackTokenModule>();
        private readonly LinkedList<Waiter> _queue = new LinkedList<Waiter>();
        private readonly Dictionary<EnemyAttackTokenModule, LinkedListNode<Waiter>> _waiting =
            new Dictionary<EnemyAttackTokenModule, LinkedListNode<Waiter>>();
        private readonly List<EnemyAttackTokenModule> _invalidHolders = new List<EnemyAttackTokenModule>();

        public int MaxToken => Mathf.Max(0, maxToken);
        public int ActiveTokenCount => _holders.Count;
        public int WaitingCount => _waiting.Count;
        private bool IsRegistered => ServiceLocator.Get<AttackTokenManager>() == this;

        private void OnEnable() => Initialize();

        public override void Initialize()
        {
            if (!isActiveAndEnabled || IsRegistered) return;

            AttackTokenManager current = ServiceLocator.Get<AttackTokenManager>();
            if (current != null && current.isActiveAndEnabled)
            {
                Debug.LogWarning("AttackTokenManager는 하나만 활성화할 수 있습니다.", this);
                enabled = false;

                return;
            }

            ServiceLocator.Register(this);
        }

        internal bool TryAcquire(EnemyAttackTokenModule requester)
        {
            if (!IsRegistered || requester == null || !requester.IsEligible) return false;
            if (_holders.Contains(requester)) return true;

            Prune();
            if (!requester.IsEligible) return false;
            if (!_waiting.ContainsKey(requester))
                _waiting.Add(requester, _queue.AddLast(new Waiter { Requester = requester,
                    ExpiresAt = Time.time + Mathf.Max(0.01f, waitTimeout) }));

            GrantWaiting();
            return _holders.Contains(requester);
        }

        internal void Release(EnemyAttackTokenModule requester)
        {
            if (requester == null) return;

            _holders.Remove(requester);
            if (_waiting.TryGetValue(requester, out LinkedListNode<Waiter> node)) RemoveWaiter(node);

            if (IsRegistered)
            {
                Prune();
                GrantWaiting();
            }
        }

        private void Update()
        {
            if (!IsRegistered) return;

            Prune();
            GrantWaiting();
        }

        private void Prune()
        {
            _invalidHolders.Clear();
            foreach (EnemyAttackTokenModule holder in _holders)
                if (holder == null || !holder.CanRetainToken) _invalidHolders.Add(holder);

            foreach (EnemyAttackTokenModule holder in _invalidHolders)
            {
                _holders.Remove(holder);
                if (holder != null) holder.OnTokenReleased();
            }

            LinkedListNode<Waiter> node = _queue.First;
            while (node != null)
            {
                LinkedListNode<Waiter> next = node.Next;
                EnemyAttackTokenModule requester = node.Value.Requester;
                bool expired = Time.time >= node.Value.ExpiresAt;
                if (expired || requester == null || !requester.IsEligible)
                {
                    RemoveWaiter(node);
                    if (requester != null) requester.OnQueueRemoved(expired);
                }
                node = next;
            }
        }

        private void GrantWaiting()
        {
            while (_holders.Count < MaxToken && _queue.First != null)
            {
                LinkedListNode<Waiter> node = _queue.First;
                EnemyAttackTokenModule requester = node.Value.Requester;
                RemoveWaiter(node);
                if (requester == null || !requester.IsEligible)
                {
                    if (requester != null) requester.OnQueueRemoved(false);
                    continue;
                }

                _holders.Add(requester);
                requester.OnTokenGranted(this);
            }
        }

        private void RemoveWaiter(LinkedListNode<Waiter> node)
        {
            _waiting.Remove(node.Value.Requester);
            _queue.Remove(node);
        }

        public override void GameEnd() => ReleaseAll();

        private void OnDisable() => ReleaseAll();

        private void ReleaseAll()
        {
            if (IsRegistered) ServiceLocator.Register<AttackTokenManager>(null);
            _invalidHolders.Clear();
            _invalidHolders.AddRange(_holders);
            foreach (Waiter waiter in _queue)
                if (waiter.Requester != null) waiter.Requester.OnQueueRemoved(false);

            _holders.Clear();
            _waiting.Clear();
            _queue.Clear();

            // 스킬 취소의 반환 콜백이 큐를 수정해도 컬렉션 순회가 깨지지 않도록 비운 뒤 알립니다.
            foreach (EnemyAttackTokenModule holder in _invalidHolders)
                if (holder != null) holder.OnManagerStopped();
            _invalidHolders.Clear();
        }
    }
}
