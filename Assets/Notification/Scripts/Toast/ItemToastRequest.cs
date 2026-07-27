namespace Sandbox.Notification
{
    public sealed class ItemToastRequest : NotificationRequest
    {
        public readonly string ItemId;
        public readonly string DisplayName;
        public readonly int AddedAmount;
        public readonly int TotalAmount;

        public ItemToastRequest(string itemId, string displayName, int addedAmount, int totalAmount)
        {
            ItemId = itemId;
            DisplayName = displayName;
            AddedAmount = addedAmount;
            TotalAmount = totalAmount;
        }
    }
}
