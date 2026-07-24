using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace steph.Unity.WFC.Runtime
{
    [Serializable]
    public class Tile
    {
        [field: SerializeField] public string Name { get; private set; }
        //sockets are the edges of tiles divided into 3 parts. These three parts are then checked with other tiles to see if they can be connected
        //A is empty
        //B is road
        [SerializeField] Sockets _sockets;
        public Sockets Sockets => _sockets;
        [field: SerializeField, HideInInspector] public float Rotation { get; private set; }
        public List<Tile> Neighbours { get; } = new List<Tile>(4);


        [Polymorphic, SerializeReference] public TileData TileData;

        public override string ToString()
        {
            return $"NAME: {Name}:: sockets: {_sockets}, rotation: {Rotation}, TileData: {DebugFormat.Debug(TileData)}";
        }

        public Tile(Sockets sockets)
        {
            _sockets = sockets;
        }

        public Tile()
        {

        }

        private Tile(string name, float rotation, Sockets sockets, TileData data)
        {
            Rotation = rotation;
            _sockets = sockets;
            Name = name;
            TileData = data;
        }

        public void Rotate()
        {
            _sockets.Rotate();
            Rotation += 90;
        }

        public Tile Clone()
        {
            Tile nt = new(Name.Clone() as string, Rotation, _sockets.Clone(), TileData);
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

        public bool HasEmptySockets()
        {
            return Sockets.Any(s => string.IsNullOrEmpty(s));
        }
    }
}