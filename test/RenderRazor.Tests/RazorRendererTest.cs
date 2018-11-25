namespace RenderRazor.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Xunit;

    public class RazorRendererTest
    {
        [Fact]
        public async Task RenderForeachTemplate()
        {
            const string TemplateString = "@inherits TemplateBase<MyModel>\n@foreach(int i in Model.Ids) { @i }";

            var model = new MyModel
            {
                Ids = new List<int>
                {
                    1, 2, 3, 4
                }
            };

            var render = RazorRenderer.Create<MyModel>(TemplateString);

            string result = await render(model);

            Assert.Equal("1234", result);
        }

        [Fact]
        public async Task RenderSimpleTemplate()
        {
            const string TemplateString = "@inherits TemplateBase<MyModel>\nHello @Model.Name, welcome to Razor World!";

            var model = new MyModel
            {
                Name = "Cats",
                Ids = new List<int>
                {
                    1,
                    2,
                    3,
                    4
                }
            };

            var render = RazorRenderer.Create<MyModel>(TemplateString);

            string result = await render(model);

            Assert.Equal("Hello Cats, welcome to Razor World!", result);
        }
    }

    public class MyModel
    {
        public string Name { get; set; }

        public List<int> Ids { get; set; }
    }
}