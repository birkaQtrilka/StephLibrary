using System;
using System.Collections;
using System.Collections.Generic;
namespace steph.Unity.WFC.Runtime
{
    public class SocketEnumerator : IEnumerator<string>
    {
        string[] _edges;
        int _index = -1;

        public SocketEnumerator(string[] edges)
        {
            _edges = edges;
        }

        public string Current => _edges[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _edges.Length;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}