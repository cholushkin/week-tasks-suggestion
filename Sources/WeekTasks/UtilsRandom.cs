namespace WeekTasks.Utils.Random
{
    public interface IPseudoRandomNumberGeneratorState
    {
        string Save();
        void Load(string state);
        IPseudoRandomNumberGenerator Create();
        long AsNumber();
    }

    public interface IPseudoRandomNumberGenerator
    {
        void SetState(IPseudoRandomNumberGeneratorState state);
        IPseudoRandomNumberGeneratorState GetState();
        double Next(); //  [0.0, 1.0)
    }
    
    public class LinearConRng : IPseudoRandomNumberGenerator
    {
        public struct State : IPseudoRandomNumberGeneratorState
        {
            public State(long seed)
            {
                _seed = seed;
            }

            public string Save()
            {
                throw new NotImplementedException();
            }

            public void Load(string state)
            {
                throw new NotImplementedException();
            }

            public IPseudoRandomNumberGenerator Create()
            {
                return new LinearConRng(this);
            }

            public long AsNumber()
            {
                return _seed;
            }

            internal long _seed;
        }

        private const long a = 25214903917;
        private const long c = 11;
        
        private State _state;

        public LinearConRng(long seed)
        {
            if (seed < 0)
                throw new Exception($"Bad seed {seed}");
            _state = new State(seed);
        }

        public LinearConRng(State state)
        {
            _state = state;
        }

        private int next(int bits) // helper
        {
            _state._seed = (_state._seed * a + c) & ((1L << 48) - 1);
            return (int)(_state._seed >> (48 - bits));
        }

        public void SetState(IPseudoRandomNumberGeneratorState state)
        {
            _state = (State)state;
        }

        public IPseudoRandomNumberGeneratorState GetState()
        {
            return _state;
        }

        public double Next()
        {
            return (((long)next(26) << 27) + next(27)) / (double)(1L << 53);
        }
    }


    public static class RandomHelper
    {
        public delegate void SimpleFunction();

        private static int _nextRndSeedPointer = -1;
        private const int MaxSeed = 1000000;

        public static readonly IPseudoRandomNumberGenerator Rnd = CreateRandomNumberGenerator();

        public static IPseudoRandomNumberGenerator CreateRandomNumberGenerator(long seed = -1)
        {
            if (seed == -1)
                seed = RandomSeed();

            if (seed < 0)
            {
                Console.WriteLine($"Your seed should be above zero {seed} : changed to positive");
                seed = Math.Abs(seed);
            }

            if (seed > MaxSeed)
            {
                seed = seed % MaxSeed;
            }

            return new LinearConRng(seed);
        }

        public static IPseudoRandomNumberGenerator CreateRandomNumberGenerator(
            IPseudoRandomNumberGeneratorState state)
        {
            if (state == null)
                return CreateRandomNumberGenerator();
            return state.Create();
        }

        #region values

        public static int ValueInt(this IPseudoRandomNumberGenerator rng)
        {
            return (int)(rng.Next() * Int32.MaxValue);
        }

        public static int ValueInt(this IPseudoRandomNumberGenerator rng, int max)
        {
            if (max == 0)
                return 0;
            return ValueInt(rng) % max;
        }

        public static float ValueFloat(this IPseudoRandomNumberGenerator rng)
        {
            return (float)rng.Next();
        }

        public static double ValueDouble(this IPseudoRandomNumberGenerator rng)
        {
            return rng.Next();
        }

        #endregion

        #region ranges

        public static float
            Range(this IPseudoRandomNumberGenerator rng, float min, float max) // min[inclusive] and max[inclusive]
        {
            return (float)((max - min) * rng.ValueDouble() + min);
        }

        public static int
            Range(this IPseudoRandomNumberGenerator rng, int min, int max) // min[inclusive] and max[exclusive]
        {
            return rng.ValueInt(max - min) + min;
        }

        public static float FromRange(this IPseudoRandomNumberGenerator rng, (float from, float to) range) // ()
        {
            return rng.Range(range.from, range.to);
        }

        public static int FromRangeInt(this IPseudoRandomNumberGenerator rng, (float from, float to) range) // [)
        {
            return rng.Range((int)range.from, (int)range.to);
        }

        public static int FromRangeIntInclusive(this IPseudoRandomNumberGenerator rng, (float from, float to) range) // []
        {
            return rng.Range((int)range.from, (int)(range.to + 1));
        }

        public static int FromRangeIntInclusive(this IPseudoRandomNumberGenerator rng, int from, int to) // []
        {
            return rng.Range(from, to + 1);
        }

        #endregion

        #region containers

        public static T FromArray<T>(this IPseudoRandomNumberGenerator rng, T[] arr)
        {
            return arr[rng.Range(0, arr.Length)];
        }

        public static T[]
            FromArray<T>(this IPseudoRandomNumberGenerator rng, T[] arr, int amount) // get amount values from array
        {
            var src = arr.ToList();
            var res = new T[amount];
            for (int i = 0; i < amount; ++i)
            {
                res[i] = rng.FromList(src);
                src.Remove(res[i]);
            }

            return res;
        }

        public static T FromList<T>(this IPseudoRandomNumberGenerator rng, List<T> lst)
        {
            return lst[rng.Range(0, lst.Count)];
        }

        public static List<T> FromList<T>(this IPseudoRandomNumberGenerator rng, List<T> lst, int amount)
        {
            var src = new List<T>(lst);
            var res = new List<T>(amount);
            for (int i = 0; i < amount; ++i)
            {
                T tmp = rng.FromList(src);
                res.Add(tmp);
                src.Remove(tmp);
            }

            return res;
        }

        public static KeyValuePair<T, T2> FromDictionary<T, T2>(this IPseudoRandomNumberGenerator rng,
            Dictionary<T, T2> dic)
        {
            return dic.ElementAt(rng.Range(0, dic.Count));
        }

        public static T FromEnumerable<T>(this IPseudoRandomNumberGenerator rng, IEnumerable<T> enumerable)
        {
            int index = rng.Range(0, enumerable.Count());
            return enumerable.ElementAt(index);
        }

        public static T[] Shuffle<T>(this IPseudoRandomNumberGenerator rng, T[] array)
        {
            T[] shuffledArray = new T[array.Length];
            Array.Copy(array, shuffledArray, array.Length);
            for (int i = shuffledArray.Length - 1; i > 0; i--)
            {
                int rndIndex = rng.Range(0, i);
                T temp = shuffledArray[i];
                shuffledArray[i] = shuffledArray[rndIndex];
                shuffledArray[rndIndex] = temp;
            }

            return shuffledArray;
        }

        public static List<T> Shuffle<T>(this IPseudoRandomNumberGenerator rng, List<T> list)
        {
            List<T> shuffledList = new List<T>(list.Count);

            foreach (var item in list)
                shuffledList.Add(item);

            for (int i = shuffledList.Count - 1; i > 0; i--)
            {
                int rndIndex = rng.Range(0, i);
                T temp = shuffledList[i];
                shuffledList[i] = shuffledList[rndIndex];
                shuffledList[rndIndex] = temp;
            }

            return shuffledList;
        }

        public static void ShuffleInplace<T>(this IPseudoRandomNumberGenerator rng, T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int rndIndex = rng.Range(0, i);
                T temp = array[i];
                array[i] = array[rndIndex];
                array[rndIndex] = temp;
            }
        }

        public static void ShuffleInplace<T>(this IPseudoRandomNumberGenerator rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int rndIndex = rng.Range(0, i);
                T temp = list[i];
                list[i] = list[rndIndex];
                list[rndIndex] = temp;
            }
        }

        #endregion

        #region probabilities

        public static bool TrySpawnEvent(this IPseudoRandomNumberGenerator rng, float probability,
            SimpleFunction eventFunc = null)
        {
            if (rng.ValueFloat() <= probability)
            {
                eventFunc?.Invoke();
                return true;
            }

            return false;
        }

        public static int SpawnEvent(this IPseudoRandomNumberGenerator rng, float[] probs)
        {
            // get prob line
            float sum = 0;
            for (int i = 0; i < probs.Length; ++i)
                sum += probs[i];

            // select val
            float point = rng.ValueFloat() * sum;

            // return event
            for (int i = 0; i < probs.Length; ++i)
                if ((point -= probs[i]) < 0)
                    return i;

            return -1;
        }
        // Example:
        // int selectedEventIndex;
        // EventOption selectedEvent = rng.SpawnEvent(options, option => option.Probability, out selectedEventIndex);
        // EventOption another = rng.SpawnEvent(options, option => option.Probability, out _);
        public static T SpawnEvent<T>(this IPseudoRandomNumberGenerator rng, IEnumerable<T> items, Func<T, float> probabilitySelector, out int index)
        {
            // Calculate total probability sum
            float totalProbability = items.Sum(probabilitySelector);

            if (totalProbability <= 0)
            {
                index = -1; // Optional index in case of failure
                return default;  // Return the default value for type T (null for reference types, zero for value types)
            }

            // Generate a random point in the range [0, totalProbability)
            float randomValue = rng.ValueFloat() * totalProbability;

            // Iterate through items and select based on the cumulative probability
            index = 0; // Initialize index
            foreach (var item in items)
            {
                randomValue -= probabilitySelector(item);
                if (randomValue < 0)
                    return item; // Return the selected item
                index++;
            }

            index = -1; // In case of rounding issues or unexpected input
            return default; // Return the default value for type T
        }

        public static bool YesNo(this IPseudoRandomNumberGenerator rng, SimpleFunction function = null)
        {
            if (rng.ValueFloat() < 0.5f)
            {
                if (function != null)
                    function();
                return true;
            }

            return false;
        }

        #endregion

        #region enums

        public static int FromEnum(this IPseudoRandomNumberGenerator rng, Type enumType)
        {
            Array arr = Enum.GetValues(enumType);
            return (int)arr.GetValue(rng.Range(0, arr.Length));
        }

        public static T FromEnum<T>(this IPseudoRandomNumberGenerator rng)
        {
            Array arr = Enum.GetValues(typeof(T));
            return (T)arr.GetValue(rng.Range(0, arr.Length));
        }

        #endregion


        private static int _getPseudoRand(int val)
        {
            return (((val * 1103515245) + 12345) & 0x7fffffff) % MaxSeed;
        }

        private static long RandomSeed()
        {
            if (_nextRndSeedPointer == -1)
                _nextRndSeedPointer = (int)(DateTime.Now.Ticks % MaxSeed);
            else
                _nextRndSeedPointer = _getPseudoRand(_nextRndSeedPointer);
            return _nextRndSeedPointer;
        }
    }
}