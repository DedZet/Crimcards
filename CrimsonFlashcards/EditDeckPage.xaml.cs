using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CrimsonFlashcards
{
    public partial class EditDeckPage : ContentPage, INotifyPropertyChanged
    {
        private readonly Deck _deck;
        private readonly Action<Deck> _onSave;
        private readonly bool _isNewDeck;

        private string _theme;
        private string _description;
        private string _faceLang;
        private string _backLang;
        private ObservableCollection<CardEntry> _cards;

        public string PageTitle => _isNewDeck ? "Создание колоды" : "Редактирование колоды";

        public string Theme
        {
            get => _theme;
            set { _theme = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string FaceLang
        {
            get => _faceLang;
            set { _faceLang = value; OnPropertyChanged(); }
        }

        public string BackLang
        {
            get => _backLang;
            set { _backLang = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CardEntry> Cards
        {
            get => _cards;
            set { _cards = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand SaveCommand { get; }

        public EditDeckPage(Deck deck = null, Action<Deck> onSave = null)
        {
            InitializeComponent();

            _deck = deck ?? new Deck();
            _onSave = onSave;
            _isNewDeck = (deck == null);

            Theme = _deck.Theme;
            Description = _deck.Description;
            FaceLang = _deck.FaceLang;
            BackLang = _deck.BackLang;

            Cards = new ObservableCollection<CardEntry>();
            foreach (var kv in _deck.GetCards())
            {
                Cards.Add(new CardEntry
                {
                    FacePlaceholder = FaceLang,
                    BackPlaceholder = BackLang,
                    Face = kv.Key,
                    Back = kv.Value
                });
            }

            if (Cards.Count == 0)
                Cards.Add(CreateEmptyCard());

            AddCommand = new Command(OnAddCard);
            RemoveCommand = new Command<CardEntry>(OnRemoveCard);
            SaveCommand = new Command(OnSave);

            BindingContext = this;
        }

        private CardEntry CreateEmptyCard() =>
            new CardEntry
            {
                FacePlaceholder = FaceLang,
                BackPlaceholder = BackLang,
                Face = string.Empty,
                Back = string.Empty
            };

        private void OnAddCard() => Cards.Add(CreateEmptyCard());

        private void OnRemoveCard(CardEntry card)
        {
            if (Cards.Count > 1)
                Cards.Remove(card);
        }

        private async void OnSave()
        {
            if (string.IsNullOrWhiteSpace(Theme) ||
                string.IsNullOrWhiteSpace(FaceLang) ||
                string.IsNullOrWhiteSpace(BackLang))
            {
                await DisplayAlert("Ошибка", "Заполните тему и оба языка.", "OK");
                return;
            }

            foreach (var card in Cards)
            {
                if (string.IsNullOrWhiteSpace(card.Face) || string.IsNullOrWhiteSpace(card.Back))
                {
                    await DisplayAlert("Ошибка", "Все поля карточек должны быть заполнены.", "OK");
                    return;
                }
            }

            _deck.Theme = Theme;
            _deck.Description = Description;
            _deck.FaceLang = FaceLang;
            _deck.BackLang = BackLang;

            _deck.ClearCards();
            foreach (var card in Cards)
                _deck.AddCard(card.Face, card.Back);

            _onSave?.Invoke(_deck);
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Отмена", "Вы уверены, что хотите выйти без сохранения?", "Да", "Нет");
            if (confirm)
                await Navigation.PopAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class CardEntry : INotifyPropertyChanged
    {
        private string _face, _back, _facePlaceholder, _backPlaceholder;
        public string FacePlaceholder { get => _facePlaceholder; set { _facePlaceholder = value; OnPropertyChanged(); } }
        public string BackPlaceholder { get => _backPlaceholder; set { _backPlaceholder = value; OnPropertyChanged(); } }
        public string Face { get => _face; set { _face = value; OnPropertyChanged(); } }
        public string Back { get => _back; set { _back = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}