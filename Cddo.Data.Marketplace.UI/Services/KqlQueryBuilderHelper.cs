using System.Text.RegularExpressions;

namespace Cddo.Data.Marketplace.UI.Services
{
    public static class KqlQueryBuilderHelper
    {
      public static  List<string> ExtractExtendValues(string query)
        {
            var extendValues = new List<string>();

            // Regular expression to match the extend line
            var regex = new Regex(@"extend\s+(.+?)\s*[\|]",
                                    RegexOptions.Singleline,
                                    TimeSpan.FromMilliseconds(500));
            var match = regex.Match(query);

            if (match.Success)
            {
                // Get the part inside the extend clause
                string extendClause = match.Groups[1].Value.Trim();

                // Split by comma to get individual items
                var items = extendClause.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    // Get just the variable names (before the '=')
                    var parts = item.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && !parts[0].Contains("'"))
                    {
                        // Trim the variable name and add it to the list
                        extendValues.Add(parts[0].Trim());
                    }
                    
                }
            }

            return extendValues;
        }
    }
}
