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