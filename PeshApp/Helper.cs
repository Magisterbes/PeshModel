using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PeshApp
{
    static class Helper
    {
        /// <summary>
        /// Грубая обрезка слов до рабочего состояния
        /// </summary>
        /// <param name="word">Слово, которое надо обрезать</param>
        /// <returns></returns>
        public static string TrimWord(string word)
        {
            if (word.Length > 4 && word.Length < 7)
            {
                word = word.Substring(0, 5);

            }
            if (word.Length >= 7 && word.Length < 9)
            {
                word = word.Substring(0, 5);
            }

            if (word.Length >= 9 && word.Length < 11)
            {
                word = word.Substring(0, 6);
            }

            if (word.Length >= 11)
            {
                word = word.Substring(0, 7);
            }

            return word.ToLower();
        }

        /// <summary>
        /// Словарь посчитанных слов в корпусе
        /// </summary>
        /// <param name="data">Массив текста</param>
        /// <returns></returns>
        public static Dictionary<string,int> GetDictionary(string[] data)
        {
            var di = new Dictionary<string, int>();

            foreach (var row in data)
            {
                row.Split(' ').Where(w=> w.Length>3).ToList().ForEach(word => {
                    //грубо обрезаем слова, чтобы окончания не мешали. Можно не грубо, но сложно.                    
                    word = TrimWord(word);
                    //Можно использовать стемм библиотеку, но это тупня.
                    //word = Porter.TransformingWord(word);

                    if (di.ContainsKey(word)) { di[word]++; }
                    else { di.Add(word, 1); }
                
                        }
                    );
            }

            return di;
        }


        /// <summary>
        /// Функция, чтобы представлять вектор более компактно и сохранять в файл
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static List<string> VectorToIdx(float[] vec)
        {
            var lCompact = new List<string>();

            for (int i = 0; i < vec.Length; i++)
            {
                if (vec[i] != 0)
                {
                    lCompact.Add(string.Format("{0}:{1}", i, ((int)(vec[i]*1000000)).ToString()));
                }
            }

            return lCompact;

        }

        /// <summary>
        /// Функция, чтобы восстанавливать фектора из файла
        /// </summary>
        /// <param name="idxs">Компактное строчное представление вектора</param>
        /// <param name="length">Длина вектора</param>
        /// <returns></returns>
        public static float[] IdxToVector(string idxs, int length)
        {
            var vec = new float[length];
            var arr = idxs.Split(';');

            foreach (var item in arr)
            {
                var split = item.Split(':');
                vec[int.Parse(split[0])] = float.Parse(split[1])/1000000;
            }

            return vec;

        }

        /// <summary>
        /// Автогенератор имени файлов
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string GenerateFileName(string suffix)
        {
            var dts = System.DateTime.Now.ToString("ddmmyyyy_hhmm");
            if (suffix.Length ==  0)
                return dts;

            return dts+"_"+suffix;
        }

        /// <summary>
        /// Из огромного файла делает разбитый по "кластерам" словарь, где индекс слова, это имя кластера
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Dictionary<int,Dictionary<float[],string>> ParseTextsContext(string filename)
        {
            var Clusters = new Dictionary<int, Dictionary<float[], string>>();

            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var sr = new StreamReader(stream, Encoding.UTF8);

                string line = String.Empty;
                line = sr.ReadLine();
                var vectorLength = int.Parse(line.Trim());

                for (int i = 0; i < vectorLength; i++)
                {
                    Clusters.Add(i, new Dictionary<float[], string>());
                }

                while ((line = sr.ReadLine()) != null)
                {
                    var split = line.Split(';');
                    var vec = Helper.IdxToVector(split[2], vectorLength);
                    Clusters[int.Parse(split[1])].Add(vec, split[0]);

                }


            }

            return Clusters;
        }

    }

    /// <summary>
    /// Cпер с гитхаба https://github.com/SergeiGalkovskii/Porter-s-algorithm-for-stemming-for-russian-language-csharp
    /// Очень медленно. Убрал и сделал грубо.
    /// </summary>
    public static class Porter
    {

        private static Regex PERFECTIVEGROUND = new Regex("((ив|ивши|ившись|ыв|ывши|ывшись)|((<;=[ая])(в|вши|вшись)))$");

        private static Regex REFLEXIVE = new Regex("(с[яь])$");

        private static Regex ADJECTIVE = new Regex("(ее|ие|ые|ое|ими|ыми|ей|ий|ый|ой|ем|им|ым|ом|его|ого|ему|ому|их|ых|ую|юю|ая|яя|ою|ею)$");

        private static Regex PARTICIPLE = new Regex("((ивш|ывш|ующ)|((?<=[ая])(ем|нн|вш|ющ|щ)))$");

        private static Regex VERB = new Regex("((ила|ыла|ена|ейте|уйте|ите|или|ыли|ей|уй|ил|ыл|им|ым|ен|ило|ыло|ено|ят|ует|уют|ит|ыт|ены|ить|ыть|ишь|ую|ю)|((?<=[ая])(ла|на|ете|йте|ли|й|л|ем|н|ло|но|ет|ют|ны|ть|ешь|нно)))$");

        private static Regex NOUN = new Regex("(а|ев|ов|ие|ье|е|иями|ями|ами|еи|ии|и|ией|ей|ой|ий|й|иям|ям|ием|ем|ам|ом|о|у|ах|иях|ях|ы|ь|ию|ью|ю|ия|ья|я)$");

        private static Regex RVRE = new Regex("^(.*?[аеиоуыэюя])(.*)$");

        private static Regex DERIVATIONAL = new Regex(".*[^аеиоуыэюя]+[аеиоуыэюя].*ость?$");

        private static Regex DER = new Regex("ость?$");

        private static Regex SUPERLATIVE = new Regex("(ейше|ейш)$");

        private static Regex I = new Regex("и$");
        private static Regex P = new Regex("ь$");
        private static Regex NN = new Regex("нн$");

        public static string TransformingWord(string word)
        {
            word = word.ToLower();
            word = word.Replace('ё', 'е');
            MatchCollection m = RVRE.Matches(word);
            if (m.Count > 0)
            {
                Match match = m[0]; // only one match in this case 
                GroupCollection groupCollection = match.Groups;
                string pre = groupCollection[1].ToString();
                string rv = groupCollection[2].ToString();

                MatchCollection temp = PERFECTIVEGROUND.Matches(rv);
                string StringTemp = ReplaceFirst(temp, rv);


                if (StringTemp.Equals(rv))
                {
                    MatchCollection tempRV = REFLEXIVE.Matches(rv);
                    rv = ReplaceFirst(tempRV, rv);
                    temp = ADJECTIVE.Matches(rv);
                    StringTemp = ReplaceFirst(temp, rv);
                    if (!StringTemp.Equals(rv))
                    {
                        rv = StringTemp;
                        tempRV = PARTICIPLE.Matches(rv);
                        rv = ReplaceFirst(tempRV, rv);
                    }
                    else
                    {
                        temp = VERB.Matches(rv);
                        StringTemp = ReplaceFirst(temp, rv);
                        if (StringTemp.Equals(rv))
                        {
                            tempRV = NOUN.Matches(rv);
                            rv = ReplaceFirst(tempRV, rv);
                        }
                        else
                        {
                            rv = StringTemp;
                        }
                    }

                }
                else
                {
                    rv = StringTemp;
                }

                MatchCollection tempRv = I.Matches(rv);
                rv = ReplaceFirst(tempRv, rv);
                if (DERIVATIONAL.Matches(rv).Count > 0)
                {
                    tempRv = DER.Matches(rv);
                    rv = ReplaceFirst(tempRv, rv);
                }

                temp = P.Matches(rv);
                StringTemp = ReplaceFirst(temp, rv);
                if (StringTemp.Equals(rv))
                {
                    tempRv = SUPERLATIVE.Matches(rv);
                    rv = ReplaceFirst(tempRv, rv);
                    tempRv = NN.Matches(rv);
                    rv = ReplaceFirst(tempRv, rv);
                }
                else
                {
                    rv = StringTemp;
                }
                word = pre + rv;

            }

            return word;
        }

        public static string ReplaceFirst(MatchCollection collection, string part)
        {
            string StringTemp = "";
            if (collection.Count == 0)
            {
                return part;
            }
            /*else if(collection.Count == 1) 
            { 
            return StringTemp; 
            }*/
            else
            {
                StringTemp = part;
                for (int i = 0; i < collection.Count; i++)
                {
                    GroupCollection GroupCollection = collection[i].Groups;
                    if (StringTemp.Contains(GroupCollection[i].ToString()))
                    {
                        string deletePart = GroupCollection[i].ToString();
                        StringTemp = StringTemp.Replace(deletePart, "");
                    }

                }
            }
            return StringTemp;
        }

    }

}
