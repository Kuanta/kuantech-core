namespace Kuantech.Core.Database
{
    public interface IKtDataTable
    {
        public KtDataEntry GetDataEntry(string rowId, string entryKey);
    }
}