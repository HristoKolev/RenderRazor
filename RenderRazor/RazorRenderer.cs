namespace RenderRazor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Razor.Hosting;
    using Microsoft.AspNetCore.Razor.Language;
    using Microsoft.AspNetCore.Razor.Language.Extensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    public static class RazorRenderer
    {
        private static readonly Lazy<MetadataReference[]> References = new Lazy<MetadataReference[]>(() => new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RazorCompiledItemAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TemplateBase<>).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"))
        });

        public static Func<T, Task<string>> Create<T>(byte[] templateBytes)
        {
            string templateCode = CompileToCode<T>(templateBytes);

            var templateType = CompileToType<T>(templateCode);

            return async model =>
            {
                var template = (TemplateBase<T>)Activator.CreateInstance(templateType);

                template.Model = model;

                await template.ExecuteAsync();

                return template.Result;
            };
        }

        private static Type CompileToType<T>(string templateCode)
        {
            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(templateCode)
            };

            var modelAssemblyReference = MetadataReference.CreateFromFile(typeof(T).Assembly.Location);
            var allReferences = References.Value.Concat(new[] { modelAssemblyReference });

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
            var compilation = CSharpCompilation.Create("test", syntaxTrees, allReferences, compilationOptions);

            Assembly generatedAssembly;

            using (var peStream = new MemoryStream())
            {
                var result = compilation.Emit(peStream);

                if (!result.Success)
                {
                    throw new ApplicationException($"An error occurred while compiling a template file. {string.Join(", ", result.Diagnostics)}");
                }

                generatedAssembly = Assembly.Load(peStream.ToArray());
            }

            var templateType = generatedAssembly.GetType($"{typeof(TemplateBase<>).Namespace}.Template");

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

            code = code.Replace($"TemplateBase<{typeof(T).Name}>", $"TemplateBase<{typeof(T).FullName}>");

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
        private readonly byte[] bytes;

        public InMemoryProjectItem(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public override Stream Read()
        {
            return new MemoryStream(this.bytes);
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override string BasePath { get; }

        public override string FilePath => "DummyFilePath";

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
