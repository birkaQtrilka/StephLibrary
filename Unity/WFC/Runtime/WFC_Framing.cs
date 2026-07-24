using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    [CreateAssetMenu(menuName = "Stefan/WFC/Behaviors/Framing", fileName = "new WFC_Behavior")]
    public class WFC_Framing : WFC_Behavior
    {
        [SerializeField] string frameTileName = "Blank";
        [Range(0,3), SerializeField] int rotationCount = 0;

        public override void BeforePropagate(WorldGenConfig ctxt)
        {
            Tile emptyTile = ctxt.AvailableTiles.GetTiles().Find(x => x.Name == frameTileName);
            if (emptyTile == null)
            {
                Debug.LogError($"Could not find tile with name {frameTileName} in available tiles. Please make sure the tile exists.");
                return;
            }
            emptyTile = emptyTile.GenerateRotatedVersion(rotationCount);

            for (int y = 0; y < ctxt.Columns; y++)
                ctxt.CollapseCell(ctxt.Grid[y, 0], emptyTile);
            for (int y = 0; y < ctxt.Columns; y++)
                ctxt.CollapseCell(ctxt.Grid[y, ctxt.Columns - 1], emptyTile);

            for (int x = 1; x < ctxt.Columns - 1; x++)
                ctxt.CollapseCell(ctxt.Grid[0, x], emptyTile);
            for (int x = 1; x < ctxt.Columns - 1; x++)
                ctxt.CollapseCell(ctxt.Grid[ctxt.Columns - 1, x], emptyTile);
        }
    }
}
