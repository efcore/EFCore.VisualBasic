using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq.Expressions;

namespace EFCore.Design.Tests.Shared
{
    public class TestTypeMappingPlugin<T> : IRelationalTypeMappingSourcePlugin
    {
        private readonly Func<T, Expression> _literalExpressionFunc;

        public TestTypeMappingPlugin(Func<T, Expression> literalExpressionFunc)
        {
            _literalExpressionFunc = literalExpressionFunc;
        }

        public RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            return _literalExpressionFunc == null
                 ? new SimpleTestNonImplementedTypeMapping()
                 : new SimpleTestTypeMapping<T>(_literalExpressionFunc);
        }
    }

    public class SimpleTestTypeMapping<T> : RelationalTypeMapping
    {
        private readonly Func<T, Expression> _literalExpressionFunc;

        public SimpleTestTypeMapping(
            Func<T, Expression> literalExpressionFunc)
            : base("storeType", typeof(SimpleTestType))
        {
            _literalExpressionFunc = literalExpressionFunc;
        }

        public override Expression GenerateCodeLiteral(object value)
            => _literalExpressionFunc((T)value);

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => throw new NotSupportedException();
    }

    public class SimpleTestNonImplementedTypeMapping : RelationalTypeMapping
    {
        public SimpleTestNonImplementedTypeMapping()
            : base("storeType", typeof(SimpleTestType))
        {
        }

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => throw new NotSupportedException();
    }

    public class SimpleTestType
    {
        public static readonly int SomeStaticField = 8;
        public readonly int SomeField = 8;
        public static int SomeStaticProperty { get; } = 8;
        public int SomeInstanceProperty { get; } = 8;

        public SimpleTestType()
        {
        }

        public SimpleTestType(string arg1)
            : this(arg1, null)
        {
        }

        public SimpleTestType(string arg1, int? arg2)
        {
            Arg1 = arg1;
            Arg2 = arg2;
        }

        public string Arg1 { get; } = null!;
        public int? Arg2 { get; }
    }

    public class SimpleTestTypeFactory
    {
        public SimpleTestTypeFactory()
        {
        }

        public SimpleTestTypeFactory(string factoryArg)
        {
            FactoryArg = factoryArg;
        }

        public string FactoryArg { get; } = null!;

        public SimpleTestType Create()
            => new();

        public object Create(string arg1)
            => new SimpleTestType(arg1);

        public object Create(string arg1, int? arg2)
            => new SimpleTestType(arg1, arg2);

        public static SimpleTestType StaticCreate()
            => new();

        public static object StaticCreate(string arg1)
            => new SimpleTestType(arg1);

        public static object StaticCreate(string arg1, int? arg2)
            => new SimpleTestType(arg1, arg2);
    }
}
