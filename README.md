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
    <attribute ctor="M:JetBrains.Annotations.PublicAPIAttribute.#ctor" />
    <attribute ctor="M:JetBrains.Annotations.BaseTypeRequiredAttribute.#ctor(System.Type)">
      <argument>typeof(string)</argument>
    </attribute>
  </member>
  <member name="F:JetBrains.Annotations.Dnx.TestClass._readOnlyField">
    <attribute ctor="M:JetBrains.Annotations.NotNullAttribute.#ctor" />
  </member>
  <member name="F:JetBrains.Annotations.Dnx.TestClass._otherField">
    <attribute ctor="M:JetBrains.Annotations.NotNullAttribute.#ctor" />
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.#ctor(System.String)">
    <parameter name="value">
      <attribute ctor="M:JetBrains.Annotations.NotNullAttribute.#ctor" />
    </parameter>
  </member>
  <member name="P:JetBrains.Annotations.Dnx.TestClass.Value">
    <attribute ctor="M:JetBrains.Annotations.CanBeNullAttribute.#ctor" />
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.set_Value(System.String)">
    <attribute ctor="M:JetBrains.Annotations.NotNullAttribute.#ctor" />
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.SomeMethod(System.Collections.Generic.IEnumerable{System.String},System.String)">
    <attribute ctor="M:JetBrains.Annotations.StringFormatMethodAttribute.#ctor(System.String)">
      <argument>format</argument>
    </attribute>
    <attribute ctor="M:JetBrains.Annotations.ContractAnnotationAttribute.#ctor(System.String)">
      <argument>values:null =&gt; halt</argument>
    </attribute>
    <parameter name="values">
      <attribute ctor="M:JetBrains.Annotations.NotNullAttribute.#ctor" />
      <attribute ctor="M:JetBrains.Annotations.ItemNotNullAttribute.#ctor" />
      <attribute ctor="M:JetBrains.Annotations.NoEnumerationAttribute.#ctor" />
    </parameter>
    <parameter name="format">
      <attribute ctor="M:JetBrains.Annotations.CanBeNullAttribute.#ctor" />
    </parameter>
  </member>
  <member name="M:JetBrains.Annotations.Dnx.TestClass.GenericMethod``1(System.Collections.Generic.IEnumerable{``0})">
    <parameter name="values">
      <attribute ctor="M:JetBrains.Annotations.NotNullAttribute.#ctor" />
      <attribute ctor="M:JetBrains.Annotations.NoEnumerationAttribute.#ctor" />
    </parameter>
  </member>
</assembly>
```

It's not 100% yet, but it's getting there :grin:
