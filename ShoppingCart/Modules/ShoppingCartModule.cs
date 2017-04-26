namespace ShoppingCart.ShoppingCart
{
  using Nancy;
  using Nancy.ModelBinding;
  using EventFeed;
  
  //Declares this as a NancyModule. Nancy automatically dicovers all Nancy modules at startup.
  public class ShoppingCartModule : NancyModule
  {
    public ShoppingCartModule(IShoppingCartStore shoppingCartStore, IProductCatalogueClient productCatalogue, IEventStore eventStore)
      :base("/shoppingcart") //tells Nancy that all routes in this module start with /shopping cart
    {
      Get("/{userid:int}", parameters => //register this route in the pipeline via simple lambda expression
      {
        var userId = (int) parameters.userid;
        return shoppingCartStore.Get(userId); //return the user's shopping cart. Nancy serializes it to XML or JSON.
      });
      
      Post("/{userid:int}/items",
        async (parameters, _) =>
      {
        var productCatalogueIds = this.Bind<int[]>(); //Reads and deserializes the array of product IDs in the HTTP request 
        var userId = (int) parameters.userid;
        
        var shoppingCart = shoppingCartStore.Get(userId);
        var shoppingCartItems = await productCatalogue.GetShoppingCartItems(productCatalogueIds).ConfigureAwait(false); //Fetches the product information from the Product Catalog microservice
          
        shoppingCart.AddItems(shoppingCartItems, eventStore); //Add items to the cart 
        shoppingCartStore.Save(shoppingCart);
        
        return shoppingCart;
      });
    }
  }
}