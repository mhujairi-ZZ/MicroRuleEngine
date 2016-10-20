using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MicroRuleEngine
{
    public static class RuleCompiler
    {
        public static Func<T, bool> Compile<T>(Rule rule)
        {
            var type = typeof(T);
            var expressionParameter = Expression.Parameter(type);
            var expression = ExpressionBuilder.Build(type,rule, expressionParameter);

            return Expression.Lambda<Func<T, bool>>(expression, expressionParameter).Compile();
        }

        public static Func<T, bool> Compile<T>(IEnumerable<Rule> rules)
        {
            var type = typeof (T);
            var expressionParameter = Expression.Parameter(type);
            var expression = ExpressionBuilder.Build(type,rules, expressionParameter, ExpressionType.And);
            return Expression.Lambda<Func<T, bool>>(expression, expressionParameter).Compile();
        }

        public static Func<object,bool> Compile(Type type, Rule rule)
        {
            var expressionParameter = Expression.Parameter(ExpressionBuilder.ObjectType);
            var expression = ExpressionBuilder.Build(type,rule, expressionParameter);
            return Expression.Lambda<Func<object, bool>>(expression, expressionParameter).Compile();
        }

        public static Func<object, bool> Compile<T>(Type type, IEnumerable<Rule> rules)
        {
            var expressionParameter = Expression.Parameter(type);
            var expression = ExpressionBuilder.Build(type,rules, expressionParameter, ExpressionType.And);
            return Expression.Lambda<Func<object, bool>>(expression, expressionParameter).Compile();
        }
    }
}