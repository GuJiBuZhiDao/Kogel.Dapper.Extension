﻿using System;
using System.Linq.Expressions;
using Kogel.Dapper.Extension;
using Kogel.Dapper.Extension.Core.Interfaces;
using Kogel.Dapper.Extension.MsSql.Extension;
using System.Text.RegularExpressions;
using Kogel.Dapper.Extension.Extension;
using System.Collections.Generic;
using System.Linq;

namespace Kogel.Dapper.Extension
{
    public class MsSqlProvider : SqlProvider
    {
        private readonly static string OpenQuote = "[";
        private readonly static string CloseQuote = "]";
        private readonly static char ParameterPrefix = '@';
        public override IResolveExpression ResolveExpression { get; set; }
        public MsSqlProvider()
        {
            ProviderOption = new ProviderOption(OpenQuote, CloseQuote, ParameterPrefix);
            ResolveExpression = new ResolveExpression(this);
        }

        public sealed override IProviderOption ProviderOption { get; set; }

        public override SqlProvider FormatGet<T>()
        {
            var selectSql = ResolveExpression.ResolveSelect(1);

            var fromTableSql = FormatTableName();

            var nolockSql = ResolveExpression.ResolveWithNoLock(Context.Set.NoLock);

            var whereSql = ResolveExpression.ResolveWhereList();

            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref selectSql);

            var groupSql = ResolveExpression.ResolveGroupBy();

            var havingSql = ResolveExpression.ResolveHaving();

            var orderbySql = ResolveExpression.ResolveOrderBy();

            SqlString = $"{selectSql} {fromTableSql} {nolockSql} {joinSql} {whereSql} {groupSql} {havingSql} {orderbySql}";

            return this;
        }

        public override SqlProvider FormatToList<T>()
        {
            var topNum = DataBaseContext<T>().QuerySet.TopNum;

            var selectSql = ResolveExpression.ResolveSelect(topNum);

            var fromTableSql = FormatTableName();

            var nolockSql = ResolveExpression.ResolveWithNoLock(Context.Set.NoLock);

            var whereSql = ResolveExpression.ResolveWhereList();

            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref selectSql);

            var groupSql = ResolveExpression.ResolveGroupBy();

            var havingSql = ResolveExpression.ResolveHaving();

            var orderbySql = ResolveExpression.ResolveOrderBy();

            SqlString = $"{selectSql} {fromTableSql} {nolockSql} {joinSql} {whereSql} {groupSql} {havingSql} {orderbySql}";

            return this;
        }

        public override SqlProvider FormatToPageList<T>(int pageIndex, int pageSize)
        {
            var orderbySql = ResolveExpression.ResolveOrderBy();

            var selectSql = ResolveExpression.ResolveSelect(null);

            var fromTableSql = FormatTableName();

            var nolockSql = ResolveExpression.ResolveWithNoLock(Context.Set.NoLock);

            var whereSql = ResolveExpression.ResolveWhereList();

            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref selectSql);

            var groupSql = ResolveExpression.ResolveGroupBy();

            var havingSql = ResolveExpression.ResolveHaving();

            SqlString = $@"SELECT T.* FROM    ( 
                            SELECT ROW_NUMBER() OVER ( {orderbySql} ) AS ROWNUMBER,
                            {(new Regex("SELECT").Replace(selectSql, "", 1))}
                            {fromTableSql} {nolockSql}{joinSql}
                            {whereSql}
                            {groupSql}
                            {havingSql}
                            ) T
                            WHERE ROWNUMBER BETWEEN {((pageIndex - 1) * pageSize) + 1} AND {pageIndex * pageSize};";

            return this;
        }

        public override SqlProvider FormatCount()
        {
            var selectSql = "SELECT COUNT(1)";

            var fromTableSql = FormatTableName();

            var nolockSql = ResolveExpression.ResolveWithNoLock(Context.Set.NoLock);

            var whereSql = ResolveExpression.ResolveWhereList();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql);

            var groupSql = ResolveExpression.ResolveGroupBy();

            var havingSql = ResolveExpression.ResolveHaving();

            if (!Context.Set.IsDistinct)
            {
                if (string.IsNullOrEmpty(groupSql))
                    SqlString = $"{selectSql} {fromTableSql} {nolockSql} {joinSql} {whereSql}";
                else
                {
                    selectSql = ResolveExpression.ResolveSelect(null);
                    SqlString = $"SELECT COUNT(*) FROM( {selectSql} {fromTableSql} {nolockSql} {joinSql} {whereSql} {groupSql} {havingSql} )T";
                }
            }
            else
            {
                //字段解析字符
                string countBySql = ResolveExpression.ResolveSelect(null);

                SqlString = $@"SELECT COUNT(*) FROM(
                                {countBySql} {fromTableSql} {nolockSql}
                                {joinSql}
                                {whereSql}
                                 )T";
            }

            return this;
        }

        public override SqlProvider FormatDelete()
        {
            var fromTableSql = FormatTableName(false, false);

            ProviderOption.IsAsName = false;

            var whereSql = ResolveExpression.ResolveWhereList();

            SqlString = $"DELETE {fromTableSql} {whereSql }";

            return this;
        }

        public override SqlProvider FormatInsert<T>(T entity, string[] excludeFields)
        {
            SqlString = ResolveExpression.ResolveBulkInsert<T>(new List<T> { entity }, excludeFields);
            return this;
        }

        public override SqlProvider FormatInsert<T>(IEnumerable<T> entitys, string[] excludeFields)
        {
            SqlString = ResolveExpression.ResolveBulkInsert<T>(entitys, excludeFields);
            return this;
        }

        public override SqlProvider FormatInsertIdentity<T>(T entity, string[] excludeFields)
        {
            SqlString = $"{ResolveExpression.ResolveBulkInsert<T>(new List<T> { entity }, excludeFields)} SELECT @@IDENTITY";
            return this;
        }

        public override SqlProvider FormatUpdate<T>(Expression<Func<T, T>> updateExpression)
        {
            var update = ResolveExpression.ResolveUpdate(updateExpression);

            ProviderOption.IsAsName = false;

            var whereSql = ResolveExpression.ResolveWhereList();
            Params.AddDynamicParams(update.Param);

            SqlString = $"UPDATE {FormatTableName(false, false)} {update.SqlCmd} {whereSql}";

            return this;
        }

        public override SqlProvider FormatUpdate<T>(T entity, string[] excludeFields)
        {
            var update = ResolveExpression.ResolveUpdate(entity, Params, excludeFields);

            ProviderOption.IsAsName = false;

            var whereSql = ResolveExpression.ResolveWhereList();
            //如果不存在条件，就用主键作为条件
            if (whereSql.Trim().Equals("WHERE 1=1"))
                whereSql += GetIdentityWhere(entity, Params);

            SqlString = $"UPDATE {FormatTableName(false, false)} {update} {whereSql}";
            return this;
        }

        public override SqlProvider FormatUpdate<T>(IEnumerable<T> entites, string[] excludeFields)
        {
            var update = ResolveExpression.ResolveBulkUpdate(entites, Params, excludeFields);
            ProviderOption.IsAsName = false;
            //批量修改只能用主键作为条件
            var whereSql = GetIdentityWhere(entites.FirstOrDefault(), Params);

            SqlString = $"UPDATE {FormatTableName(false, false)} {update} WHERE 1=1 {whereSql}";
            return this;
        }

        public override SqlProvider FormatSum(LambdaExpression sumExpression)
        {
            var selectSql = ResolveExpression.ResolveSum(sumExpression);

            var fromTableSql = FormatTableName();

            var nolockSql = ResolveExpression.ResolveWithNoLock(Context.Set.NoLock);

            var whereSql = ResolveExpression.ResolveWhereList();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql);

            SqlString = $"{selectSql} {fromTableSql} {nolockSql} {joinSql} {whereSql} ";

            return this;
        }

        public override SqlProvider FormatMin(LambdaExpression minExpression)
        {
            var selectSql = ResolveExpression.ResolveMin(minExpression);

            var fromTableSql = FormatTableName();

            var whereSql = ResolveExpression.ResolveWhereList();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql);

            SqlString = $"{selectSql} {fromTableSql}{joinSql} {whereSql} ";

            return this;
        }
        public override SqlProvider FormatMax(LambdaExpression maxExpression)
        {
            var selectSql = ResolveExpression.ResolveMax(maxExpression);

            var fromTableSql = FormatTableName();

            var whereSql = ResolveExpression.ResolveWhereList();

            string noneSql = "";
            var joinSql = ResolveExpression.ResolveJoinSql(JoinList, ref noneSql);

            SqlString = $"{selectSql} {fromTableSql}{joinSql} {whereSql} ";

            return this;
        }

        public override SqlProvider FormatUpdateSelect<T>(Expression<Func<T, T>> updator)
        {
            var update = ResolveExpression.ResolveUpdate(updator);

            var selectSql = ResolveExpression.ResolveSelect(null);

            var fromTableSql = FormatTableName();

            var whereSql = ResolveExpression.ResolveWhereList();

            SqlString = $"UPDATE {FormatTableName(false, false)} {update.SqlCmd} {whereSql}; {selectSql} {fromTableSql} {whereSql};";

            return this;
        }

        public override SqlProvider Create()
        {
            return new MsSqlProvider();
        }
    }
}
