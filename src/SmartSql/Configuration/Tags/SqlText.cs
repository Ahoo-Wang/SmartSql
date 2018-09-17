﻿using SmartSql.Abstractions;
using SmartSql.Configuration.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartSql.Configuration.Tags
{
    public class SqlText : ITag
    {
        public TagType Type => TagType.SqlText;
        public string BodyText { get; set; }
        public ITag Parent { get; set; }
        public Statement Statement { get; set; }

        public void BuildSql(RequestContext context)
        {
            context.Sql.Append(BodyText);
        }

        public bool IsCondition(RequestContext context)
        {
            return true;
        }

    }
}
