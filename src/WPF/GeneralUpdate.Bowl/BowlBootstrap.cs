using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Plugins.Core;
using System.Diagnostics;

namespace GeneralUpdate.Bowl
{
    public class BowlBootstrap
    {
        public async void Test() 
        {
            var kernel = Kernel.Builder.Build();
            var time = kernel.ImportFunctions(new TimePlugin());
            var result = await kernel.RunAsync(time["Today"]);
            Debug.WriteLine(result);
        }
    }
}
