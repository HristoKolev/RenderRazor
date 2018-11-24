[![NuGet](https://img.shields.io/nuget/v/RenderRazor.svg?maxAge=2592000?style=plastic)](https://www.nuget.org/packages/RenderRazor/)  [![Build status](https://ci.appveyor.com/api/projects/status/2473o0aqvwc8ejgf?svg=true)](https://ci.appveyor.com/project/HristoKolev/renderrazor)

# RenderRazor
A small .NET Standard library that renders Razor templates to strings.

# How to installl
```sh
dotnet add package RenderRazor
```
or
```sh
Install-Package RenderRazor
```

# How to use.

* Razor template as a string:

```C#
const string TemplateString = "@inherits TemplateBase<MyModel>\nHello @Model.Name, welcome to Razor World!";
```

* The .NET class that you want to pass as the @model:

```C#
var model = new MyModel
{
    Name = "Cats"
};
```

* Create a renderer:

This is slow. When you create a renderer, be sure to reuse it.
```C#
var render = RazorRenderer.Create<MyModel>(TemplateString);
```

* Call the render:
This is very fast. The simple 
```C#
string result = await render(model);
Assert.Equal("Hello Cats, welcome to Razor World!", result);
```

