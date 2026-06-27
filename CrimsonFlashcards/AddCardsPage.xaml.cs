using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CrimsonFlashcards
{
    public partial class AddCardsPage : ContentPage
    {
        private readonly Deck _deck;
        private readonly Action<Deck> _onSaved;
        private readonly bool _isEditing;
        private ObservableCollection<CardEntry> _cards;

        public string DeckTitle { get; }

        public ObservableCollection<CardEntry> Cards
        {
            get => _cards;
            set { _cards = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand SaveCommand { get; }

        public AddCardsPage(Deck deck, Action<Deck> onSaved = null, bool isEditing = false)
        {
            InitializeComponent();
            _deck = deck;
            _onSaved = onSaved;
            _isEditing = isEditing;
            DeckTitle = (isEditing ? "Редактирование колоды: " : "Новая колода: ") + deck.Theme;

            AddCommand = new Command(OnAddCard);
            RemoveCommand = new Command<CardEntry>(OnRemoveCard);
            SaveCommand = new Command(OnSave);

            Cards = new ObservableCollection<CardEntry>();

            if (isEditing)
            {
                foreach (var kv in deck.GetCards())
                {
                    Cards.Add(new CardEntry
                    {
                        FacePlaceholder = deck.FaceLang,
                        BackPlaceholder = deck.BackLang,
                        Face = kv.Key,
                        Back = kv.Value
                    });
                }
            }

            BindingContext = this;
        }

        private CardEntry CreateEmptyCard() =>
            new CardEntry
            {
                FacePlaceholder = _deck.FaceLang,
                BackPlaceholder = _deck.BackLang,
                Face = string.Empty,
                Back = string.Empty
            };

        private void OnAddCard() => Cards.Add(CreateEmptyCard());

        private void OnRemoveCard(CardEntry parameter)
        {
            if (Cards.Count > 1) Cards.Remove(parameter);
        }

        private async void OnSave()
        {
            if (Cards.Count == 0)
            {
                await DisplayAlert("Ошибка", "Добавьте хотя бы одну карточку.", "OK");
                return;
            }

            foreach (var card in Cards)
            {
                if (string.IsNullOrWhiteSpace(card.Face) || string.IsNullOrWhiteSpace(card.Back))
                {
                    await DisplayAlert("Ошибка", "Все поля должны быть заполнены.", "OK");
                    return;
                }
            }

            if (_isEditing)
                _deck.ClearCards();

            foreach (var card in Cards)
                _deck.AddCard(card.Face, card.Back);

            _onSaved?.Invoke(_deck);
            await Navigation.PopAsync();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}