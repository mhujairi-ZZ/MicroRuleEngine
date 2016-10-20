using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MicroRuleEngine
{
    internal static class ExpressionBuilder
    {
        private const string StrIsMatch = "IsMatch";
        private const string StrNull = "null";
        private static Type typeOfNullReferenceException = typeof(NullReferenceException);
        private static Type typeOfBool = typeof(bool);
        private static Type typeOfRegex = typeof(Regex);
        private static Type typeOfString = typeof(string);
        private static Type typeOfRegexOptions = typeof(RegexOptions);

        private static readonly ExpressionType[] NestedOperators =
        {
            ExpressionType.And,
            ExpressionType.AndAlso,
            ExpressionType.Or,
            ExpressionType.OrElse
        };

        public static Expression Build<T>(Rule rule, ParameterExpression parameterExpression)
        {
            return Build(typeof(T), rule, parameterExpression);
        }

        //private static Expression BuildEnumrableOpratorExpretion(Type type, Rule rule, ParameterExpression parameterExpression)
        //{
        //     var propExp = GetPropertyExpression(type,rule, parameterExpression);
        //    var org = Expression.Parameter(type, "org");
        //    //Expression<Func<OrganizationField, bool>> predicate =
        //    //    a => a.CustomField.Name == filter.Name && values.Contains(a.Value);
        //    var body = Expression.Call(typeof(Enumerable), "Any", new[] { propExp.Type },
        //        propExp, predicate);

        //    Expression.Block(typeOfBool, Expression.Call())
        //    var lambda = Expression.Lambda<Func<type, bool>>(body, org);
        //}

        private static bool isEnumrableOprator(string oprator)
        {
            return string.Compare(oprator, "Any", StringComparison.CurrentCultureIgnoreCase) ==0;
        }
        public static Expression Build(Type type, Rule rule, ParameterExpression parameterExpression)
        {
            //if (isEnumrableOprator(rule.Operator))
            //{
            //    return BuildEnumrableOpratorExpretion(type, rule, parameterExpression);
            //}



            ExpressionType nestedOperator;
            return Enum.TryParse(rule.Operator, out nestedOperator) &&
                    NestedOperators.Contains(nestedOperator) &&
                    rule.Rules != null &&
                    rule.Rules.Any()
                       ? Build(type,rule.Rules, parameterExpression, nestedOperator)
                       : BuildExpression(type,rule, parameterExpression);
        }

        public static Expression Build<T>(IEnumerable<Rule> rules, ParameterExpression parameterExpression, ExpressionType operation)
        {
            return Build(typeof(T), rules, parameterExpression, operation);
        }

        public static Expression Build(Type type, IEnumerable<Rule> rules, ParameterExpression parameterExpression, ExpressionType operation)
        {
            var expressions = rules.Select(r => Build(type,r, parameterExpression));

            return Build(expressions, operation);
        }



        private static Expression Build(IEnumerable<Expression> expressions, ExpressionType operationType)
        { 
            Func<Expression, Expression, Expression> expressionAggregateMethod;
            switch (operationType)
            {
                case ExpressionType.Or:
                    expressionAggregateMethod = Expression.Or;
                    break;
                case ExpressionType.OrElse:
                    expressionAggregateMethod = Expression.OrElse;
                    break;
                case ExpressionType.AndAlso:
                    expressionAggregateMethod = Expression.AndAlso;
                    break;
                default:
                    expressionAggregateMethod = Expression.And;
                    break;
            }
            return BuildExpression(expressions, expressionAggregateMethod);
        }

        private static Expression BuildExpression(IEnumerable<Expression> expressions, Func<Expression, Expression, Expression> expressionAggregateMethod)
        {
            return expressions.Aggregate<Expression, Expression>(null,
                (current, expression) => current == null
                    ? expression
                    : expressionAggregateMethod(current, expression)
            );
        }

        private static Expression BuildExpression(Type type, Rule rule, Expression expression)
        {
            var propExpression = GetPropertyExpression(type,rule, expression);
            ExpressionType tBinary;
            // is the operator a known .NET operator?
            if (Enum.TryParse(rule.Operator, out tBinary))
            {
                var right = StringToExpression(rule.TargetValue, propExpression.Type);
                return Expression.MakeBinary(tBinary, propExpression, right);
            }
            if (rule.Operator == StrIsMatch)
            {
                return Expression.Call(
                    typeOfRegex.GetMethod(StrIsMatch,
                        new[]
                        {
                            typeOfString,
                            typeOfString,
                            typeOfRegexOptions
                        }
                    ),
                    propExpression,
                    Expression.Constant(rule.TargetValue, typeOfString),
                    Expression.Constant(RegexOptions.IgnoreCase, typeOfRegexOptions)
                    );
            }
            //Invoke a method on the Property
            var inputs = rule.Inputs.Select(x => x.GetType()).ToArray();
            var methodInfo = propExpression.Type.GetMethod(rule.Operator, inputs);
            if (!methodInfo.IsGenericMethod)
                inputs = null; //Only pass in type information to a Generic Method
            var expressions = rule.Inputs.Select(Expression.Constant).ToArray();

            return Expression.TryCatch(
                Expression.Block(typeOfBool, Expression.Call(propExpression, rule.Operator, inputs, expressions)),
                Expression.Catch(typeOfNullReferenceException, Expression.Constant(false))
            );
        }

        public static Type ObjectType = typeof(object);
        private static Expression GetPropertyExpression(Type type,Rule rule, Expression expression)
        {
            Expression propExpression;

            if (expression.Type == ObjectType)
            {
                expression = Expression.TypeAs(expression, type);
            }

            if (string.IsNullOrEmpty(rule.MemberName)) //check is against the object itself
            {
                propExpression = expression;
            }
            else if (rule.MemberName.Contains('.')) //Child property
            {
                var childProperties = rule.MemberName.Split('.');
                var property = type.GetProperty(childProperties[0]);
                // not being used?
                // ParameterExpression paramExp = Expression.Parameter(typeof(T), "SomeObject");

                propExpression = Expression.PropertyOrField(expression, childProperties[0]);
                for (var i = 1; i < childProperties.Length; i++)
                {
                    // not being used?
                    // PropertyInfo orig = property;
                    if (property == null) continue;
                    property = property.PropertyType.GetProperty(childProperties[i]);
                    if (property == null) continue;
                    propExpression = Expression.PropertyOrField(propExpression, childProperties[i]);
                }
            }
            else //Property
            {
                propExpression = Expression.PropertyOrField(expression, rule.MemberName);
            }

            propExpression = Expression.TryCatch(
                Expression.Block(propExpression.Type, propExpression),
                Expression.Catch(typeOfNullReferenceException, Expression.Default(propExpression.Type))
                );
            return propExpression;
        }

        private static Expression StringToExpression(string value, Type propType)
        {
            return value.ToLower() == StrNull
                ? Expression.Constant(null)
                : Expression.Constant(propType.IsEnum
                    ? Enum.Parse(propType, value)
                    : Convert.ChangeType(value, propType));
        }
    }
}
