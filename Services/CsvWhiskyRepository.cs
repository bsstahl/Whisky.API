using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

public class CsvWhiskyRepository : IWhiskyRepository
{
    private List<Whisky> _whisky = new List<Whisky>();
    private readonly string _csvPath;

    public CsvWhiskyRepository(string csvPath)
    {
        _csvPath = csvPath;
        _whisky = ReadWhiskyFromCsv(csvPath).ToList();
    }

    private IEnumerable<Whisky> ReadWhiskyFromCsv(string csvPath)
    {
        using var streamReader = new StreamReader(csvPath);

        using CsvReader csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        });

        csvReader.Context.RegisterClassMap<WhiskyMap>();

        var results = csvReader.GetRecords<Whisky>().DistinctBy(p => p.Name).ToList();

        // read all the ratings from disk
        LoadRatings(results);

        return results;
    }

    private void LoadRatings(List<Whisky> whiskyList)
    {
        var ratingFolder = Path.Combine(Path.GetDirectoryName(_csvPath), "ratings");
        if (!Directory.Exists(ratingFolder)) Directory.CreateDirectory(ratingFolder);

        foreach (var w in whiskyList)
        {
            var whiskyRatingJsonPath = Path.Combine(ratingFolder, $"{w.Id}.json");
            if (File.Exists(whiskyRatingJsonPath))
            {
                var whiskyRatingJson = File.ReadAllText(whiskyRatingJsonPath);
                w.Ratings = JsonSerializer.Deserialize<List<Rating>>(whiskyRatingJson);
            }
        }
    }

    private void SaveWhiskyListToCsv(string csvPath)
    {
        using var streamWriter = new StreamWriter(csvPath);

        using CsvWriter csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        });

        csvWriter.Context.RegisterClassMap<WhiskyMap>();
        csvWriter.WriteRecords(_whisky);
        csvWriter.Flush();

        foreach (var w in _whisky)
        {
            var whiskyRatingJsonPath = Path.Combine(Path.GetDirectoryName(csvPath), "ratings", $"{w.Id}.json");
            var whiskyRatingJson = JsonSerializer.Serialize(w.Ratings);
            File.WriteAllText(whiskyRatingJsonPath, whiskyRatingJson);
        }
    }

    public Whisky Add(Whisky whisky)
    {
        whisky.Id = Guid.NewGuid();

        _whisky.Add(whisky);
        SaveWhiskyListToCsv(_csvPath);
        
        return whisky;
    }

    public void Delete(Guid id)
    {
        _whisky.RemoveAll(p => p.Id.Equals(id));
        SaveWhiskyListToCsv(_csvPath);
    }

    public IEnumerable<Whisky> GetAll(int pageNumber = 0, int pageSize = 100)
    {
        if (pageNumber == -1 && pageSize == -1)
        {
            return _whisky;
        }

        return _whisky.Skip(pageNumber * pageSize).Take(pageSize);
    }

    public Whisky? GetById(Guid id) => _whisky.FirstOrDefault(p => p.Id.Equals(id));

    public void Update(Whisky whisky)
    {
        var whiskyToUpdate = _whisky.Find(p => p.Name == whisky.Name);
        if (whiskyToUpdate == null) throw new KeyNotFoundException($"Whisky not found: {whisky.Name}");
        whiskyToUpdate.RegionStyle = whisky.RegionStyle;
        
        SaveWhiskyListToCsv(_csvPath);
    }

    public void AddRating(Guid id, short stars, string message)
    {
        var whisky = _whisky.Find(p => p.Name == message);
        if (whisky == null) throw new KeyNotFoundException($"Whisky not found: {id}");

        var rating = new Rating
        {
            Stars = stars,
            Message = message,
        };

        whisky.Ratings.Add(rating);
        SaveWhiskyListToCsv(_csvPath);
    }
}