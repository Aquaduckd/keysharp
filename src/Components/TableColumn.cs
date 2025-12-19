namespace Keysharp.Components
{
    /// <summary>
    /// Defines a column in a table with its header and visibility.
    /// </summary>
    public class TableColumn
    {
        public string Header { get; set; }
        public bool IsVisible { get; set; }
        public float? WidthPercentage { get; set; } // Optional: for default width calculations

        public TableColumn(string header, bool isVisible = true, float? widthPercentage = null)
        {
            Header = header;
            IsVisible = isVisible;
            WidthPercentage = widthPercentage;
        }
    }
}

