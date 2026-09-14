using Kuantech.Core.Data;
using Kuantech.Core.Database;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Fills the run <see cref="DifficultyConfig"/> from a one-row difficulty table. The config carries the
    /// [KtDatabaseVariable] column mapping and does the actual assignment; this balancer just resolves the
    /// right row and hands it over. The table can be authored inline in the editor or pulled from a remote
    /// sheet — either way the config ends up tuned without a rebuild.
    /// </summary>
    public class RunDifficultyBalancer : DatabaseBalancer
    {
        public DifficultyConfig DifficultyConfig;
        public string RowId = "Default";

        protected override void BalanceFromDb(KtDatabase db, string tableName)
        {
            if (DifficultyConfig == null || db == null) return;

            DataTable table = db.GetDataTable(tableName);
            if (table == null) return;

            // One config → one row. Prefer the named row; fall back to the first for a single-row table.
            DataTable.KtRowData row = table.GetRow(RowId);
            if (row == null && table.Rows != null && table.Rows.Count > 0)
                row = table.Rows[0];
            if (row == null) return;

            DifficultyConfig.UpdateFromDatabaseRow(row);
        }
    }
}
