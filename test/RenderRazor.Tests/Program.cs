namespace RenderRazor.Tests
{
    using System.Threading.Tasks;

    using Xunit;

    public class RazorRendererTest
    {
        [Fact]
        public async Task RenderSimpleTemplate()
        {
            const string TemplateString = "@inherits TemplateBase<MyModel>\nHello @Model.Name, welcome to Razor World!";

            var model = new MyModel
            {
                Name = "Cats"
            };

            var render = RazorRenderer.Create<MyModel>(TemplateString);

            string result = await render(model);

            Assert.Equal("Hello Cats, welcome to Razor World!", result);
        }
    }

    public class MyModel
    {
        public string Name { get; set; }
    }
}