using Microsoft.SemanticKernel;

namespace GeneralUpdate.Bowl
{
    public class BowlBootstrap
    {
        public async Task Launch() 
        {
            var builder = new KernelBuilder();

            builder.WithAzureChatCompletionService(
                     "gpt-35-turbo",                  // Azure OpenAI Deployment Name
                     "https://contoso.openai.azure.com/", // Azure OpenAI Endpoint
                     "...your Azure OpenAI Key...");      // Azure OpenAI Key

            // Alternative using OpenAI
            //builder.WithOpenAIChatCompletionService(
            //         "gpt-3.5-turbo",               // OpenAI Model name
            //         "...your OpenAI API Key...");     // OpenAI API Key

            var kernel = builder.Build();

            var prompt = @"{{$input}}

One line TLDR with the fewest words.";

            var summarize = kernel.CreateSemanticFunction(prompt, maxTokens: 100);

            string text1 = @"
1st Law of Thermodynamics - Energy cannot be created or destroyed.
2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

            string text2 = @"
1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
2. The acceleration of an object depends on the mass of the object and the amount of force applied.
3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

            Console.WriteLine(await summarize.InvokeAsync(text1));

            Console.WriteLine(await summarize.InvokeAsync(text2));

            // Output:
            //   Energy conserved, entropy increases, zero entropy at 0K.
            //   Objects move in response to forces.
        }
    }
}
