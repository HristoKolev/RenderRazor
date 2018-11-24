namespace CoreTest
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;

    using RenderRazor;

    public class Program
    {
        static async Task Main()
        {
            byte[] templateBytes = Encoding.UTF8.GetBytes(@"@inherits TemplateBase<MyModel> 
                Hello @Model.Name, welcome to Razor World!");

            var model = new MyModel
            {
                Name = "Cats"
            };

            var render = RazorRenderer.Create<MyModel>(templateBytes);

            await render(model);

            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 1000000; i++)
            {
                await render(model);
            }

            Console.WriteLine(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public class MyModel
    {
        public string Name { get; set; }
    }
}
