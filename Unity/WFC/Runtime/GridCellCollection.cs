using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    [Serializable]
    public class GridCellCollection : IEnumerable<GridCell>
    {

        [SerializeField] GridCellRow[] _cellRows = new GridCellRow[0];
        public int Length => _cellRows[0].Cells.Length * _cellRows.Length;

        public GridCellCollection(int columns)
        {
            _cellRows = new GridCellRow[columns];
            for (int i = 0; i < _cellRows.Length; i++)
            {
                _cellRows[i] = new GridCellRow(columns);
            }
        }

        public GridCell this[int indexY, int indexX]
        {
            get => _cellRows[indexY].Cells[indexX];
            set => _cellRows[indexY].Cells[indexX] = value;
        }

        public int GetVerticalLength()
        {
            return _cellRows.Length;
        }

        public int GetHorizontalLength()
        {
            return _cellRows.Length == 0 ? -1 : _cellRows[0].Cells.Length;
        }

        public IEnumerator<GridCell> GetEnumerator()
        {
            return new GridCellEnumerator(_cellRows);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new GridCellEnumerator(_cellRows);
        }
    }
}