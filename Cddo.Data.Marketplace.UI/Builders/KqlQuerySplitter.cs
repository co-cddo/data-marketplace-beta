
using System.Text;

namespace Cddo.Data.Marketplace.UI.Builders
{
    public static class KqlQuerySplitter
    {

        public static Dictionary<string, object> SplitKqlQuery(string kqlQuery)
        {
            var queryParts = new Dictionary<string, object>();

            //Lets get the source table
            string sourceTable = ExtractSourceTable(kqlQuery);
            queryParts["SourceTable"] = sourceTable;

            //lets get the extends
            var extendFields = ExtractExtendField(kqlQuery);
            queryParts["ExtendFields"] = extendFields;

            var whereCondition = ExtractWhereConditions(kqlQuery);
            queryParts["WhereCondition"] = whereCondition;

            var summarizeFields = ExtractSummarizeFields(kqlQuery);
            queryParts["SummarizeFields"] = summarizeFields;

            //string by = ExtractBy(kqlQuery);
            //queryParts["By"] = by;

            string orderBy = ExtractOrderBy(kqlQuery);
            queryParts["OrderBy"] = orderBy;

            return queryParts;
        }

        private static List<string> ExtractWhereConditions(string kqlQuery)
        {
            var conditions = new List<string>();
            var lines = kqlQuery.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("| where"))
                {
                    conditions.Add(trimmedLine.Substring("| where".Length).Trim());
                }
            }
            return conditions;
        }
        static List<string> ExtractSummarizeFields(string query)
        {
            var summarizeFields = new List<string>();
            var lines = query.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            bool inSummarizeSection = false;
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("| summarize"))
                {
                    inSummarizeSection = true;
                }
                else if (inSummarizeSection)
                {
                    if(!trimmedLine.Contains("|"))
                    {
                        summarizeFields.Add(trimmedLine);
                    }
                    else
                    {
                        inSummarizeSection = false;
                    }
                }
            }

            return summarizeFields;
        }

        static string ExtractOrderBy(string query)
        {
            var lines = query.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("| order by"))
                {
                    return trimmedLine.Substring("| order by".Length).Trim();
                }
            }
            return string.Empty;
        }

        private static List<string> ExtractExtendField(string kqlQuery)
        {
            var extendedFields = new List<string>();
            var lines = kqlQuery.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            bool isExtendSection = false;

            foreach ( var line in lines )
            {
                var trimmedLine = line.Trim();
                if(trimmedLine.StartsWith("| extend"))
                {
                    isExtendSection = true;
                }else if (isExtendSection)
                {
                    if (trimmedLine.Contains("=") && !trimmedLine.Contains("|"))
                    {
                        extendedFields.Add(trimmedLine);
                    }
                    else
                    {
                        isExtendSection = false;
                    }
                }
            }
            return extendedFields;

        }

        private static string ExtractSourceTable(string kqlQuery)
        {
            var lines = kqlQuery.Split(new[] {"\r\n", "\n"},  StringSplitOptions.RemoveEmptyEntries);

            return lines[0].Trim();
        }

        public static string RebuildKqlQuery(Dictionary<string, object> queryParts)
        {
            StringBuilder kqlQuery = new StringBuilder();

            // Add the source table
            kqlQuery.AppendLine(queryParts["SourceTable"].ToString());

            // Add extend fields
            var extendFields = queryParts["ExtendFields"] as List<string>;
            if (extendFields != null && extendFields.Count > 0)
            {
                kqlQuery.AppendLine("| extend");
                foreach (var field in extendFields)
                {
                    kqlQuery.AppendLine($"    {field}");
                }
                kqlQuery.Length--; // Remove the last comma
            }

            // Add where condition
            var whereCondition = queryParts["WhereCondition"] as List<string>;
            if (whereCondition != null && whereCondition.Count > 0)
            {
                foreach (var item in whereCondition)
                {
                    kqlQuery.AppendLine($"| where {item}");
                }
                
            }

            // Add summarize fields
            var summarizeFields = queryParts["SummarizeFields"] as List<string>;
            if (summarizeFields != null && summarizeFields.Count > 0)
            {
                kqlQuery.AppendLine("| summarize ");
                var all = string.Join(" ", summarizeFields);
                var clean = all.Replace(", by,", " by ");
                kqlQuery.AppendLine(clean);
            }

            // Add order by
            string orderBy = queryParts["OrderBy"].ToString();
            if (!string.IsNullOrEmpty(orderBy))
            {
                kqlQuery.AppendLine($"| order by {orderBy}");
            }

            return kqlQuery.ToString();
        }
    }
}
