using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    [Serializable]
    public class Sockets : IEnumerable<string>, IEquatable<Sockets>
    {
        //up right down left
        [SerializeField] string[] _edges = new string[4];

        public int socketLength = 3;

        private Sockets() { }

        public Sockets(int socketLength)
        {
            this.socketLength = socketLength;
            _edges = new string[4];
            for (int i = 0; i < _edges.Length; i++)
            {
                _edges[i] = new string('a', socketLength);
            }
        }

        public string[] GetArray()
        {
            return _edges;
        }

        public void Rotate()
        {
            string lastSocket = _edges[^1];
            for (int i = _edges.Length - 1; i >= 1; i--)
            {
                _edges[i] = _edges[i - 1];
            }

            _edges[0] = lastSocket;
        }

        public string GetSocket(NeighbourDir direction)
        {
            return _edges[(int)direction];
        }

        public bool IsBlank(NeighbourDir direction, out string socket)
        {
            socket = GetSocket(direction);
            return socket.All(s => s == 'a');
        }

        public Sockets Clone()
        {
            Sockets nt = new()
            {
                _edges = (string[])_edges.Clone()
            };
            return nt;
        }

        public override string ToString()
        {
            return $"Up: {_edges[0]}, Right: {_edges[1]}, Down: {_edges[2]}, Left: {_edges[3]}";
        }

        public IEnumerator<string> GetEnumerator()
        {
            return new SocketEnumerator(_edges);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SocketEnumerator(_edges);
        }

        public bool Equals(Sockets other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            for (int i = 0; i < _edges.Length; i++)
            {
                if (_edges[i] != other._edges[i]) return false;
            }
            return true;
        }
    }
}