using System.Collections;
using System.Collections.Generic;
namespace steph.Unity.WFC.Runtime
{
    public class GridCellEnumerator : IEnumerator<GridCell>
    {
        private readonly GridCellRow[] _cellRows;
        int x = -1;
        int y = 0;

        public GridCellEnumerator(GridCellRow[] cellRows)
        {
            _cellRows = cellRows;
        }

        public GridCell Current => _cellRows[y].Cells[x];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            x++;

            if (x >= _cellRows.Length)
            {
                y++;
                x = 0;
            }
            return y < _cellRows.Length;
        }

        public void Reset()
        {
            x = -1;
            y = 0;
        }
        public void Dispose()
        {
        }
    }
}