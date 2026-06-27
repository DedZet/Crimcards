using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;

namespace CrimsonFlashcards
{
    public partial class TestPage : ContentPage
    {
        private Deck _deck;
        private bool _isBackSideFirst;
        private List<Card> _reviewQueue;
        private int _currentIndex = 0;
        private bool _isFlipped = false;

        public TestPage(Deck deck, bool isBackSideFirst)
        {
            InitializeComponent();
            _deck = deck;
            _isBackSideFirst = isBackSideFirst;
            LoadCards();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnCardTapped;
            CardBorder.GestureRecognizers.Add(tapGesture);
        }

        private void LoadCards()
        {
            _reviewQueue = _deck.Cards.ToList();
            if (_reviewQueue.Count == 0)
            {
                DisplayAlert("Ошибка", "В колоде нет карточек.", "OK");
                Navigation.PopAsync();
                return;
            }
            _reviewQueue = _reviewQueue.OrderBy(x => Guid.NewGuid()).ToList();
            _currentIndex = 0;
            ShowCurrentCard();
        }

        private void ShowCurrentCard()
        {
            if (_currentIndex >= _reviewQueue.Count)
            {
                DisplayAlert("Завершено", "Вы успешно завершили сессию повторения!", "OK");
                Navigation.PopAsync();
                return;
            }
            var card = _reviewQueue[_currentIndex];
            FrontLabel.Text = card.Face;
            BackLabel.Text = card.Back;
            _isFlipped = _isBackSideFirst;
            UpdateCardVisibility();

            CardGrid.Scale = 1;
            CardGrid.TranslationX = 0;
            CardGrid.Opacity = 1;
        }

        private void UpdateCardVisibility()
        {
            FrontView.IsVisible = !_isFlipped;
            BackView.IsVisible = _isFlipped;
        }

        private async void OnCardTapped(object sender, EventArgs e)
        {
            await FlipCard();
        }

        private async Task FlipCard()
        {
            await CardGrid.ScaleTo(0, 200, Easing.CubicIn);
            _isFlipped = !_isFlipped;
            UpdateCardVisibility();
            await CardGrid.ScaleTo(1, 200, Easing.CubicOut);
        }

        private async void OnKnowClicked(object sender, EventArgs e)
        {
            var card = _reviewQueue[_currentIndex];
            _deck.UpdateCard(card, 5);
            await AnimateCardExit(true);
            _currentIndex++;
            ShowCurrentCard();
        }

        private async void OnDontKnowClicked(object sender, EventArgs e)
        {
            var card = _reviewQueue[_currentIndex];
            _deck.UpdateCard(card, 0);
            await AnimateCardExit(false);
            _currentIndex++;
            ShowCurrentCard();
        }

        private async Task AnimateCardExit(bool toRight)
        {
            double x = toRight ? 300 : -300;
            await CardGrid.TranslateTo(x, 0, 300, Easing.CubicIn);
            await CardGrid.FadeTo(0, 200);
            CardGrid.TranslationX = 0;
            CardGrid.Opacity = 1;
        }
    }
}