﻿using Caredev.Mego.Resolve.Commands;
using Caredev.Mego.Resolve.Operates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Transactions;

namespace Caredev.Mego.Tests
{
    public static class Utility
    {
        public static void CompareSql(DbContext db, Expression exp, string sql)
        {
            var itemtype = exp.Type;
            if (itemtype.IsGenericType && typeof(IEnumerable).IsAssignableFrom(itemtype))
            {
                itemtype = itemtype.GetGenericArguments()[0];
            }
            var _Operate = new DbQueryCollectionOperate(db, exp, itemtype);
            var expression = db.Configuration.Translator.Translate(_Operate);
            var comands = new OperateCommandCollection(_Operate.Executor);
            comands.NextCommand();

            var generatesql = db.Database.Generator.Generate(_Operate, expression);
            CompareSql(sql, generatesql);
        }

        public static void CompareSql(DbContext db, DbOperateBase ope, string sql)
        {
            DbOperateBase operate = (DbOperateBase)ope;
            var comands = new OperateCommandCollection(operate.Executor);
            comands.NextCommand();

            var generatesql = ope.GenerateSql();

            CompareSql(sql, generatesql);
        }

        private static void CompareSql(string sql, string generatesql)
        {
            var reg = new Regex("\\s+");

            generatesql = generatesql.Trim();

            generatesql = Regex.Replace(generatesql, @"`t\$\d+`", "`t$1`");
            generatesql = Regex.Replace(generatesql, @"\[t\$\d+\]", "[t$1]");

            System.Diagnostics.Debug.WriteLine("生成代码：");
            System.Diagnostics.Debug.WriteLine(generatesql);
            System.Diagnostics.Debug.WriteLine("");

            var left = reg.Replace(generatesql, "");
            var right = reg.Replace(sql, "");

            var result = left + Environment.NewLine + right;

            Assert.AreEqual(left, right, true);
        }

        public static void CommitTest<T>(T context, Action<T> action, string sql = "") where T : DbContext
        {
            try
            {
                if (context.Database.Provider.SupportDistributedTransaction)
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required))
                    {
                        try
                        {
                            action(context);
                        }
                        catch (NotSupportedException e)
                        {
                            if (sql != Constants.NotSuppored) throw;
                        }
                    }
                }
                else
                {
                    var tran = context.Database.BeginTransaction();
                    try
                    {
                        try
                        {
                            action(context);
                        }
                        catch (NotSupportedException ex)
                        {
                            if (sql != Constants.NotSuppored) throw;
                        }
                    }
                    finally
                    {
                        tran.Rollback();
                    }
                }
            }
            finally
            {
                context.Dispose();
            }
        }
    }
}
