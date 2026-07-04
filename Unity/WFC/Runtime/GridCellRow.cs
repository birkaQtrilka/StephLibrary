using System;
namespace steph.Unity.WFC.Runtime
{
  // testing update 
    [Serializable]
    public class GridCellRow
    {
        public GridCell[] Cells = new GridCell[0];
        public int Length => Cells.Length;

        public GridCellRow(int columns)
        {
            Cells = new GridCell[columns];
        }
    }
}