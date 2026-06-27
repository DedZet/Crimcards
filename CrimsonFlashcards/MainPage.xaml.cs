using Microsoft.Maui.Storage;
using System.IO;
using System.Text;
using System;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace CrimsonFlashcards;

public partial class MainPage : ContentPage
{
    private static readonly string SAVE_FOLDER_PATH = Path.Combine(FileSystem.AppDataDirectory, "CrimsonFlashcards", "Decks");

    private ContentView homeView;
    private ContentView statisticsView;
    private readonly ContentView createDeckView;

    private ContentView currView;
    private Button selectedButton;

    private List<Deck> decks = new();

    public MainPage()
    {
        if (!Directory.Exists(SAVE_FOLDER_PATH)) Directory.CreateDirectory(SAVE_FOLDER_PATH);

        System.Diagnostics.Debug.WriteLine($"Decks folder: {SAVE_FOLDER_PATH}");
        InitializeComponent();

        LoadAllDecks();

        homeView = CreateHomeView();
        statisticsView = CreateStatisticsView();
        createDeckView = CreateCreateDeckView();

        currView = homeView;
        ContentArea.Content = currView;
        SetSelectedButton(HomeButton);
    }

    private async void OnOpenFolderClicked(object sender, EventArgs e)
    {
        await OpenSaveFolder();
    }

