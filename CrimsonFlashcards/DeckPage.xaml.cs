using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;

#if WINDOWS
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
#endif

namespace CrimsonFlashcards
{
    public partial class DeckPage : ContentPage
    {
        private Deck deck;
        private readonly Action<Deck> onDeckChanged;

        public DeckPage(Deck deck, Action<Deck> onDeckChanged = null)
        {
            InitializeComponent();
            this.deck = deck;
            this.onDeckChanged = onDeckChanged;
            BindingContext = deck;
        }

        private async void OnStartTestClicked(object sender, EventArgs e)
        {
            bool startWithBack = StartWithBackSwitch.IsToggled;
            await Navigation.PushAsync(new TestPage(deck, startWithBack));
        }

        private async void OnExportCsvClicked(object sender, EventArgs e)
        {
            try
            {
                string folderPath = null;

#if WINDOWS
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                var hwnd = ((MauiWinUIWindow)Application.Current.Windows[0].Handler.PlatformView).WindowHandle;
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                var result = await folderPicker.PickSingleFolderAsync();
                if (result != null)
                {
                    folderPath = result.Path;
                }
                else
                {
                    await DisplayAlert("Отмена", "Выбор папки отменён", "OK");
                    return;
                }
#else
                await DisplayAlert("Ошибка", "Выбор папки доступен только на Windows", "OK");
                return;
#endif

                if (!string.IsNullOrEmpty(folderPath))
                {
                    if (deck.SaveToFile(folderPath, out string error))
                    {
                        await DisplayAlert("Успех", $"CSV-файл сохранён в:\n{folderPath}", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", $"Не удалось сохранить CSV: {error}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть проводник: {ex.Message}", "OK");
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            var editPage = new EditDeckPage(deck, updatedDeck =>
            {
                this.deck = updatedDeck;
                onDeckChanged?.Invoke(updatedDeck);
            });
            await Navigation.PushAsync(editPage);
        }

        private void OnFinishButtonClicked(object sender, EventArgs e)
        {
            onDeckChanged?.Invoke(deck);
            Navigation.PopAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (!isDeleting)
            {
                onDeckChanged?.Invoke(deck);
            }
        }

        private static readonly string SAVE_FOLDER_PATH = Path.Combine(FileSystem.AppDataDirectory, "CrimsonFlashcards", "Decks");

        private bool isDeleting = false;

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            isDeleting = true;
            string csvPath = Path.Combine(SAVE_FOLDER_PATH, $"{deck.Id}.csv");
            string statsPath = Path.Combine(SAVE_FOLDER_PATH, $"{deck.Id}.stats.json");

            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
                Debug.WriteLine($"{csvPath} deleted");
            }
            else Debug.WriteLine($"{csvPath} does not exist");

            if (File.Exists(statsPath))
            {
                File.Delete(statsPath);
                Debug.WriteLine($"{statsPath} deleted");
            }
            else Debug.WriteLine($"{statsPath} does not exist");

            onDeckChanged?.Invoke(null);
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}