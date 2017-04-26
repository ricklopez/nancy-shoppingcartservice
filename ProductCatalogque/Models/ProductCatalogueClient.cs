namespace ShoppingCart
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Net.Http;
  using System.Threading;
  using Newtonsoft.Json;
  using Polly;
  using ShoppingCart;

  //Client needs to be aware of the ProductCatalogue endpoints and how to deserialize the JSON data into the ProductType for the caller service
  public class ProductCatalogueClient : IProductCatalogueClient
  {
  
    //In this case our resiliance policy is to retry faile calls a couple times before giving up. We are using the Polly lib
    private static Policy exponentialRetryPolicy =
      Policy // Uses Polly's fluent API to set up a retry policy with an exponential back-off
        .Handle<Exception>()
        .WaitAndRetryAsync(
          3, 
          attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), (ex, _) => Console.WriteLine(ex.ToString()));
    
    //URL of the fake Product Catalog microservice
    private static string productCatalogueBaseUrl = 
      @"http://private-05cc8-chapter2productcataloguemicroservice.apiary-mock.com";
      
    private static string getProductPathTemplate =
      "/products?productIds=[{0}]";

    // Wraps calls to the Product Catalog microservices in the retry policy 
    public Task<IEnumerable<ShoppingCartItem>>
      GetShoppingCartItems(int[] productCatalogueIds) =>
      exponentialRetryPolicy
        .ExecuteAsync(async () => await GetItemsFromCatalogueService(productCatalogueIds).ConfigureAwait(false));
    
    //This method makes calls to the other methods in this class to complete a request and returns translated results
    private async Task<IEnumerable<ShoppingCartItem>>
      GetItemsFromCatalogueService(int[] productCatalogueIds)
    {
      var response = await
        RequestProductFromProductCatalogue(productCatalogueIds).ConfigureAwait(false);
      return await ConvertToShoppingCartItems(response).ConfigureAwait(false);
    }
    
    //Adds the product IDS as a query string parameter to the path of the /products endpoint
    private static async Task<HttpResponseMessage> RequestProductFromProductCatalogue(int[] productCatalogueIds)
    {
      var productsResource = string.Format(
        getProductPathTemplate, string.Join(",", productCatalogueIds));
      
      //Creates a client for making the HTTP GET request
      using (var httpClient = new HttpClient())
      {
        httpClient.BaseAddress = new Uri(productCatalogueBaseUrl);
        
        //Tells HttpClient to perform the HTTP GET asynchronously
        return await httpClient.GetAsync(productsResource).ConfigureAwait(false);
      }
    }
    
    //Uses Json.Net to deserialize the JSON from the Product Catalog microservices [line 64]
    private static async Task<IEnumerable<ShoppingCartItem>> ConvertToShoppingCartItems(HttpResponseMessage response)
    {
      response.EnsureSuccessStatusCode();
      var products = 
        JsonConvert.DeserializeObject<List<ProductCatalogueProduct>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
      return
        products //Creates a ShoppingCartItem for each product in the response 
          .Select(p => new ShoppingCartItem(
            int.Parse(p.ProductId),
            p.ProductName,
            p.ProductDescription,
            p.Price
        ));
    }

    // Uses a private class to represent the product data 
    private class ProductCatalogueProduct 
    {
      public string ProductId { get; set; }
      public string ProductName { get; set; }
      public string ProductDescription { get; set; }
      public Money Price { get; set; }
    }
    //NOTE: If you notice this model doesn't use all the properties returned in the JSON object.
    //  This is because the Shopping Cart microservice doesn't need all the information, so there's 
    //  no reason to read the remaining properties. Doing so would only introduce unneccessary coupling. 
  }
}