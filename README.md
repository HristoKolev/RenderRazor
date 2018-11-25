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

# How to use

* Razor template as a string:

The first line should aways be: `@inherits TemplateBase<MyModel>` where `MyModel` is the .NET class that you want to use as the `@Model`. You can also use the `Fully Qualified Type Name` there if that helps you. :) 

```C#
const string TemplateString = "@inherits TemplateBase<MyModel>\nHello @Model.Name, welcome to Razor World!";
```

* The .NET class that you want to pass as the @Model:

```C#
var model = new MyModel
{
    Name = "Cats"
};
```

* Create a renderer:

```C#
var render = new RazorRenderer<MyModel>(TemplateString);
```

* Call the compile method:

This is very slow.
It calls both the Razor and the C# compillers.
This simple example executes 100 times for ~300ms on my Intel I7-6700K Workstation.
When you create a renderer, be sure to reuse it.

```C#
render.Compile();
```

* Call the render:

This is very fast. 
This simple example executes 1 000 000 times for ~300ms on my Intel I7-6700K Workstation.
```C#
string result = await render.Render(model);
Assert.Equal("Hello Cats, welcome to Razor World!", result);
```
