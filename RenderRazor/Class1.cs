namespace RenderRazor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Razor.Hosting;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.AspNetCore.Razor.Language.Extensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    public class Program
    {
        // initialize this on demand
        private static readonly MetadataReference[] References = {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RazorCompiledItemAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"))
        };

        private static Func<T, Task<string>> GetRenderer<T>(byte[] templateBytes)
        {
            string templateCode = CompileToCode<T>(templateBytes);

            var templateType = CompileToType(templateCode);

            return async model =>
            {
                var template = (TemplateBase<T>)Activator.CreateInstance(templateType);

                template.Model = model;

                await template.ExecuteAsync();

                return template.Result;
            };
        }

        private static Type CompileToType(string templateCode)
        {
            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(templateCode)
            };

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create("test", syntaxTrees, References, compilationOptions);

            Type templateType;

            using (var memStream = new MemoryStream())
            {
                var result = compilation.Emit(memStream);

                if (!result.Success)
                {
                    throw new ApplicationException($"An error occurred while compiling a template file. {string.Join(", ", result.Diagnostics)}");
                }

                var generatedAssembly = Assembly.Load(memStream.ToArray());

                templateType = generatedAssembly.GetType($"{typeof(TemplateBase<>).Namespace}.Template");
            }

            return templateType;
        }

        private static string CompileToCode<T>(byte[] templateBytes)
        {
            var fs = new DummyFs();

            var engine = RazorProjectEngine.Create(RazorConfiguration.Default, fs, builder =>
            {
                InheritsDirective.Register(builder);
                builder.SetNamespace(typeof(TemplateBase<>).Namespace);
            });

            string code = engine.Process(new InMemoryProjectItem(templateBytes)).GetCSharpDocument().GeneratedCode;

            code = code.Replace($"DummyTemplate<{typeof(T).Name}>", $"DummyTemplate<{typeof(T).FullName}>");

            return code;
        }
    }

    public abstract class TemplateBase<T>
    {
        private readonly StringBuilder builder = new StringBuilder();

        public T Model { get; set; }

        public void WriteLiteral(string literal)
        {
            this.builder.Append(literal);
        }

        public void Write(object obj)
        {
            this.builder.Append(obj);
        }

        public string Result => this.builder.ToString();

        public virtual Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }

    internal class InMemoryProjectItem : RazorProjectItem
    {
        private byte[] Bytes { get; }

        public InMemoryProjectItem(byte[] bytes)
        {
            this.Bytes = bytes;
        }

        public override Stream Read()
        {
            return new MemoryStream(this.Bytes);
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override string BasePath { get; }

        public override string FilePath => "Y://Generated.ddd";

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override string PhysicalPath { get; }

        public override bool Exists => true;
    }

    internal class DummyFs : RazorProjectFileSystem
    {
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            throw new NotImplementedException();
        }

        public override RazorProjectItem GetItem(string path)
        {
            throw new NotImplementedException();
        }
    }
}
