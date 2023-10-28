// See https://aka.ms/new-console-template for more information

using DatabaseService;
using NLipsum.Core;

var service = new Service();

var text = Lipsums.LoremIpsum.ToLower().Split(' ').Take(150).ToList();
var startTime = DateTime.Now;

// await service.InsertWors(text);

string[] words = { "модуль", "пакет", "python" };

var list = await service.GetWordsByStrings(words);
foreach (var word in list)
{
    Console.WriteLine(word.word);
}


var ts = DateTime.Now.Subtract(startTime);
string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
    ts.Hours, ts.Minutes, ts.Seconds,
    ts.Milliseconds / 10);
Console.WriteLine(elapsedTime, "RunTime");