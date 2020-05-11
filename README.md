## gRPC_Starter

### Setup
The command creates a new gRPC service in the ProductService folder, with a sample in it.
```sh
dotnet new grpc --name ProductService
```

### Run the created gRPC service
```sh
dotnet run
```
This command provides a convenient option to run the application from the source code with one command.
But unfortunately an error has occurred: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.

To work around this issue, configure Kestrel and the gRPC client to use HTTP/2 without TLS. You should only do this during development. Not using TLS will result in gRPC messages being sent without encryption.

Kestrel must configure an HTTP/2 endpoint without TLS in `Program.cs`:

```cs
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
    	.ConfigureWebHostDefaults(webBuilder =>
    	{
    	webBuilder.ConfigureKestrel(options =>
    		{
    		// Setup a HTTP/2 endpoint without TLS.
    		options.ListenLocalhost(5001, o => o.Protocols = HttpProtocols.Http2);
    		});
    
    	webBuilder.UseStartup<Startup>();
 });
```
then run `dotnet run` again.

### Examine the project files
- **'greet.proto'** – The 'Protos/greet.proto' file defines the 'Greeter' gRPC and is used to generate the gRPC server assets. For more information, see [Introduction to gRPC](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-3.1).
- **'Services' folder**: Contains the implementation of the 'Greeter' service.
- **'appSettings.json'** – Contains configuration data, such as protocol used by Kestrel. For more information, see [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1).
-**'Program.cs'** – Contains the entry point for the gRPC service. For more information, see .[NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1).
-**'Startup.cs'** – Contains code that configures app behavior. For more information, see [App startup](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-3.1).

Sample project explained:  https://docs.microsoft.com/en-us/aspnet/core/grpc/basics?view=aspnetcore-3.1

### Invoke the gRPC service
'gRPCurl' is a command-line tool that lets you interact with gRPC services. It's basically 'curl' for gRPC services.

In the terminal, go to the Protos folder and run the following command:
```sh
grpcurl -plaintext -proto greet.proto -d '{"name": "World"}' localhost:5001 greet.Greeter/SayHello
```
In the above example, the supplied body must be in JSON format. The body will be parsed and then transmitted to the server in the protobuf binary format.

You should see the following result.
```
{   
"message": "Hello World" 
}
```
See: https://github.com/fullstorydev/grpcurl

### Implement the gRPC service
Defining the contract

gRPC uses a contract-first approach to API development. Services and messages are defined in `*.proto` files:

So, move into the 'ProductService/Protos' folder and remove the 'greet.proto' file. Then, add a new file named 'product_service.proto' in the same folder and put the following content into it:
```cs
syntax = "proto3";

option csharp_namespace = "ProductService";

package catalog;

service ProductService {
  rpc CreateProduct(CreateProductRequest) returns (Product);

  rpc ListProducts(ListProductsRequest) returns (ListProductsResponse);
}

message CreateProductRequest {
  string name = 1;
  string description = 2;
}

message ListProductsRequest {
}

message ListProductsResponse {
  repeated Product products = 1;
}

message Product {
  string id = 1;
  string name = 2;
  string description = 3;
}
```
- 'product_service.proto' defines a 'ProductService' service.
- The 'ProductService' service defines two rpc calls 'CreateProduct' and 'ListProducts'.
    * 'CreateProduct' sends a 'CreateProductRequest' message and receives a `'Product' message.
    * 'ListProducts' sends a 'ListProductsRequest' message and receives a 'ListProducstsResponse' message.

The 'product_service.proto' file is included in the 'ProductService.csproj' by adding it to the <Protobuf> item group:

```cs
<ItemGroup> 		
    <Protobuf Include="Protos\greet.proto" GrpcServices="Server" />
    <Protobuf Include="..\Protos\product_service.proto" GrpcServices="Server" />
</ItemGroup>
```
It's a good recommendation to place 'Protos' folder outside of your gRPC service.

### Implementing the service
So, move into the 'Services' folder and remove the 'GreeterService.cs' file. Then, add a new file named 'ProductServiceImpl.cs' in the same folder and put the following content into it:
```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace ProductService
{
    public class ProductServiceImpl : ProductService.ProductServiceBase
    {
        private readonly static List<Product> _products = new List<Product>();

        public override Task<Product> CreateProduct(CreateProductRequest request, ServerCallContext context)
        {
            var product = new Product
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description
            };
            _products.Add(product);

            return Task.FromResult(product);
        }

        public override Task<ListProductsResponse> ListProducts(ListProductsRequest request, ServerCallContext context)
        {
            var response = new ListProductsResponse();
            response.Products.Add(_products);

            return Task.FromResult(response);
        }
    }
}
```
'CreateProduct' receives a 'CreateProductRequest' message, map the message to a product then store in a products collection.

'ListProducts' simply return the products collection.

### Configure gRPC
In 'Startup.cs' add the gRPC service to the routing pipeline through the 'MapGrpcService' method.
```cs
app.UseEndpoints(endpoints =>
{
	endpoints.MapGrpcService<ProductServiceImpl>();
		
	endpoints.MapGet("/", async context =>
	{
	await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
	});
}); 
```

### Invoke the gRPC service
In the terminal, run the following command to create a AirPods Pro product:
```sh
grpcurl -plaintext -proto product_service.proto -d '{"name": "AirPods Pro", "description": "Magic like yo u have never heard"}' localhost:5001 catalog.ProductService/CreateProduct
```
result:
```
{
  "id": "6e539316-188f-4a8e-a5b7-aada1ac9e829",
  "name": "AirPods Pro",
  "description": "Magic like you have never heard"
}
```
Run the following command to list products:
```sh
grpcurl -plaintext -proto product_service.proto localhost:5001 catalog.ProductService/ListProducts
```
result: 
```
{
  "products": [
    {
      "id": "dde4b313-a614-4425-95d4-1c6ced00d3e0",
      "name": "AirPods Pro",
      "description": "Magic like you have never heard"
    }
  ]
}
```

### Communication between gRPC services
Let's say that we have a 'CartService' gRPC service, would like to communicate with 'ProductService' gRPC service to retrieve product information using a product ID.

You can include the 'product_service.proto' file in the 'CartService.csproj' by adding it to the '<Protobuf>' item group and change 'GrpcServices' attribute from 'Server' to 'Client':

```cs
<ItemGroup>
		<Protobuf Include="Protos\product_service.proto" GrpcServices="Client" />
</ItemGroup>
```
Then register a gRPC client, the generic 'AddGrpcClient' extension method can be used within 'Startup.ConfigureServices', specifying the gRPC typed client class and service address:
The gRPC client type is registered as transient with dependency injection (DI). The client can now be injected and consumed directly in types created by DI.

See https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-3.1 for more information.

Reference: https://www.rb2.nl/en/university/grpc-services-with-.net-core-3.1
