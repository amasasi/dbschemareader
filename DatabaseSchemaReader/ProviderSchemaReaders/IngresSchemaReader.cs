﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DatabaseSchemaReader.ProviderSchemaReaders
{
    class IngresSchemaReader : SchemaExtendedReader
    {
        public IngresSchemaReader(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
        }

        //ingres also has sequences (there is an iisequences but I can't find any reference)
        //it also has identity columns (generated always as identity/generated by default as identity like db2).
        //For identity, check the column default- otherwise I found no metadata catalog

        //reference: http://docs.ingres.com/ingres/10.0/opensql-reference-guide/5067-the-iiconstraints-catalog

        protected override DataTable PrimaryKeys(string tableName, DbConnection connection)
        {
            const string sql = @"SELECT 
trim(constraint_name) AS constraint_name, 
trim(table_name) AS table_name, 
trim(schema_name) AS schema_name, 
trim(column_name) AS column_name, 
key_position AS ordinal_position 
FROM iikeys
WHERE 
    (trim(table_name) = @tableName OR @tableName IS NULL) AND 
    (trim(schema_name) = @schemaOwner OR @schemaOwner IS NULL)";

            return CommandForTable(tableName, connection, "PrimaryKeys", sql);
        }

        protected override DataTable CheckConstraints(string tableName, DbConnection connection)
        {
            const string sql = @"SELECT 
trim(constraint_name) AS constraint_name, 
trim(table_name) AS table_name, 
trim(schema_name) AS schema_name, 
text_segment AS Expression 
FROM iiconstraints
WHERE 
    constraint_type = 'C' AND
    (trim(table_name) = @tableName OR @tableName IS NULL) AND 
    (trim(schema_name) = @schemaOwner OR @schemaOwner IS NULL)";

            return CommandForTable(tableName, connection, "CheckKeys", sql);
        }

        protected override DataTable UniqueKeys(string tableName, DbConnection connection)
        {
            const string sql = @"SELECT 
trim(constraint_name) AS constraint_name, 
trim(table_name) AS table_name, 
trim(schema_name) AS schema_name, 
text_segment
FROM iiconstraints
WHERE 
    constraint_type = 'U' AND
    (trim(table_name) = @tableName OR @tableName IS NULL) AND 
    (trim(schema_name) = @schemaOwner OR @schemaOwner IS NULL)";

            var uniqueKeys = CommandForTable(tableName, connection, "CheckKeys", sql);
            uniqueKeys.Columns.Add("column_name", typeof(string));
            var additions = new List<DataRow>();
            foreach (DataRow row in uniqueKeys.Rows)
            {
                ParseColumnNames(row, uniqueKeys, additions);
            }

            //unique constraints on more than one column
            foreach (var addition in additions)
            {
                uniqueKeys.Rows.Add(addition);
            }

            return uniqueKeys;
        }

        private static void ParseColumnNames(DataRow row, DataTable uniqueKeys, ICollection<DataRow> additions)
        {
            var txt = row["text_segment"].ToString();
            if (string.IsNullOrEmpty(txt)) return;
            var brace1 = txt.IndexOf('(') + 1;
            var brace2 = txt.IndexOf(')');
            var cols = txt.Substring(brace1, brace2 - brace1);
            var cola = cols.Split(',');
            for (int i = 1; i < cola.Length; i++)
            {
                var clone = uniqueKeys.NewRow();
                clone.ItemArray = row.ItemArray;
                clone["column_name"] = cola[i].Trim('"', ' ');
                additions.Add(clone);
            }

            var col = cola[0].Trim('"',' '); //remove quoting
            //what if you have multi-column keys?
            row["column_name"] = col;
        }

        protected override DataTable ForeignKeys(string tableName, DbConnection connection)
        {
            //need to trim the char(256)
            const string sql = @"SELECT 
trim(c.ref_constraint_name) AS constraint_name, 
trim(c.ref_table_name) AS table_name, 
trim(c.ref_schema_name) AS schema_name, 
trim(c.unique_constraint_name) AS unique_constraint_name,
trim(c.unique_table_name) AS fk_table,
i.text_segment
FROM iiref_constraints c
JOIN iiconstraints i ON c.ref_constraint_name = i.constraint_name AND c.ref_schema_name = i.schema_name
WHERE 
    (trim(c.ref_table_name) = @tableName OR @tableName IS NULL) AND 
    (trim(c.ref_schema_name) = @schemaOwner OR @schemaOwner IS NULL)";

            var foreignKeys = CommandForTable(tableName, connection, "ForeignKeys", sql);
            foreignKeys.Columns.Add("column_name", typeof(string));
            var additions = new List<DataRow>();
            //text_segment: FOREIGN KEY (al_ccode) REFERENCES "martin".country(ct_code)
            foreach (DataRow row in foreignKeys.Rows)
            {
                ParseColumnNames(row, foreignKeys, additions);
            }

            foreach (var addition in additions)
            {
                foreignKeys.Rows.Add(addition);
            }

            return foreignKeys;
        }
    }
}