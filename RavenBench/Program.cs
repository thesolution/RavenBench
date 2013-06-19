using Raven.Client;
using Raven.Client.Document;
using RavenBench.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RavenBench
{
    class Program
    {
        private static IDocumentStore store;
        private static ConcurrentQueue<string> errors = new ConcurrentQueue<string>();

        static void Main(string[] args)
        {
          
            var userCount = Int32.Parse(ConfigurationManager.AppSettings["userCount"]);
            var readWriteCount = Int32.Parse(ConfigurationManager.AppSettings["readWriteCount"]);

            Time(sw => Try(InitializeDb, "initialize db"), "initialize db");

            WriteUsers(userCount, CreateUser);

            var tasks = new Task[] { 
                Task.Run(() => Time(sw => RandomRead(sw, readWriteCount, userCount), "random reads")),
                Task.Run(() => Time(sw => RandomWrite(sw, readWriteCount, userCount), "random writes"))
            };

            //Task.WaitAll(tasks);
            Console.ReadKey();
            
            File.WriteAllLines("Errors_" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString() + ".txt", errors.ToArray());

            Console.WriteLine();
            Console.WriteLine("done.");
            Console.ReadKey(true);
        }

        static void InitializeDb()
        {
            store = new DocumentStore { ConnectionStringName = "RavenDB" };
            store.Initialize();
        }

        static void RandomWrite(Stopwatch sw, int readWriteCount, int userCount)
        {
            int complete = 0, errors = 0;

            InParallel(i => {
                var random = new Random();
                var next = random.Next(1, userCount);

                using (var session = store.OpenSession())
                {
                    User user = null;

                    var loaded = Try(() => { 
                        user = session.Load<User>("users-" + next.ToString());
                    }, "read");

                    bool written = false;
                    if (loaded && user != null)
                    {
                        user.SessionId = new string(random.Next(1, 9).ToString().First(), 10);

                        written = Try(() =>
                        {
                            session.Store(user);
                            session.SaveChanges();
                        }, "write");
                    }

                    if (loaded && written) Interlocked.Increment(ref complete);
                    if (!loaded || !written) Interlocked.Increment(ref errors);

                    if (complete % 1000 == 0)
                    {
                        PrintStatus(sw, complete * 2 /* read & write */, errors, 5);
                    }
                }
            }, 
            readWriteCount);
        }

        static void RandomRead(Stopwatch sw, int readWriteCount, int userCount)
        {
            int complete = 0, errors = 0;

            InParallel(i =>
            {
                var random = new Random();
                var next = random.Next(1, userCount);
                var read = Try(() => Read<User>("users-" + next.ToString()), "read");
                if (read) Interlocked.Increment(ref complete);
                if (!read) Interlocked.Increment(ref errors);

                if (complete % 1000 == 0)
                {
                    PrintStatus(sw, complete, errors, 6);
                }
            },
            readWriteCount);            
        }


        static User CreateUser(int i)
        {
            var user = new User
            {
                Id = "users-" + i.ToString(),
                Name = i.ToString(),
                Marketboards = new List<Marketboard>() {
                    new Marketboard {
                        ColumnCount = 5,
                        RowCount = 5,
                        Symbols = Enumerable.Range(1, 25).Select(n => new MarketboardSymbol { Symbol = new string(n.ToString().First(), 7) }).ToList()
                    }
                }
            };

            return user;
        }

        static T Read<T>(string id)
        {
            T entity = default(T);

            using (var session = store.OpenSession())
            {
                entity = session.Load<T>(id);
            }

            return entity;
        }

        static void Write<T>(T entity)
        {
            using (var session = store.OpenSession())
            {
                session.Store(entity);
                session.SaveChanges();
            }
        }

        static void WriteAll<T>(IEnumerable<T> entities, Stopwatch sw)
        {
            int complete = 0, errors = 0;

            foreach (var entity in entities)
            {
                var written = Try(() => Write(entity), "write");
                if (!written) errors++;
                complete++;

                if (complete % 1000 == 0)
                {
                    PrintStatus(sw, complete, 0, 3);
                }
            }
        }

        static void BatchStore<T>(IEnumerable<T> entities, Stopwatch sw)
        {
            int complete = 0;

            using (var bulk = store.BulkInsert())
            {
                foreach (var entity in entities)
                {
                    bulk.Store(entity);

                    complete++;

                    if (complete % 1000 == 0)
                    {
                        PrintStatus(sw, complete, 0, 3);
                    }
                }
            }
        }

        static void WriteUsers(int count, Func<int, User> createNewObject)
        {
            User user = null;
            var read = Try(() => user = Read<User>("users-1"), "test user read");

            if (!read)
            {
                Console.WriteLine("failed to read test user.");
                return;
            }

            if (user != null)
            {
                Console.WriteLine("user documents already exist.");
                return;
            }

            var objects = Enumerable
                .Range(1, count)
                .Select(createNewObject);

            Time(sw => Try(() => WriteAll(objects, sw), "batch insert"), "batch insert");
        }

        static bool Try(Action action, string description)
        {
            var success = false;

            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex, "Exception - " + description);
                errors.Enqueue(ex.ToString());
            }

            return success;
        }

        static void InParallel(Action<int> iterable, int count)
        {
            Parallel.ForEach(Enumerable.Range(1, count), iterable);
        }

        static object locker = new object();

        static void PrintStatus(Stopwatch timer, int complete, int errors, int row)
        {
            var time = Math.Floor(timer.Elapsed.TotalSeconds);
            var rps = Math.Floor(complete / timer.Elapsed.TotalSeconds);

            lock (locker)
            {
                Console.CursorTop = row;
                Console.CursorLeft = 0;
                Console.Write(new String(' ', 75));
                Console.CursorLeft = 0;

                Console.Write(
                    "complete: {0} errors: {1} time: {2} rps: {3}",
                    complete, errors, time, rps
                );
            }
        }

        static void Time(Action<Stopwatch> action, string taskDescription)
        {
            var sw = new Stopwatch();
            Console.Write("starting " + taskDescription + " ... ");

            sw.Start();
            action(sw);
            sw.Stop();

            Console.WriteLine("\ndone. elapsed: {0}\n", sw.ElapsedMilliseconds);
        }
    }


}
