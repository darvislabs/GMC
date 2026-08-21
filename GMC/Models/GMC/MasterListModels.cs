namespace GMC.Models.GMC
{
    public sealed class MasterListIndexModel
    {
        public string Title { get; set; } = string.Empty;
        public string AddPlaceholder { get; set; } = string.Empty;
        public string ItemLabel { get; set; } = "Name";
        public string AddAction { get; set; } = string.Empty;
        public string EditAction { get; set; } = string.Empty;
        public string DeleteAction { get; set; } = string.Empty;

        public List<string> Items { get; set; } = new();
    }

    public sealed class EditMasterItemModel
    {
        public string OldName { get; set; } = string.Empty;
        public string NewName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SaveAction { get; set; } = string.Empty;
        public string CancelAction { get; set; } = string.Empty;
    }
}

