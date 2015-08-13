using System;
using System.Collections.Generic;

namespace JetBrains.Annotations.Dnx
{
    [PublicAPI]
    [BaseTypeRequired(typeof(string))]
    public class TestClass : Attribute
    {
        [NotNull]
        private readonly string _readOnlyField, _otherField;

        public TestClass([NotNull] string value)
        {
        }

        [CanBeNull]
        public string Value { get; [NotNull] set; }

        [StringFormatMethod("format")]
        [ContractAnnotation("values:null => halt")]
        public void SomeMethod([NotNull, ItemNotNull, NoEnumeration] IEnumerable<string> values, [CanBeNull] string format)
        {
        }

        public void GenericMethod<T>([NotNull] [NoEnumeration] IEnumerable<T> values)
        {
            
        }
    }
}