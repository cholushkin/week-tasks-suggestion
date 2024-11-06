using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeekTasks
{
    public class TaskType
    {
        public string Description { get; set; }

        // Updated properties to ValueTuple<int, int> with named fields
        [JsonConverter(typeof(ValueTupleConverter))]
        public (int from, int to) WeeklyAmountDays { get; set; }

        [JsonConverter(typeof(ValueTupleConverter))]
        public (int from, int to) DailyAmount { get; set; }

        public double PickUpPriority { get; set; }
        public int SortingIndex { get; set; }
        public string Color { get; set; }

        // Load all task types from a JSON file
        public static Dictionary<string, TaskType> LoadFromJson(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                var taskTypes = JsonSerializer.Deserialize<Dictionary<string, TaskType>>(jsonData, new JsonSerializerOptions
                {
                    Converters = { new ValueTupleConverter() }
                });

                return taskTypes ?? throw new InvalidOperationException();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: The file {filePath} does not exist.");
                Environment.Exit(1);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading task configuration: {ex.Message}");
                Environment.Exit(1);
                return null;
            }
        }

        // Serialize a dictionary of task types to JSON and save to a file
        public static void SaveToJson(string filePath, Dictionary<string, TaskType> taskTypes)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(taskTypes, new JsonSerializerOptions 
                { 
                    WriteIndented = true, 
                    Converters = { new ValueTupleConverter() } 
                });
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving task configuration: {ex.Message}");
            }
        }
    }

    // Custom converter to handle (int, int) ValueTuple in JSON serialization and deserialization
    public class ValueTupleConverter : JsonConverter<(int from, int to)>
    {
        public override (int from, int to) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            reader.Read();
            int from = reader.GetInt32();

            reader.Read();
            int to = reader.GetInt32();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();

            return (from, to);
        }

        public override void Write(Utf8JsonWriter writer, (int from, int to) value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.from);
            writer.WriteNumberValue(value.to);
            writer.WriteEndArray();
        }
    }
}
