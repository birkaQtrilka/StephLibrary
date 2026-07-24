using System.Collections.Generic;
using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    [System.Serializable]
    public class AvailableTiles 
    {
        [SerializeField] private List<Tile> _tiles;
        [SerializeField] private int _socketLength = 1;

        public AvailableTiles(WorldGenConfig config)
        {
            _tiles = new (config.SocketsCount);
            _socketLength = config.SocketsCount;
        }

        public void AddNew()
        {
            _tiles ??= new List<Tile>();
            _tiles.Add(new Tile(new Sockets(_socketLength)));
        }

        public void RemoveAt(int i)
        {
            _tiles.RemoveAt(i);
        }

        public List<Tile> GetTiles()
        {
            return _tiles;
        }
    }
}
