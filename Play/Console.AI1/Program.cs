// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

//using Microsoft.SemanticKernel.VectorStores.Memory;
//using Microsoft.SemanticKernel.Embeddings;
//using Microsoft.SemanticKernel.VectorStores;

//var kernel = Kernel.CreateBuilder()
//    .AddAzureOpenAITextEmbeddingGeneration("embedding-deployment", "https://your-openai-endpoint.openai.azure.com", "your-api-key")
//    .AddVolatileVectorStore()  // Adds the latest vector store abstraction
//    .Build();

//var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGeneration>();
//var vectorStore = new VolatileVectorStore();
//var memory = new VectorSearchMemory(vectorStore, embeddingGenerator);

//var embedding = await embeddingGenerator.GenerateEmbeddingAsync("Azure AI");
//await vectorStore.UpsertAsync("my_collection", new[]
//{
//    new VectorRecord("1", embedding, new Dictionary<string, object> { { "text", "Azure AI" } }, DateTime.UtcNow)
//});

//var queryEmbedding = await embeddingGenerator.GenerateEmbeddingAsync("cloud computing");
//var results = await vectorStore.GetNearestMatchesAsync("my_collection", queryEmbedding, 2);