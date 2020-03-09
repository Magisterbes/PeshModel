using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PeshApp
{
    class Program
    {
        static void Main(string[] args)
        {
           var mark = Train();
           
           Test(mark);

        }

        static void Test(string filePrefix)
        {
            //Тексты для проверки
            var texts = File.ReadAllText(@"./Data/manytexts.txt", Encoding.GetEncoding(1251)).Split(new char[] { '\n', '.', '?', '!' });
            texts = texts.Select(a => PhraseTransform(a)).Where(a => PhraseFilter(a)).Skip(100000).Take(100).Distinct().ToArray();

            //Загрузка модели и текстов в которых надо искать
            var modelFile = filePrefix + "_model.mod";
            var searchLines = filePrefix + "_txts.txt";

            var mod = new Model(modelFile);
            var clusteredTexts = Helper.ParseTextsContext(searchLines);

            //Проверка кластеров на равномерность
            //var filter = clusteredTexts.Where(a => a.Value.Count > 2000).Select(a=> mod.VectorStructure[a.Key]).ToList();

            var times = new List<double>();
            var forCheck = new List<string>();

            foreach (var testText in texts)
            {


                var d1 = DateTime.Now;

                //Векторизация с весами
                var searchVector = mod.VectorizePhrase(PhraseTransform(testText), true);

                if (searchVector == null)
                {
                    forCheck.Add(testText + ";" + "НЕ НАШЛОСЬ");
                    continue;
                }

                //Ищем в N  индексах с максимальными значениями... 
                var ordered = searchVector.OrderByDescending(x => x).Take(10);
                var maxIndieces = ordered.Select(a => searchVector.ToList().IndexOf(a)).ToList();
                var near = new List<string>();
                double dist = 0;
                var nearest = "";


                //Проверка по косинусной мере
                var normA = Math.Sqrt(searchVector.Select(a => a * a).Sum());

                foreach (var maxIndex in maxIndieces)
                {

                    if (clusteredTexts[maxIndex].Count != 0)
                    {

                        foreach (var k in clusteredTexts[maxIndex].Keys)
                        {
                            var normB = Math.Sqrt(k.Select(a => a * a).Sum());

                            var cos = searchVector.Zip(k, (x, y) => x * y).Sum() / (normA * normB);

                            if (cos > dist)
                            {
                                nearest = clusteredTexts[maxIndex][k];
                                dist = cos;
                            }
                        }
                    }
                }

                var time = (DateTime.Now - d1).TotalMilliseconds;
                times.Add(time);
                forCheck.Add(testText +" -- "+nearest.ToUpper());
            }

            var meanTime = times.Average();

            //Сравнение с перебором
            //double dist2 = 0;
            //var nearest2 = "";
            //foreach (var idx in clusteredTexts.Keys)
            //{
            //    foreach (var k in clusteredTexts[idx].Keys)
            //    {
            //        var normB = Math.Sqrt(k.Select(a => a * a).Sum());

            //        var cos = searchVector.Zip(k, (x, y) => x * y).Sum() / (normA * normB);

            //        if (cos > dist2)
            //        {
            //            nearest2 = clusteredTexts[idx][k];
            //            dist2 = cos;
            //        }
            //    }
            //}

            //var time2 = (DateTime.Now - d1).TotalMilliseconds;

        }



        static string Train()
        {
            var trainData = LoadData();
            Train train = new Train(trainData);
            var mod = train.TrainModel();

            return mod.Mark;
        }


        static bool PhraseFilter(string phrase)
        {
            //Предложения должны быть хотя бы больше N символов и тд.
            if (phrase.Length < 30)
                return false;

            return true;
        }


        static string PhraseTransform(string phrase)
        {
            phrase = phrase.Trim().ToLower();
            phrase = Regex.Replace(phrase, "[^а-я0-9 ]", "");
            return phrase;
        }


        static string[] LoadData()
        {
            var texts = File.ReadAllText(@"./Data/manytexts.txt", Encoding.GetEncoding(1251)).Split(new char[] { '\n' ,'.', '?', '!' });
            
            texts = texts.Select(a => PhraseTransform(a)).Where(a => PhraseFilter(a)).Take(100000).Distinct().ToArray();

            return texts;
        }
    }
}
