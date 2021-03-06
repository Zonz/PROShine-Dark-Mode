﻿using PROBot;
using PROProtocol;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PROShine
{
    public partial class TeamView : UserControl
    {
        private BotClient _bot;
        private Point _startPoint;
        private int pokemonUId { get; set; }

        public TeamView(BotClient bot)
        {
            _bot = bot;
            InitializeComponent();           
        }

        private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem =
                    FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    // Find the data behind the ListViewItem
                    Pokemon pokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject("PROShinePokemon", pokemon);
                    DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                }
            }
        }

        private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PROShinePokemon"))
            {
                Pokemon sourcePokemon = e.Data.GetData("PROShinePokemon") as Pokemon;
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem =
                    FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    // Find the data behind the ListViewItem
                    Pokemon destinationPokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                    lock (_bot)
                    {
                        if (_bot.Game != null)
                        {
                            _bot.Game.SwapPokemon(sourcePokemon.Uid, destinationPokemon.Uid);
                            
                        }
                    }
                }
            }
        }

        private void List_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("PROShinePokemon") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        //private void PokemonsListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    _startPoint = e.GetPosition(null);
        //    ListView listView = sender as ListView;
        //    ListViewItem listViewItem =
        //        FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
        //    if (listView != null)
        //    {
        //        Pokemon pokemon = (Pokemon)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

        //        if (!(pokemon.Uid >= 1 && pokemon.Uid <= _bot.Game.Team.Count))
        //        {
        //            return;
        //        }
        //        if (_bot.Game.Team[pokemon.Uid - 1].ItemHeld != "")
        //        {
        //            pokemonUId = pokemon.Uid;
        //            Item_Taker.Visibility = Visibility.Visible;
        //        }
        //        else
        //        {
        //            Item_Taker.Visibility = Visibility.Hidden;
        //            return;
        //        }
        //    }
        //}
        private void PokemonsListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PokemonsListView.SelectedItems.Count <= 0)
                return;
            else
            {
                PokemonItemContextMenu();
            }
        }

        private void MenuItemGiveItem_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonsListView.SelectedItems.Count <= 0)
                return;

            Pokemon pokemon = (Pokemon)PokemonsListView.SelectedItems[0];
            string itemName = ((MenuItem)e.OriginalSource).Header.ToString();
            lock (_bot)
            {
                InventoryItem item = _bot.Game.Items.Find(i => i.Name == itemName);
                _bot.Game.SendGiveItem(pokemon.Uid, item.Id);
            }
        }

        private void MenuItemTakeItem_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonsListView.SelectedItems.Count <= 0)
                return;

            Pokemon pokemon = (Pokemon)PokemonsListView.SelectedItems[0];
            lock (_bot)
            {
                _bot.Game.SendTakeItem(pokemon.Uid);
            }
        }
        private void UseItem_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonsListView.SelectedItems.Count <= 0)
                return;

            Pokemon pokemon = (Pokemon)PokemonsListView.SelectedItems[0];
            string itemName = ((MenuItem)e.OriginalSource).Header.ToString();
            lock (_bot)
            {
                InventoryItem item = _bot.Game.Items.Find(i => i.Name == itemName);
                _bot.Game.UseItem(item.Id, pokemon.Uid);
            }
        }

        public void PokemonItemContextMenu()
        {
            lock (_bot)
            {
                if (_bot.Game != null)
                {
                    if (_bot.Game.IsConnected)
                    {
                        Pokemon pokemon = (Pokemon)PokemonsListView.SelectedItems[0];
                        ContextMenu contextMenu = new ContextMenu();
                        if (!string.IsNullOrEmpty(pokemon.ItemHeld))
                        {
                            MenuItem takeItem = new MenuItem();
                            takeItem.Header = "Take " + pokemon.ItemHeld;
                            takeItem.Click += MenuItemTakeItem_Click;
                            contextMenu.Items.Add(takeItem);
                        }
                        if (_bot.Game.Items.Count > 0)
                        {
                            MenuItem giveItem = new MenuItem();
                            giveItem.Header = "Give item";
                            MenuItem useItem = new MenuItem();
                            useItem.Header = "Use Item";

                            _bot.Game.Items
                                .OrderBy(i => i.Name)
                                .ToList()
                                .ForEach(i => giveItem.Items.Add(i.Name));
                            _bot.Game.Items
                                .OrderBy(i => i.Name)
                                .ToList()
                                .ForEach(i => useItem.Items.Add(i.Name));

                            giveItem.Click += MenuItemGiveItem_Click;
                            useItem.Click += UseItem_Click;
                            contextMenu.Items.Add(giveItem);
                            contextMenu.Items.Add(useItem);
                        }
                        PokemonsListView.ContextMenu = contextMenu;
                    }
                }
            }
        }
    }
}
