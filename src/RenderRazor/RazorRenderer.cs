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

    public class RazorRenderer<T>
    {
        public string TemplateString { get; }

        private readonly Assembly[] referencedAssemblies;

        public string TemplateCode { get; private set; }

        private Type TemplateType { get; set; }

        public RazorRenderer(string templateString, Assembly[] referencedAssemblies = null)
        {
            this.TemplateString = templateString;
            this.referencedAssemblies = referencedAssemblies;
        }

        public void Compile()
        {
            byte[] templateBytes = Encoding.UTF8.GetBytes(this.TemplateString);

            string templateCode = CompileToCode(templateBytes);
            this.TemplateCode = templateCode;

            var templateType = CompileToType(templateCode, this.referencedAssemblies);
            this.TemplateType = templateType;
        }

        private static Type CompileToType(string templateCode, Assembly[] referencedAssemblies)
        {
            var syntaxTrees = new[]
            {
                CSharpSyntaxTree.ParseText(templateCode)
            };

            var modelAssemblyReference = MetadataReference.CreateFromFile(typeof(T).Assembly.Location);
            var allReferences = RazorRendererStore.DefaultReferences.Value.Concat(new[] { modelAssemblyReference }).ToList();

            if (referencedAssemblies != null)
            {
                allReferences.AddRange(referencedAssemblies.Select(x => MetadataReference.CreateFromFile(x.Location)));
            }

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

        private static string CompileToCode(byte[] templateBytes)
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

        public async Task<string> Render(T model)
        {
            var template = (TemplateBase<T>)Activator.CreateInstance(this.TemplateType);

            template.Model = model;
            await template.ExecuteAsync();

            return template.Result;
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

    internal static class RazorRendererStore
    {
        public static readonly Lazy<MetadataReference[]> DefaultReferences = new Lazy<MetadataReference[]>(() => new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RazorCompiledItemAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TemplateBase<>).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Linq.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"))
        });
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
