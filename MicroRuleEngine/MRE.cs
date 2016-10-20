using System;

namespace MicroRuleEngine
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// for backword compatibility
    /// </remarks>
    public class MRE
    {
        public static MRE Instance { get; } = new MRE();

        public Func<T, bool> Compile<T>(Rule rule)
        {
            return RuleCompiler.Compile<T>(rule);
        }
        public Func<object, bool> Compile(Type type,Rule rule)
        {
            return RuleCompiler.Compile(type,rule);
        }
    }
}