﻿namespace DatabaseSchemaReader.CodeGen
{
    static class PackagesWriter
    {
        public static string WriteEntityFrameworkNet4()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""EntityFramework"" version=""6.0.1"" targetFramework=""net40"" />
</packages>";
        }

        public static string WriteFluentNHibernateNet4()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""FluentNHibernate"" version=""1.3.0.733"" targetFramework=""net40"" />
  <package id=""Iesi.Collections"" version=""3.2.0.4000"" targetFramework=""net40"" />
  <package id=""NHibernate"" version=""3.3.3.4001"" targetFramework=""net40"" />
</packages>";
        }
    }
}