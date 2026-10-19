namespace SmartWarehouse.Cloud
{
    public enum Key { SystemLog, Transaction, Catalog, ProductIcon, Layout };
    public static class CloudSaveKey
    {
        public static string GetPath(Key path)
        {
            return path.ToString();
        }
    }
}
