using System;
using System.Collections;
using System.Collections.Generic;
using CTJ.Enemies;
using Lrw.Script.Agent.Players;
using UnityEngine;

namespace SDW.Scripts.Maps.Encounter
{
    // 방 프리팹 안의 전투 구역. 플레이어가 트리거에 처음 들어오면 배리어로 근처를 막고 EncounterSO의 웨이브를 차례로 생성한다.
    // 웨이브의 적이 모두 죽으면 다음 웨이브로 넘어가고, 마지막 웨이브까지 끝나면 배리어를 열고 Cleared를 알린다.
    // 생성한 적은 방에 등록되므로, 주변 상자/NPC의 InteractionLock이 이 적들을 보고 잠긴다.
    [RequireComponent(typeof(Collider2D))]
    public class EncounterZone : MonoBehaviour
    {
        private const float WaveCheckInterval = 0.2f;

        public enum State
        {
            Idle,
            Active,
            Cleared
        }

        [SerializeField] private EncounterSO encounter;
        [Tooltip("이 구역의 적이 생성될 위치")]
        [SerializeField] private List<Transform> spawnPoints = new();
        [Tooltip("전투 중에만 켜져서 길을 막는 오브젝트. 평소에는 꺼져 있다. 비워두면 막지 않는다.")]
        [SerializeField] private List<GameObject> barriers = new();

        private RoomController _room;
        private Collider2D _col;

        public State CurrentState { get; private set; } = State.Idle;

        public event Action Cleared;

        private void Awake()
        {
            _room = GetComponentInParent<RoomController>();

            _col = GetComponent<Collider2D>();
            Debug.Assert(_col.isTrigger, $"{name} : 전투 구역 콜라이더는 Trigger여야 합니다.");

            SetBarriers(false);
        }

        // 웨이브형 전투가 진행 중이면, 구역에서 radius 안에 있는 상호작용을 마지막 웨이브가 끝날 때까지 잠근다.
        // (웨이브 사이에 적이 하나도 없는 순간에도 열리지 않게 하기 위함)
        public bool LocksInteractionAt(Vector2 position, float radius)
        {
            if (CurrentState != State.Active || encounter == null || !encounter.IsWave)
                return false;

            return _col.bounds.SqrDistance(position) <= radius * radius;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (CurrentState != State.Idle)
                return;

            if (other.GetComponentInParent<PlayerController>() == null)
                return;

            Begin();
        }

        private void Begin()
        {
            if (_room == null || encounter == null || encounter.Waves.Count == 0)
            {
                Debug.LogWarning($"{name} : 방 또는 Encounter가 없어 전투를 시작하지 않습니다.");
                CurrentState = State.Cleared;
                return;
            }

            CurrentState = State.Active;
            SetBarriers(true);
            StartCoroutine(RunWaves());
        }

        // TODO: 시간 기반 웨이브 등 진행 규칙이 늘어나면 이 코루틴을 전략 객체로 분리한다.
        private IEnumerator RunWaves()
        {
            WaitForSeconds interval = new WaitForSeconds(WaveCheckInterval);

            foreach (EncounterWave wave in encounter.Waves)
            {
                if (wave == null)
                    continue;

                if (wave.StartDelay > 0f)
                    yield return new WaitForSeconds(wave.StartDelay);

                List<EnemyBase> spawned = EnemySpawnUtility.SpawnEnemies(_room, spawnPoints, wave.Enemies,
                    encounter.SameSpawnPointSpacing);

                while (HasAlive(spawned))
                    yield return interval;
            }

            Finish();
        }

        private void Finish()
        {
            CurrentState = State.Cleared;
            SetBarriers(false);
            Cleared?.Invoke();
        }

        private static bool HasAlive(List<EnemyBase> enemies)
        {
            foreach (EnemyBase enemy in enemies)
            {
                if (RoomController.IsAlive(enemy))
                    return true;
            }

            return false;
        }

        private void SetBarriers(bool active)
        {
            foreach (GameObject barrier in barriers)
            {
                if (barrier != null)
                    barrier.SetActive(active);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);

            if (TryGetComponent(out BoxCollider2D box))
                Gizmos.DrawWireCube(transform.TransformPoint(box.offset), Vector3.Scale(box.size, transform.lossyScale));

            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.4f);
            }
        }
    }
}
