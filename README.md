# JetBrains.Annotations.Dnx

Given the following class:

```csharp
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
```

The following is produced in `./bin/{configuration}` at build-time:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly name="JetBrains.Annotations.Dnx">
  <member name="T:JetBrains.Annotations.Dnx.TestClass">
    <attribute ctor="JetBrains.Annotations.PublicAPIAttribute.PublicAPIAttribute()" />
    <attribute ctor="JetBrains.Annotations.BaseTypeRequiredAttribute.BaseTypeRequiredAttribute(System.Type)">
      <argument>typeof(string)</argument>
    </attribute>
  </member>
  <member name="F:JetBrains.Annotations.Dnx.TestClass._readOnlyField">
    <attribute ctor="JetBrains.Annotations.NotNullAttribute.NotNullAttribute()" />
  </member>
  <member name="F:JetBrains.Annotations.Dnx.TestClass._otherField">
    <attribute ctor="JetBrains.Annotations.NotNullAttribute.NotNullAttribute()" />
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.TestClass(string)">
    <parameter name="value">
      <attribute ctor="JetBrains.Annotations.NotNullAttribute.NotNullAttribute()" />
    </parameter>
  </member>
  <member name="P:JetBrains.Annotations.Dnx.TestClass.Value">
    <attribute ctor="JetBrains.Annotations.CanBeNullAttribute.CanBeNullAttribute()" />
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.Value.set">
    <attribute ctor="JetBrains.Annotations.NotNullAttribute.NotNullAttribute()" />
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.SomeMethod(System.Collections.Generic.IEnumerable&lt;string&gt;, string)">
    <attribute ctor="JetBrains.Annotations.StringFormatMethodAttribute.StringFormatMethodAttribute(string)">
      <argument>format</argument>
    </attribute>
    <attribute ctor="JetBrains.Annotations.ContractAnnotationAttribute.ContractAnnotationAttribute(string)">
      <argument>values:null =&gt; halt</argument>
    </attribute>
    <parameter name="values">
      <attribute ctor="JetBrains.Annotations.NotNullAttribute.NotNullAttribute()" />
      <attribute ctor="JetBrains.Annotations.ItemNotNullAttribute.ItemNotNullAttribute()" />
      <attribute ctor="JetBrains.Annotations.NoEnumerationAttribute.NoEnumerationAttribute()" />
    </parameter>
    <parameter name="format">
      <attribute ctor="JetBrains.Annotations.CanBeNullAttribute.CanBeNullAttribute()" />
    </parameter>
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.GenericMethod&lt;T&gt;(System.Collections.Generic.IEnumerable&lt;T&gt;)">
    <parameter name="values">
      <attribute ctor="JetBrains.Annotations.NotNullAttribute.NotNullAttribute()" />
      <attribute ctor="JetBrains.Annotations.NoEnumerationAttribute.NoEnumerationAttribute()" />
    </parameter>
  </member>
</assembly>
```

It's not 100% yet, but it's getting there :grin:
