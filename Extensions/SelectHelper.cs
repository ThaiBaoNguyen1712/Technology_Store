namespace Tech_Store.Extensions
{
    public static class SelectHelper
    {
        public static string IsSelected(string currentValue, string optionValue)
        {
            return currentValue == optionValue ? "selected" : "";
        }
    }
}
