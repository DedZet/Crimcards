using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrimsonFlashcards
{
    public class Deck
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Theme { get; set; }
        public string Description { get; set; }
        public string FaceLang { get; set; }
        public string BackLang { get; set; }

        private List<Card> cards = new List<Card>();

        public Deck(string theme, string description, string faceLang, string backLang)
        {
            this.Theme = theme;
            this.Description = description;
            this.FaceLang = faceLang;
            this.BackLang = backLang;
        }

        public Deck() { }

        private class CardStat
        {
            public string Face { get; set; }
            public int Interval { get; set; }
            public int Repetition { get; set; }
            public double Easiness { get; set; }
            public DateTime NextReview { get; set; }
        }

        public void SaveStatistics(string folderPath)
        {
            var stats = cards.Select(c => new CardStat
            {
                Face = c.Face,
                Interval = c.Interval,
                Repetition = c.Repetition,
                Easiness = c.Easiness,
                NextReview = c.NextReview
            }).ToList();

            string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(folderPath, $"{Id}.stats.json");
            File.WriteAllText(filePath, json);
        }

        public void DeleteFiles(string folderPath)
        {
            string csvPath = Path.Combine(folderPath, $"{this.Id}.csv");
            string statsPath = Path.Combine(folderPath, $"{this.Id}.stats.json");

            if (File.Exists(csvPath)) File.Delete(csvPath);
            else Debug.WriteLine("CSV does not exist");
            if (File.Exists(statsPath)) File.Delete(statsPath);
            else Debug.WriteLine("JSON does not exist");
        }

        public void LoadStatistics(string folderPath)
        {
            string filePath = Path.Combine(folderPath, $"{Id}.stats.json");
            if (!File.Exists(filePath)) return;

            string json = File.ReadAllText(filePath);
            var stats = JsonSerializer.Deserialize<List<CardStat>>(json);
            if (stats == null) return;

            foreach (var stat in stats)
            {
                var card = cards.FirstOrDefault(c => c.Face == stat.Face);
                if (card != null)
                {
                    card.Interval = stat.Interval;
                    card.Repetition = stat.Repetition;
                    card.Easiness = stat.Easiness;
                    card.NextReview = stat.NextReview;
                }
            }
        }

        public void DeleteStatistics(string folderPath)
        {
            string filePath = Path.Combine(folderPath, $"{this.Id}.stats.json");
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        public IReadOnlyList<Card> Cards => cards.AsReadOnly();

        public void ImportCSV(FileStream csv)
        {
            if (csv == null)
                throw new ArgumentNullException(nameof(csv));

            using (var reader = new StreamReader(csv, Encoding.UTF8, true, -1, true))
            {
                string wordsJson = null;
                string headerLine = reader.ReadLine();
                if (headerLine == null)
                    throw new InvalidDataException("CSV file is empty.");

                string dataLine = reader.ReadLine();
                if (dataLine == null)
                    throw new InvalidDataException("CSV file does not contain a data row.");

                string[] headers = ParseCsvLine(headerLine);
                string[] fields = ParseCsvLine(dataLine);

                if (headers.Length == 6 && fields.Length == 6)
                {
                    if (Guid.TryParse(fields[0], out Guid id))
                        this.Id = id;
                    else
                        this.Id = Guid.NewGuid();

                    this.Theme = fields[1];
                    this.Description = fields[2];
                    this.FaceLang = fields[3];
                    this.BackLang = fields[4];
                    wordsJson = fields[5];
                }
                else
                {
                    throw new InvalidDataException($"CSV must contain either 6 columns. Found {headers.Length} columns.");
                }

                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(wordsJson)
                           ?? new Dictionary<string, string>();

                cards.Clear();
                foreach (var kvp in dict)
                {
                    cards.Add(new Card { Face = kvp.Key, Back = kvp.Value });
                }
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(field.ToString());
                        field.Clear();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
            }

            fields.Add(field.ToString());
            return fields.ToArray();
        }

        public bool SaveToFile(string folderPath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                string fileName = $"{Id}.csv";
                string filePath = Path.Combine(folderPath, fileName);

                var dict = new Dictionary<string, string>();
                foreach (var card in cards)
                {
                    dict[card.Face] = card.Back;
                }

                string wordsJson = JsonSerializer.Serialize(dict);

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("\"Id\",\"theme\",\"description\",\"faceLang\",\"backLang\",\"words\"");
                    string Escape(string s) => "\"" + s?.Replace("\"", "\"\"") + "\"";
                    string line = string.Join(",",
                        Escape(Id.ToString()),
                        Escape(Theme),
                        Escape(Description),
                        Escape(FaceLang),
                        Escape(BackLang),
                        Escape(wordsJson)
                    );
                    writer.WriteLine(line);
                }
                SaveStatistics(folderPath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static Deck LoadFromFile(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var deck = new Deck();
            deck.ImportCSV(fs);
            string folder = Path.GetDirectoryName(filePath);
            deck.LoadStatistics(folder);
            return deck;
        }

        public void AddCard(string faceWord, string backWord)
        {
            cards.Add(new Card { Face = faceWord, Back = backWord });
        }

        public void insertCard(int pos, string faceWord, string backWord)
        {
            cards.Insert(pos, new Card { Face = faceWord, Back = backWord });
        }

        public void remCard(int pos)
        {
            cards.RemoveAt(pos);
        }

        public int getLength() => cards.Count;

        public List<KeyValuePair<string, string>> GetCards()
        {
            return cards.Select(c => new KeyValuePair<string, string>(c.Face, c.Back)).ToList();
        }

        public void ClearCards() => cards.Clear();

        public void UpdateCard(Card card, int quality)
        {
            if (quality >= 3)
            {
                if (card.Repetition == 0)
                    card.Interval = 1;
                else if (card.Repetition == 1)
                    card.Interval = 6;
                else
                    card.Interval = (int)Math.Round(card.Interval * card.Easiness);

                card.Repetition++;
            }
            else
            {
                card.Repetition = 0;
                card.Interval = 1;
            }

            card.Easiness = card.Easiness + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
            if (card.Easiness < 1.3) card.Easiness = 1.3;

            card.NextReview = DateTime.Now.AddDays(card.Interval);
        }

        public List<Card> GetCardsForReview(DateTime date)
        {
            return cards.Where(c => c.NextReview <= date).ToList();
        }

        public (int TotalCards, int LearnedCards, int DueCards, double AverageEasiness) GetStatistics()
        {
            int total = cards.Count;
            int learned = cards.Count(c => c.Repetition > 0);
            int due = cards.Count(c => c.NextReview <= DateTime.Now);
            double avgEasiness = cards.Any() ? cards.Average(c => c.Easiness) : 0;
            return (total, learned, due, avgEasiness);
        }
    }
}