    private async Task OpenSaveFolder()
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                System.Diagnostics.Process.Start("explorer.exe", SAVE_FOLDER_PATH);
            }
            else if (DeviceInfo.Platform == DevicePlatform.macOS)
            {
                System.Diagnostics.Process.Start("open", SAVE_FOLDER_PATH);
            }
            else
            {
                await Launcher.OpenAsync(new Uri($"file://{SAVE_FOLDER_PATH}"));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось открыть папку: {ex.Message}", "OK");
        }
    }

    private void LoadAllDecks()
    {
        decks.Clear();
        var files = Directory.GetFiles(SAVE_FOLDER_PATH, "*.csv");
        foreach (var file in files)
        {
            try
            {
                var deck = Deck.LoadFromFile(file);
                decks.Add(deck);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки {file}: {ex.Message}");
            }
        }

        var statsFiles = Directory.GetFiles(SAVE_FOLDER_PATH, "*.stats.json");
        foreach (var statsFile in statsFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(statsFile);
            if (Guid.TryParse(fileName, out Guid guid))
            {
                string csvFile = Path.Combine(SAVE_FOLDER_PATH, $"{guid}.csv");
                if (!File.Exists(csvFile))
                {
                    try
                    {
                        File.Delete(statsFile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Не удалось удалить {statsFile}: {ex.Message}");
                    }
                }
            }
        }
    }

    private void SaveDeck(Deck deck)
    {
        if (deck == null) return;
        if (!Directory.Exists(SAVE_FOLDER_PATH))
            Directory.CreateDirectory(SAVE_FOLDER_PATH);

        if (deck.SaveToFile(SAVE_FOLDER_PATH, out string error))
        {
            
        }
        else
        {
            DisplayAlert("Ошибка", $"Не удалось сохранить колоду: {error}", "OK");
        }
    }

    private void OnHomeClicked(object sender, EventArgs e)
    {
        SwitchContentWithAnimation(homeView);
        SetSelectedButton(HomeButton);
    }

    private void OnStatisticsClicked(object sender, EventArgs e)
    {
        statisticsView = CreateStatisticsView();
        SwitchContentWithAnimation(statisticsView);
        SetSelectedButton(StatisticsButton);
    }

    private async void OnCreateDeckClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new EditDeckPage(null, deck =>
        {
            decks.Add(deck);
            SaveDeck(deck);
            RefreshHomeView();
        }));
    }

    private async void OnImportDeckClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI,     new[] { ".csv" } },
                    { DevicePlatform.macOS,     new[] { ".csv" } },
                    { DevicePlatform.Android,   new[] { ".csv" } }
                });

            var options = new PickOptions
            {
                PickerTitle = "Выберите CSV-файл колоды",
                FileTypes = customFileType,
            };

            var result = await FilePicker.PickAsync(options);
            if (result == null)
                return;

            using var fileStream = new FileStream(result.FullPath, FileMode.Open, FileAccess.Read);
            Deck importedDeck = new Deck();
            importedDeck.ImportCSV(fileStream);

            SaveDeck(importedDeck);

            decks.Add(importedDeck);
            RefreshHomeView();

            await DisplayAlert("Успех", "Колода успешно импортирована", "OK");
            RefreshHomeView();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось импортировать колоду: {ex.Message}", "OK");
            RefreshHomeView();
        }
        
    }

    private async Task SwitchContentWithAnimation(ContentView newView)
    {
        if (currView == newView) return;

        var oldView = currView;
        currView = newView;

        await oldView.FadeTo(0, 200);
        ContentArea.Content = newView;
        newView.Opacity = 0;
        await newView.FadeTo(1, 200);
    }

    private async Task AnimateButtonPress(Button btn)
    {
        if (btn == null) return;
        await btn.ScaleTo(0.95, 80, Easing.CubicInOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicOut);
    }

    private void SetSelectedButton(Button activeButton)
    {
        if (selectedButton != null)
        {
            selectedButton.BackgroundColor = Colors.Transparent;
            selectedButton.TextColor = (Color)Application.Current.Resources["TextPrimary"];
        }

        selectedButton = activeButton;
        selectedButton.BackgroundColor = (Color)Application.Current.Resources["CrimsonAccent"];
        selectedButton.TextColor = Colors.White;
    }

    private ContentView CreateHomeView()
    {
        var layout = new VerticalStackLayout { Spacing = 20, Padding = 10 };

        layout.Children.Add(new Label
        {
            Text = "🏠 Главная страница",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = (Color)Application.Current.Resources["CrimsonAccent"]
        });

        if (decks.Count == 0)
        {
            layout.Children.Add(new Label
            {
                Text = "У вас нет колод",
                FontSize = 16,
                TextColor = (Color)Application.Current.Resources["TextSecondary"]
            });
        }
        else
        {
            foreach (var deck in decks)
            {
                var card = CreateDeckCard(deck.Theme, deck.FaceLang, deck.BackLang, deck.getLength(), deck);
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnDeckTapped(deck);
                card.GestureRecognizers.Add(tapGesture);
                layout.Children.Add(card);
            }
        }

        return new ContentView { Content = layout };
    }

    public void RefreshHomeView()
    {
        homeView = CreateHomeView();
        if (currView == homeView)
        {
            LoadAllDecks();
            ContentArea.Content = homeView;
        }
        
    }

    private async void OnDeckTapped(Deck deck)
    {
        var deckPage = new DeckPage(deck, updatedDeck =>
        {
            if (updatedDeck == null)
            {
                decks.Remove(deck);
            }
            else
            {
                SaveDeck(updatedDeck);
            }
            RefreshHomeView();
        });
        await Navigation.PushAsync(deckPage);
    }

    private ContentView CreateStatisticsView()
    {
        var layout = new VerticalStackLayout { Spacing = 20, Padding = 10 };

        layout.Children.Add(new Label
        {
            Text = "🧮 Статистика",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = (Color)Application.Current.Resources["CrimsonAccent"]
        });

        int totalCards = 0;
        int learnedCards = 0;
        int dueCards = 0;
        double avgEasiness = 0;
        int countWithEasiness = 0;

        foreach (var deck in decks)
        {
            var stats = deck.GetStatistics();
            totalCards += stats.TotalCards;
            learnedCards += stats.LearnedCards;
            dueCards += stats.DueCards;
            avgEasiness += stats.AverageEasiness * stats.TotalCards;
            countWithEasiness += stats.TotalCards;
        }
        if (countWithEasiness > 0)
            avgEasiness /= countWithEasiness;

        var statsGrid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection { new RowDefinition(), new RowDefinition() },
            ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(), new ColumnDefinition() },
            RowSpacing = 15,
            ColumnSpacing = 15
        };

        var stat1 = CreateStatCard("Всего карточек", totalCards.ToString(), "📚");
        statsGrid.Children.Add(stat1);
        Grid.SetRow(stat1, 0);
        Grid.SetColumn(stat1, 0);

        var stat2 = CreateStatCard("Изучено", learnedCards.ToString(), "✅");
        statsGrid.Children.Add(stat2);
        Grid.SetRow(stat2, 0);
        Grid.SetColumn(stat2, 1);

        var stat3 = CreateStatCard("К повторению", dueCards.ToString(), "⏰");
        statsGrid.Children.Add(stat3);
        Grid.SetRow(stat3, 1);
        Grid.SetColumn(stat3, 0);

        var stat4 = CreateStatCard("Средняя лёгкость", avgEasiness.ToString("F2"), "📈");
        statsGrid.Children.Add(stat4);
        Grid.SetRow(stat4, 1);
        Grid.SetColumn(stat4, 1);

        layout.Children.Add(statsGrid);
        return new ContentView { Content = layout };
    }

    private Border CreateStatCard(string title, string value, string icon)
    {
        return new Border
        {
            BackgroundColor = (Color)Application.Current.Resources["SurfaceDark"],
            StrokeThickness = 0,
            Padding = 15,
            Margin = 0,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = icon, FontSize = 24, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = value, FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = title, FontSize = 14, TextColor = (Color)Application.Current.Resources["TextSecondary"], HorizontalOptions = LayoutOptions.Center }
                }
            }
        };
    }

    private Border CreateDeckCard(string theme, string faceLang, string backLang, int length, Deck deck)
    {
        var fs = new FormattedString();
        fs.Spans.Add(new Span { Text = "Лицевая: ", TextColor = (Color)Application.Current.Resources["TextSecondary"] });
        fs.Spans.Add(new Span { Text = faceLang, TextColor = Colors.White });
        fs.Spans.Add(new Span { Text = "        Обратная: ", TextColor = (Color)Application.Current.Resources["TextSecondary"] });
        fs.Spans.Add(new Span { Text = backLang, TextColor = Colors.White });

        return new Border
        {
            BackgroundColor = (Color)Application.Current.Resources["SurfaceDark"],
            StrokeThickness = 0,
            Padding = 15,
            Margin = 0,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = theme, FontSize = 24, HorizontalOptions = LayoutOptions.Center },
                    new Label { FormattedText = fs, FontSize = 14, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = $"Карточек: {length}", FontSize = 14, TextColor = (Color)Application.Current.Resources["TextSecondary"], HorizontalOptions = LayoutOptions.Center }
                }
            }
        };
    }

    private ContentView CreateCreateDeckView()
    {
        var layout = new VerticalStackLayout { Spacing = 20, Padding = 10 };

        layout.Children.Add(new Label
        {
            Text = "✏️ Создание новой колоды",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = (Color)Application.Current.Resources["CrimsonAccent"]
        });

        var themeEntry = new Entry
        {
            Placeholder = "Тема",
            BackgroundColor = (Color)Application.Current.Resources["SurfaceDark"],
            TextColor = Colors.White,
            PlaceholderColor = (Color)Application.Current.Resources["TextSecondary"],
            FontSize = 16,
        };

        var descEntry = new Editor
        {
            Placeholder = "Описание (необязательно)",
            BackgroundColor = (Color)Application.Current.Resources["SurfaceDark"],
            TextColor = Colors.White,
            PlaceholderColor = (Color)Application.Current.Resources["TextSecondary"],
            HeightRequest = 100
        };

        var faceLangEntry = new Entry
        {
            Placeholder = "Язык лицевой стороны",
            BackgroundColor = (Color)Application.Current.Resources["SurfaceDark"],
            TextColor = Colors.White,
            PlaceholderColor = (Color)Application.Current.Resources["TextSecondary"],
            FontSize = 16,
        };

        var backLangEntry = new Entry
        {
            Placeholder = "Язык обратной стороны",
            BackgroundColor = (Color)Application.Current.Resources["SurfaceDark"],
            TextColor = Colors.White,
            PlaceholderColor = (Color)Application.Current.Resources["TextSecondary"],
            FontSize = 16,
        };

        var createButton = new Button
        {
            Text = "Создать колоду",
            BackgroundColor = (Color)Application.Current.Resources["CrimsonAccent"],
            TextColor = Colors.White,
            CornerRadius = 12,
            FontSize = 16,
            Padding = 12
        };

        createButton.Clicked += async (s, e) =>
        {
            await AnimateButtonPress(s as Button);

            if (string.IsNullOrWhiteSpace(themeEntry.Text) ||
                string.IsNullOrWhiteSpace(faceLangEntry.Text) ||
                string.IsNullOrWhiteSpace(backLangEntry.Text))
            {
                await DisplayAlert("Ошибка", "Заполните тему и оба языка", "OK");
                return;
            }

            var newDeck = new Deck(
                themeEntry.Text,
                descEntry.Text,
                faceLangEntry.Text,
                backLangEntry.Text
            );

            var addPage = new AddCardsPage(newDeck, deck =>
            {
                decks.Add(deck);
                SaveDeck(deck);
                RefreshHomeView();
            }, isEditing: false);

            await Navigation.PushAsync(addPage);

            themeEntry.Text = string.Empty;
            descEntry.Text = string.Empty;
            faceLangEntry.Text = string.Empty;
            backLangEntry.Text = string.Empty;
        };

        layout.Children.Add(themeEntry);
        layout.Children.Add(descEntry);
        layout.Children.Add(faceLangEntry);
        layout.Children.Add(backLangEntry);
        layout.Children.Add(createButton);

        return new ContentView { Content = layout };
    }
}