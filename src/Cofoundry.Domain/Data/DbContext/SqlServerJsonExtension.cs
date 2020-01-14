using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cofoundry.Domain.Data
{
    public static class SqlServerJsonExtension
    {
        [DbFunction("JSON_VALUE", Schema = "")]
        public static string JsonValue(string column, [NotParameterized] string path) => throw new NotSupportedException();

        [DbFunction("DATEPART", Schema = "")]
        public static int? DatePart(string datePartArg, DateTime? date) => throw new Exception();
    }
}