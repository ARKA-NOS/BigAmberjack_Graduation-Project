using UnityEngine;

namespace SDW.Scripts.Maps
{
    public class RoomNode
    {
        public readonly Vector2Int GridPosition;

        private readonly RoomNode[] _neighbors = new RoomNode[RoomDirectionExtensions.All.Length];

        public RoomNode Up    => GetNeighbor(RoomDirection.Up);
        public RoomNode Down  => GetNeighbor(RoomDirection.Down);
        public RoomNode Left  => GetNeighbor(RoomDirection.Left);
        public RoomNode Right => GetNeighbor(RoomDirection.Right);

        public RoomNode(Vector2Int gridPosition)
        {
            GridPosition = gridPosition;
        }

        public RoomNode GetNeighbor(RoomDirection direction)
        {
            return _neighbors[direction.Index()];
        }

        public void SetNeighbor(RoomDirection direction, RoomNode neighbor)
        {
            _neighbors[direction.Index()] = neighbor;
        }

        public RoomDirection GetConnectedDirections()
        {
            RoomDirection result = RoomDirection.None;

            foreach (RoomDirection direction in RoomDirectionExtensions.All)
            {
                if (GetNeighbor(direction) != null)
                    result |= direction;
            }

            return result;
        }
    }
}
