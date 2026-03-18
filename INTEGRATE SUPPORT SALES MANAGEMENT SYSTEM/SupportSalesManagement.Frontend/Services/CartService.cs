using Blazored.LocalStorage;
using SupportSalesManagement.Frontend.Models;

namespace SupportSalesManagement.Frontend.Services
{
    public class CartService
    {
        private readonly ILocalStorageService _localStorage;
        private List<CartItem> _cartItems = new();
        public event Action? OnChange;

        public CartService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task InitializeCartAsync()
        {
            _cartItems = await _localStorage.GetItemAsync<List<CartItem>>("cart") ?? new List<CartItem>();
            NotifyStateChanged();
        }

        public async Task AddToCartAsync(Product product, int quantity = 1)
        {
            var existingItem = _cartItems.FirstOrDefault(i => i.Product.Id == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _cartItems.Add(new CartItem { Product = product, Quantity = quantity });
            }
            await _localStorage.SetItemAsync("cart", _cartItems);
            NotifyStateChanged();
        }

        public async Task RemoveFromCartAsync(Product product)
        {
            var item = _cartItems.FirstOrDefault(i => i.Product.Id == product.Id);
            if (item != null)
            {
                _cartItems.Remove(item);
                await _localStorage.SetItemAsync("cart", _cartItems);
                NotifyStateChanged();
            }
        }

        public async Task UpdateQuantityAsync(Product product, int quantity)
        {
            var item = _cartItems.FirstOrDefault(i => i.Product.Id == product.Id);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    _cartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                await _localStorage.SetItemAsync("cart", _cartItems);
                NotifyStateChanged();
            }
        }

        public async Task ClearCartAsync()
        {
            _cartItems.Clear();
            await _localStorage.RemoveItemAsync("cart");
            NotifyStateChanged();
        }

        public List<CartItem> GetCartItems()
        {
            return _cartItems;
        }

        public int GetCartCount()
        {
            return _cartItems.Sum(i => i.Quantity);
        }

        public decimal GetTotal()
        {
            return _cartItems.Sum(i => i.Product.Price * i.Quantity);
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

    public class CartItem
    {
        public Product Product { get; set; } = new();
        public int Quantity { get; set; }
    }
}
