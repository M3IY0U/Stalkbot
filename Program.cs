namespace StalkBot
{
    internal class Program
    {
        public static void Main()
        {
            using (var bot = new Bot())
            {
                bot.RunAsync().Wait();
            }
        }
    }
}