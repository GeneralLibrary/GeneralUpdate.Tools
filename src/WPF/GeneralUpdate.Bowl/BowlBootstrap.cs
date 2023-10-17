using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Plugins.Core;
//using RepoUtils;

namespace GeneralUpdate.Bowl
{
    public class BowlBootstrap
    {
        public void test() 
        {
            Console.WriteLine("======== Functions ========");

            // Load native plugin
            var text = new TextPlugin();

            // Use function without kernel
            var result = text.Uppercase("Ai4c research institute!");
            Console.WriteLine(result);
        }
    }
}
