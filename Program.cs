using System;
using System.Collections.Generic;
using System.Linq;

namespace CaesarKids
{
    public static class Country
    {
        public static readonly int NUM_INTERNAL_ZONES = 1000;
        public static readonly int NUM_BORDER_ZONES = 100;
        public static readonly int NUM_COUNTRIES = 150;
        public static readonly int NUM_EXTRA_INTERNAL_ZONE_CONNECTIONS = 700;
        public static readonly int NUM_EXTRA_COUNTRY_CONNECTIONS = 150;
        public static readonly double CAESAR_BIRTH_WORLD_POPULATION = 300000000;
        public static readonly double CURRENT_WORLD_POPULATION = 7888000000;
        public static readonly double POP_AVG = CAESAR_BIRTH_WORLD_POPULATION / NUM_COUNTRIES;
        public static void GenerateInternalZones(int zoneIndex)
        {
            var residualPopulation = POP_AVG;
            for (int i = 0; i < NUM_INTERNAL_ZONES; i++)
            {
                var zonesLeft = NUM_INTERNAL_ZONES - i;
                var popPerRemainingZone = residualPopulation / zonesLeft;
                var idx = zoneIndex + i;
                var nextIdx = (idx + 1) % NUM_INTERNAL_ZONES;
                Zone.Zones[idx] = new Zone(popPerRemainingZone);
                Connection.ZoneConnections[Connection.ZoneConnectionCount++] = new Connection(idx, nextIdx, Zone.AVG_INTERNAL_FLOW_RATE);
            }
            for (int i = 0; i < NUM_EXTRA_INTERNAL_ZONE_CONNECTIONS; i++)
            {
                var a = Zone.r.Next() % NUM_INTERNAL_ZONES;
                var b = Zone.r.Next() % (NUM_INTERNAL_ZONES - 1);
                if (b >= a)
                {
                    b++;
                }
                Connection.ZoneConnections[Connection.ZoneConnectionCount++] = new Connection(a, b, Zone.AVG_INTERNAL_FLOW_RATE);
            }
        }
        public static void GenerateExternalConnections(int zoneIndex, IEnumerable<int> connectedZoneIndicies)
        {
            var connected = connectedZoneIndicies.ToArray();
            var i = 0;
            for (; i < connected.Length; i++)
            {
                var a = (Zone.r.Next() % NUM_INTERNAL_ZONES) + zoneIndex;
                var b = (Zone.r.Next() % NUM_INTERNAL_ZONES) + connected[i];
                Connection.ZoneConnections[Connection.ZoneConnectionCount++] = new Connection(a, b, Zone.AVG_EXTERNAL_FLOW_RATE);
            }
            for (; i < NUM_BORDER_ZONES; i++)
            {
                var c = (Zone.r.Next() % connected.Length);
                var a = (Zone.r.Next() % NUM_INTERNAL_ZONES) + zoneIndex;
                var b = (Zone.r.Next() % NUM_INTERNAL_ZONES) + connected[c];
                Connection.ZoneConnections[Connection.ZoneConnectionCount++] = new Connection(a, b, Zone.AVG_EXTERNAL_FLOW_RATE);
            }
        }
    }
    public struct Zone
    {
        public static readonly double AVG_INTERNAL_FLOW_RATE = 100.0 / 3000.0;
        public static readonly double AVG_EXTERNAL_FLOW_RATE = 100.0 / 300000.0;
        public static Random r = new Random();
        public static Zone[] Zones;
        public double[] descendants;
        public double[] population;
        public Zone(double avgPopulation)
        {
            descendants = new double[] { 0, 0, 0, 0 };
            var residualPopulation = (r.NextDouble() + .5) * avgPopulation;
            population = new double[] { 0, 0, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                population[i] = (r.NextDouble() + .5) * (residualPopulation / (4.0 - i));
                residualPopulation -= population[i];
            }
        }
        public void Step(int generationIndex)
        {
            var breedingGeneration = (generationIndex) % 4;
            var breedingDescendants = descendants[breedingGeneration];
            if (breedingDescendants == 0)
            {
                return;
            }
            if (breedingDescendants < 1.0)
            {
                if (r.NextDouble() > breedingDescendants)
                {
                    return;
                }
                breedingDescendants = 1.0;
            }
            var turnoverGeneration = (generationIndex + 1) % 4;
            var numBreedingPairs = population[breedingGeneration] / 2.0;
            var numChildren = population[turnoverGeneration];
            var fecundity = numChildren / numBreedingPairs;
            var descendantPartnerRate = (breedingDescendants - 1) / (population[breedingGeneration] - 1) / 2.0;
            var descendantPairs = breedingDescendants * (1.0 - descendantPartnerRate);
            descendants[turnoverGeneration] = descendantPairs * fecundity;
        }
    }
    public struct Connection
    {
        public static Connection[] ZoneConnections;
        public static int ZoneConnectionCount;
        public int a;
        public int b;
        public double flowRate;
        public Connection(int a, int b, double avgFlowRate)
        {
            this.a = a;
            this.b = b;
            flowRate = (Zone.r.NextDouble() + .5) * avgFlowRate;
        }
        public void Step()
        {
            for (int i = 0; i < 4; i++)
            {
                var aRate = Zone.Zones[a].descendants[i] / Zone.Zones[a].population[i];
                var bRate = Zone.Zones[b].descendants[i] / Zone.Zones[b].population[i];
                var aExport = aRate * flowRate;
                var bExport = bRate * flowRate;
                Zone.Zones[a].descendants[i] -= aExport;
                Zone.Zones[b].descendants[i] += aExport;
                Zone.Zones[a].descendants[i] += bExport;
                Zone.Zones[b].descendants[i] -= bExport;
            }
        }
    }
    static class Program
    {
        public static readonly double GENERATION_TIME = 26.9;
        public static void Initialize()
        {
            Zone.Zones = Zone.Zones ?? new Zone[Country.NUM_COUNTRIES * Country.NUM_INTERNAL_ZONES];
            Connection.ZoneConnections = Connection.ZoneConnections ?? new Connection[Zone.Zones.Length * 4];
            Connection.ZoneConnectionCount = 0;
            for (int i = 0; i < Country.NUM_COUNTRIES; i++)
            {
                Country.GenerateInternalZones(i * Country.NUM_INTERNAL_ZONES);
            }
            Dictionary<int, List<int>> CountryBorderGraph = new Dictionary<int, List<int>>();
            for (int i = 0; i < Country.NUM_COUNTRIES; i++)
            {
                CountryBorderGraph[i] = new List<int>() { (i + 1) % Country.NUM_COUNTRIES };
            }
            for (int i = 0; i < Country.NUM_EXTRA_COUNTRY_CONNECTIONS; i++)
            {
                var a = Zone.r.Next() % Country.NUM_COUNTRIES;
                var b = Zone.r.Next() % (Country.NUM_COUNTRIES - 1);
                if (b >= a)
                {
                    b++;
                }
                CountryBorderGraph[a].Add(b);
            }
            for (int i = 0; i < Country.NUM_COUNTRIES; i++)
            {
                Country.GenerateExternalConnections(i * Country.NUM_INTERNAL_ZONES, CountryBorderGraph[i].Select(x => x * Country.NUM_INTERNAL_ZONES));
            }
            var caesarBirthZone = Zone.r.Next() % Zone.Zones.Length;
            Zone.Zones[caesarBirthZone].descendants[0] = 1;
        }
        public static void SimulationStep(int generation)
        {
            for (int i = 0; i < Connection.ZoneConnectionCount; i++)
            {
                Connection.ZoneConnections[i].Step();
            }
            for (int i = 0; i < Zone.Zones.Length; i++)
            {
                Zone.Zones[i].Step(generation);
            }
        }
        public static double DescendantProportion()
        {
            var simPopulation = Zone.Zones.Select(x => x.population.Sum()).Sum();
            var descendantPopulation = Zone.Zones.Select(x => x.descendants.Sum()).Sum();
            return descendantPopulation / simPopulation;
        }
        public static bool AllZones()
        {
            return Enumerable.Range(0, Zone.Zones.Length).All(x => Zone.Zones[x].descendants.Any(z => z > 1));
        }
        public static bool AllCountries()
        {
            var occupiedZones = Enumerable.Range(0, Zone.Zones.Length).Where(x => Zone.Zones[x].descendants.Any(z => z > 1));
            var occupiedCountries = occupiedZones.Select(x => x / 1000).Distinct();
            return occupiedCountries.Count() == Country.NUM_COUNTRIES;
        }
        public static bool AllPopulation()
        {
            return !Zone.Zones.Any(x => x.descendants.Sum() < x.population.Sum() - 1);
        }
        public static void SimulateSingle()
        {
            Initialize();
            var yearsSinceCeasarBirth = DateTime.Now.Year - (-100);
            var simGenerations = yearsSinceCeasarBirth / GENERATION_TIME + 14;
            int generation = 0;
            var yearAllCountries = -100;
            var yearAllZones = -100;
            var proportionPresentDay = 1.0;
            var yearAll = -100;
            for (; yearAll == -100; generation++)
            {
                SimulationStep(generation);
                if (yearAllZones == -100 && AllZones())
                {
                    yearAllZones = (int)(generation * GENERATION_TIME) - 100;
                }
                if (yearAllCountries == -100 && AllCountries())
                {
                    yearAllCountries = (int)(generation * GENERATION_TIME) - 100;
                }
                if (generation == (int)simGenerations)
                {
                    proportionPresentDay = DescendantProportion();
                }
                if (AllPopulation())
                {
                    yearAll = (int)(generation * GENERATION_TIME) - 100;
                }
            }
            Console.WriteLine(string.Format("In {0}, Caesar's descendants were present in every country.", yearAllCountries));
            Console.WriteLine(string.Format("In {0}, Caesar's descendants were present in every zone.", yearAllZones));
            Console.WriteLine(string.Format("In the present day, {0:p5} of people are descendants of Caesar.", proportionPresentDay));
            Console.WriteLine(string.Format("In {0}, every person will be descendant from Caesar", yearAll));
        }
        public static void SimulateMany(int count)
        {
            Console.WriteLine("Performing bulk simulation");
            var successes = 0;
            var yearsSinceCeasarBirth = DateTime.Now.Year - (-100);
            var simGenerations = yearsSinceCeasarBirth / GENERATION_TIME + 14;
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            for (int sId = 0; sId < count; sId++)
            {
                Initialize();
                for (int generation = 0; generation < simGenerations; generation++)
                {
                    SimulationStep(generation);
                    if (AllPopulation())
                    {
                        successes++;
                        break;
                    }
                }
                var completion = (sId + 1.0) / count;
                var left = stopwatch.Elapsed / completion;
                var eta = DateTime.Now - stopwatch.Elapsed + left;
                Console.WriteLine(string.Format("{0:P} complete. {1:P} so far.  ETA: {2}", completion, successes * 1.0 / (sId + 1), eta));
            }
            Console.WriteLine("After running {2} simulations, everyone alive was a descendant of Caesar in {0}, or {1:p2} of them.", successes, successes * 1.0 / count, count);
        }
        public static void TestVilliage()
        {
            Zone.Zones = new Zone[]
            {
                new Zone(100.0)
            };
            Zone.Zones[0].descendants[0] = 1.0;
            for (int i = 0; i < 10; i++)
            {
                SimulationStep(i);
            }
        }
        public static void TestValley()
        {
            Zone.Zones = new Zone[]
            {
                new Zone(100.0),
                new Zone(100.0),
                new Zone(100.0),
                new Zone(100.0)
            };
            Zone.Zones[0].descendants[0] = 1.0;
            Connection.ZoneConnections = new Connection[]
            {
                new Connection(0, 1, 3),
                new Connection(0, 2, 3),
                new Connection(0, 3, 3),
                new Connection(1, 2, 3),
                new Connection(1, 3, 3),
                new Connection(2, 3, 3),
            };
            Connection.ZoneConnectionCount = Connection.ZoneConnections.Length;
            for (int i = 0; i < 10; i++)
            {
                SimulationStep(i);
            }
        }
        public static void TestMountain()
        {
            Zone.Zones = new Zone[]
            {
                new Zone(100.0),
                new Zone(100.0),
                new Zone(100.0),
                new Zone(100.0)
            };
            Zone.Zones[0].descendants[0] = 1.0;
            Connection.ZoneConnections = new Connection[]
            {
                new Connection(0, 1, 3),
                new Connection(0, 2, 0.5),
                new Connection(1, 2, 0.5),
                new Connection(2, 3, 1),
            };
            Connection.ZoneConnectionCount = Connection.ZoneConnections.Length;
            for (int i = 0; i < 10; i++)
            {
                SimulationStep(i);
            }
        }
        static void Main(string[] args)
        {
            //TestVilliage();
            //TestValley();
            //TestMountain();
            //SimulateSingle();
            SimulateMany(500);
        }
    }
}
