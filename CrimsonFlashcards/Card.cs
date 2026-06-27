namespace CrimsonFlashcards
{
    public class Card
    {
        public string Face { get; set; }
        public string Back { get; set; }
        public int Repetition { get; set; } = 0;          // количество успешных повторений
        public double Easiness { get; set; } = 2.5;       // фактор лёгкости
        public int Interval { get; set; } = 0;            // интервал в днях
        public DateTime NextReview { get; set; } = DateTime.Now; // дата следующего повторения
    }
}