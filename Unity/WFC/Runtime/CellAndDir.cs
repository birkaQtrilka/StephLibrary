
namespace steph.Unity.WFC.Runtime
{
    public readonly struct CellAndDir
    {
        public readonly GridCell cell;
        public readonly NeighbourDir dir;

        public CellAndDir(GridCell cell, NeighbourDir dir)
        {
            this.cell = cell;
            this.dir = dir;
        }
    }
}