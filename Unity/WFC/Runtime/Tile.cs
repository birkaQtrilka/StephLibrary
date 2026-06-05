using System;
using System.Collections.Generic;
using UnityEngine;
namespace steph.Unity.WFC.Runtime
{
    [Serializable]
    public class Tile
    {
        //sockets are the edges of tiles divided into 3 parts. These three parts are then checked with other tiles to see if they can be connected
        //A is empty
        //B is road
        [SerializeField] Sockets _sockets;
        public Sockets Sockets => _sockets;
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField, HideInInspector] public float Rotation { get; private set; }
        public bool Manual;
        [Range(0, 1)] public float SpawnChance;

        public List<Tile> Neighbours { get; } = new List<Tile>(4);

        public override string ToString()
        {
            return $"sockets: {_sockets}, rotation: {Rotation}, prefab: {Prefab}";
        }

        public Tile()
        {

        }

        private Tile(GameObject prefab, float rotation, Sockets sockets, float spawnChance, bool manual)
        {
            Rotation = rotation;
            _sockets = sockets;
            Prefab = prefab;
            SpawnChance = spawnChance;
            Manual = manual;
        }

        public void Rotate()
        {
            _sockets.Rotate();
            Rotation += 90;
        }

        public Tile Clone()
        {
            Tile nt = new(Prefab, Rotation, _sockets.Clone(), SpawnChance, Manual);
            return nt;
        }

        public bool CanConnect(Tile otherTile, NeighbourDir dir)
        {
            string mySockets = _sockets.GetSocket(dir);
            NeighbourDir oppositeDir = GetOppositeDir(dir);
            string otherSockets = Reverse(otherTile._sockets.GetSocket(oppositeDir));

            return otherSockets == mySockets;//reverse this
        }

        public bool CanConnectWithBlank(Tile otherTile, NeighbourDir dir)
        {
            if (_sockets.IsBlank(dir, out string mySockets)) return false;

            NeighbourDir oppositeDir = GetOppositeDir(dir);
            string otherSockets = otherTile._sockets.GetSocket(oppositeDir);

            return otherSockets == mySockets;//reverse this
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static NeighbourDir GetOppositeDir(NeighbourDir dir)
        {
            return dir switch
            {
                NeighbourDir.Up => NeighbourDir.Down,
                NeighbourDir.Right => NeighbourDir.Left,
                NeighbourDir.Down => NeighbourDir.Up,
                NeighbourDir.Left => NeighbourDir.Right,
                _ => dir,
            };
        }

        public void GenerateRotatedVersions(List<Tile> results, int rotations)
        {
            Tile newTile = Clone();
            for (int i = 0; i < rotations; i++)
                newTile.Rotate();

            results.Add(newTile);
        }
    }
}