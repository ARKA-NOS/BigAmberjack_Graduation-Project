using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 미니맵 ViewModel. 공간이 바뀌면 지형을 한 번 구워 Room에 넣고, 매 프레임(Tick) 플레이어 위치를 갱신한다.
    // 안개를 쓰는 공간은 플레이어 주변만 텍스처에 드러난다. 텍스처를 제자리에서 고치므로 Room은 다시 알리지 않는다.
    [CreateAssetMenu(fileName = "Minimap View Model", menuName = "UI/MVVM/Minimap View Model")]
    public class MinimapViewModel : ViewModelBase<IMinimapSource>, ITickableViewModel
    {
        [Header("Terrain")]
        [Tooltip("월드 1유닛(타일 1칸)을 텍스처 몇 픽셀로 칠할지.")]
        [SerializeField, Min(1)] private int pixelsPerUnit = 1;
        [SerializeField] private Color terrainColor = new(1f, 1f, 1f, 0.85f);

        [Header("Fog Of War")]
        [Tooltip("플레이어 주변을 밝히는 반경(월드 유닛).")]
        [SerializeField, Min(1f)] private float revealRadius = 12f;

        private readonly Observable<MinimapData> _room = new();
        private readonly Observable<bool> _hasPlayer = new();
        private readonly Observable<Vector2> _playerPosition = new();
        private readonly ObservableList<MinimapMarker> _markers = new();

        private IMinimapSource _source;
        private Texture2D _texture;
        private MinimapFog _fog;

        // 지금 공간의 지형. null이면 미니맵을 숨긴다(맵 생성 중, 연결 전).
        public IReadOnlyObservable<MinimapData> Room => _room;
        public IReadOnlyObservable<bool> HasPlayer => _hasPlayer;
        // 플레이어의 월드 좌표.
        public IReadOnlyObservable<Vector2> PlayerPosition => _playerPosition;
        // 아이콘 목록(월드 좌표). 새 맵에는 아직 표시할 대상이 없어 비어 있다.
        public IReadOnlyObservableList<MinimapMarker> Markers => _markers;

        protected override void OnConnect(IMinimapSource source)
        {
            _source = source;
            _source.AreaChanged += HandleAreaChanged;

            HandleAreaChanged();
            Tick();
        }

        protected override void OnDisconnect()
        {
            _source.AreaChanged -= HandleAreaChanged;
            _source = null;
        }

        protected override void ResetState()
        {
            ReleaseTerrain();
            _hasPlayer.Value = false;
            _playerPosition.Value = Vector2.zero;
            _markers.Clear();
        }

        public void Tick()
        {
            if (_source == null) return;

            Transform player = _source.Player;
            _hasPlayer.Value = player != null;
            if (player == null) return;

            _playerPosition.Value = player.position;
            RevealAround(player.position);
        }

        // 맵 생성 이벤트 처리 중에도 불리며 그때 플레이어는 아직 이전 위치일 수 있으므로, 여기서는 밝히지 않고 Tick에 맡긴다.
        private void HandleAreaChanged()
        {
            ReleaseTerrain();

            MinimapArea area = _source.CurrentArea;
            if (area == null || !MinimapTerrainBaker.TryGetWorldRect(area.Tilemaps, out Rect worldRect))
                return;

            int width = Mathf.Max(1, Mathf.CeilToInt(worldRect.width * pixelsPerUnit));
            int height = Mathf.Max(1, Mathf.CeilToInt(worldRect.height * pixelsPerUnit));
            Color32[] terrain = MinimapTerrainBaker.Paint(area.Tilemaps, worldRect, width, height, pixelsPerUnit, terrainColor);

            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Minimap Terrain",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            if (area.UsesFog)
            {
                _fog = new MinimapFog(terrain, _texture);
            }
            else
            {
                _texture.SetPixels32(terrain);
                _texture.Apply(false, true);
            }

            _room.Value = new MinimapData(_texture, worldRect);
        }

        private void RevealAround(Vector2 position)
        {
            if (_fog == null) return;

            Vector2 local = (position - _room.Value.WorldRect.min) * pixelsPerUnit;
            _fog.Reveal(Vector2Int.FloorToInt(local), Mathf.CeilToInt(revealRadius * pixelsPerUnit));
        }

        // 텍스처를 지우기 전에 화면이 먼저 놓도록 Room부터 비운다.
        private void ReleaseTerrain()
        {
            _fog = null;
            _room.Value = null;

            if (_texture != null)
                Destroy(_texture);

            _texture = null;
        }
    }
}
