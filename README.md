# XCC
XAML Conditional Compilation

Welcome to XCC, a preprocessor adding conditional compilation support to XAML files. Enable XCC in your project by installing a tiny NuGet package. Supporting *Windows Universal*, *WPF*, *Managed C++*, and *Xamarin Forms* projects. Other project types have not been tested.

**Note**: XCC is a proof of concept. No guarantees whatsoever, use at your own risk.

## Concept

XCC introduces custom XML namespaces for defining conditions. When an XML element or attribute is in a custom condition namespace, it is included or excluded based on the compilation symbols. XCC includes a MSBuild task that preprocesses the XAML just before handing it over to the XAML compiler.

The following XML namespace declarations define debug and release conditions:
```xml
<Page
  xmlns:debug="condition:DEBUG"
  xmlns:release="condition:!DEBUG" />
```
Any XML element and attribute prefixed with **debug:** will be included when the project is compiled with DEBUG symbol enabled. The XML elements and attributes are excluded when the DEBUG symbol is not defined. Condition expression support is limited to a single symbol and the use of the ! (not) operator. Operators || and && are not supported (yet).

Consider the following XAML snippet:
```xml
<Page ...
  xmlns:debug="condition:DEBUG"
  xmlns:release="condition:!DEBUG">
  <StackPanel>
    <debug:TextBlock Text="I'm only available in DEBUG" />
    <release:TextBlock Text="I'm only available in RELEASE" />
    <TextBlock Text="I'm always available" debug:Foreground="Red" />
  </StackPanel>
</Page>
```
When compiled with **DEBUG** this results in:
```xml
<Page ...>
  <StackPanel>
    <TextBlock Text="I'm only available in DEBUG" />

    <TextBlock Text="I'm always available" Foreground="Red" />
  </StackPanel>
</Page>
```
And when compiled **without DEBUG**:
```xml
<Page ...>
  <StackPanel>

    <TextBlock Text="I'm only available in RELEASE" />
    <TextBlock Text="I'm always available" />
  </StackPanel>
</Page>
```
The debug and release conditions are just an example of what is possible. You can define your own XML namespaces with your own conditions. You can, for instance, define conditions for **WINDOWS_APP** and **WINDOWS_PHONE_APP** to include and exclude XAML based on the project compile target.
```xml
<Page ...
  xmlns:win81="condition:WINDOWS_APP"
  xmlns:wp81="condition:WINDOWS_PHONE_APP" />
```
It is important to understand that XCC does not modify the source XAML files. It does create temporary XAML files in the obj folder of your project, and redirects the XAML compiler to use these files instead of the original XAML files.

## Designer and Intellisense support

The XAML editor and compiler in Visual Studio are not aware of custom XML namespaces and happily raise errors when you try to declare and use custom XML namespaces. To avoid this, you'll need to instruct the XAML processor to ignore the custom namespaces. Do this by adding the prefixes to the **mc:Ignorable** attribute (the mc prefix is reserved for markup compatibility language features).
```xml
<Page
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  xmlns:debug="condition:DEBUG"
  xmlns:release="condition:!DEBUG"
  mc:Ignorable="debug release" />
```
The only thing left to solve is Intellisense support. Intellisense is lost when applying a custom prefix to an XML element. Attribute Intellisense is not supported, but you can enable Intellisense for the element content by defining the **mc:ProcessContent** attribute. The attribute value is a space-separated list of element names and instructs the XAML processor to process the content even though the immediate parent is ignored. 

The following snippet demonstrates the use of **mc:ProcessContent**, so that Intellisense is available when editing the content of the grid.
```xml
<Page
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  xmlns:debug="condition:DEBUG"
  xmlns:release="condition:!DEBUG"
  mc:Ignorable="debug release"
  mc:ProcessContent="debug:* release:*">

  <debug:Grid>
    <!-- intellisense enabled here! -->
  </debug:Grid>
</Page>
```
