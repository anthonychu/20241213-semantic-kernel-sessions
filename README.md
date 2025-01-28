# Semantic Kernel + Azure Container Apps dynamic sessions code interpreter demo

## Setup

1. Create a new Azure Container Apps dynamic sessions pool (type *Python code interpreter*).
    - Assign yourself the role of `Azure ContainerApps Session Executor` on the pool. If you create the pool in the Azure portal, it should automatically assign you this role.

1. Create an Azure OpenAI resource.
    - Assign yourself the role of `Cognitive Services OpenAI User` on the resource.

1. Use `dotnet user-secrets set` to add 2 secrets to the project:
    - `POOL_MANAGEMENT_ENDPOINT` - the management endpoint of the Azure Container Apps dynamic sessions pool (retrieve from portal)
    - `AZURE_OPENAI_ENDPOINT` - the endpoint of the Azure OpenAI resource (retrieve from portal)

## Running the demo

1. Run the project.
    ```bash
    dotnet run
    ```

1. In the prompt, ask a question that involves math. If you need questions, google for "math problems for kids" or use questions from [here](https://ny01001205.schoolwires.net/cms/lib/NY01001205/Centricity/Domain/360/WordProbPacket2.pdf).

    - In the output, you should see the Python code that was used to solve the problem and the response from the code interpreter.