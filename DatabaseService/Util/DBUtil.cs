using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace DatabaseService.Util;

public class DBUtil
{
    //Could be cached
    public static NpgsqlParameter[] StringsToParams(IEnumerable<string> args, out string paramsString,
        string pTitle = "p")
    {
        var sb = new StringBuilder();
        var parameters = new List<NpgsqlParameter>();
        int i = 0;
        foreach (var arg in args)
        {
            var pName = $"{pTitle}{i}";
            var p = new NpgsqlParameter(pName, NpgsqlDbType.Varchar)
            {
                Value = arg
            };
            parameters.Add(p);

            sb.Append(':');
            sb.Append(pName);
            sb.Append(", ");

            i++;
        }

        if (sb.Length > 2)
        {
            sb.Remove(sb.Length - 2, 2);
        }

        paramsString = sb.ToString();
        return parameters.ToArray();
    }
    
    public static NpgsqlParameter[] StringsToValues(IEnumerable<string> args, out string valuesString,
        string pTitle = "val")
    {
        var sb = new StringBuilder();
        var parameters = new List<NpgsqlParameter>();
        int i = 0;
        foreach (var arg in args)
        {
            var pName = $"{pTitle}{i}";
            var p = new NpgsqlParameter(pName, NpgsqlDbType.Varchar)
            {
                Value = arg
            };
            parameters.Add(p);

            sb.Append('(');
            sb.Append('@');
            sb.Append(pName);
            sb.Append("),\n");

            i++;
        }

        if (sb.Length > 2)
        {
            sb.Remove(sb.Length - 2, 2);
        }

        valuesString = sb.ToString();
        return parameters.ToArray();
    }
}