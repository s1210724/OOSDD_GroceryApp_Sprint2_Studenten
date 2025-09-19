using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Data.Repositories;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            //Maak de lijst AvailableProducts leeg
            AvailableProducts.Clear();

            //Haal de lijst met producten op
            List<Product> products = _productService.GetAll();

            // haal lijst met producten op die al in de boodschappenlijst staan
            List<GroceryListItem> groceryListItems = _groceryListItemsService.GetAllOnGroceryListId(GroceryList.Id);

            if (groceryListItems.Count == 0)
            {
                // als er nog geen producten in de boodschappenlijst staan, voeg dan alle producten met voorraad toe
                foreach (var product in products)
                {
                    if (product.Stock > 0) AvailableProducts.Add(product);
                }
                return;
            }

            //Controleer of het product al op de boodschappenlijst staat, zo niet zet het in de AvailableProducts lijst
            //Houdt rekening met de voorraad (als die nul is kun je het niet meer aanbieden). 

            // check alle producten
            foreach (var product in products) {
                // vergelijk elk product met elk product in de huidige winkelmand
                foreach (var item in groceryListItems) {
                    // stop met vergelijken als het product gevonden is of als de voorraad 0 of kleiner is
                    if (product.Id == item.Product.Id || product.Stock <= 0) break;
                    // als het product niet in het winkelmandje staat en het door de lijst is zet deze in AvailableProducts
                    if (item.Product.Id != product.Id && item == groceryListItems.Last())
                    {
                        AvailableProducts.Add(product);
                        break;
                    }
                }
            }
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            //Controleer of het product bestaat en dat de Id > 0
            if (product != null && product.Id > 0)
            {

            //Maak een GroceryListItem met Id 0 en vul de juiste productid en grocerylistid
                GroceryListItem newItem = new(0, GroceryList.Id, product.Id, 1);

            //Voeg het GroceryListItem toe aan de dataset middels de _groceryListItemsService
                _groceryListItemsService.Add(newItem);

            //Werk de voorraad (Stock) van het product bij en zorg dat deze wordt vastgelegd (middels _productService)
                product.Stock -= 1;
                _productService.Update(product);

            //Werk de lijst AvailableProducts bij, want dit product is niet meer beschikbaar
                GetAvailableProducts();
                AvailableProducts.Remove(product);

            //call OnGroceryListChanged(GroceryList);
                OnGroceryListChanged(GroceryList);
            }
        }
    }
}
