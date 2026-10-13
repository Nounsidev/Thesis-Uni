namespace SmartWarehouse.Cloud
{
    public enum Key { ProductData, SystemLog, Transaction };
    public enum ProductType { Milk, Meat, Fish};
    public static class CloudSaveKey
    {
        public static string GetPath(Key path)
        {
            return path.ToString();
        }
    }
}
