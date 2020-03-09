using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PeshApp
{
    class Model
    {
        Dictionary<string, int> _dictonary;
        List<string> _vectorStructure;
        int _vectorLength = 0;
        Dictionary<string, float[]> _vecs;
        string _mark = "";
        float[] _weigths;

        public Dictionary<string, int> Dictonary { get => _dictonary; set => _dictonary = value; }
        public List<string> VectorStructure { get => _vectorStructure; set => _vectorStructure = value; }
        public int VectorLength { get => _vectorLength; set => _vectorLength = value; }
        public Dictionary<string, float[]> Vecs { get => _vecs; set => _vecs = value; }
        public string Mark { get => _mark; set => _mark = value; }
        public float[] Weigths { get => _weigths; set => _weigths = value; }

        public Model()
        {


        }

        /// <summary>
        /// Загрузить обученную модель из файла
        /// </summary>
        /// <param name="filename"></param>
        public Model(string filename)
        {
            var textmodel = File.ReadAllText(filename).Split('\n').Select(a=>a.Trim()).ToList();
            VectorLength =  int.Parse(textmodel[0]);
            VectorStructure = textmodel[1].Split(';').ToList();
            Vecs = new Dictionary<string, float[]>();
            Weigths = new float[VectorLength]; 

            for (int i = 2; i < textmodel.Count; i++)
            {
                if (textmodel[i] == "")
                    continue;
                var row = textmodel[i].Split(';');
                var vector = new float[VectorLength];
                for (int j = 1; j < row.Count(); j++)
                {
                    var split = row[j].Split(':');
                    vector[int.Parse(split[0])] = float.Parse(split[1]) / 1000000;
                }
                var weight = row.Count() - 1;
                Weigths[VectorStructure.IndexOf(row[0])] = (float)1/(float)(weight+1);
                Vecs.Add(row[0], vector);

            }
        }


        /// <summary>
        /// Созраняет основные данные о модели в файл
        /// </summary>
        /// <param name="filename"></param>
        public void SaveModelToFile(string filename)
        {
            var text = new List<string>();
            text.Add(VectorLength.ToString());
            text.Add(string.Join(";",VectorStructure));

            foreach (var k in Vecs.Keys)
            {
                text.Add(string.Format("{0};{1}", k, string.Join(";", Helper.VectorToIdx(Vecs[k]))));
            }


            File.WriteAllLines(filename, text.ToArray());

        }

        public float[] VectorizePhrase(string phrase)
        {
            return VectorizePhrase(phrase, false) ;
        }

        public float[] VectorizePhrase(string phrase, bool useWeight)
        {
            var vector = new float[VectorLength];
            var phraseArr = phrase.Split(' ').Where(w => w.Length > 3).Select(a => Helper.TrimWord(a)).ToArray();

            foreach (var w in phraseArr)
            {
                var index =  VectorStructure.IndexOf(w);
                if (index >= 0)
                {
                    var toadd = Vecs[w];
                    if (useWeight)
                    {
                        toadd = toadd.Select(a=>a*Weigths[index]).ToArray();
                    }
                    vector = vector.Zip(toadd, (x, y) => x + y).ToArray();
                }

            }
            //Нормировка вектора
            var sum = vector.Sum();

            //Фраза с очень редкими словами
            if (sum == 0)
            {
                return null;
            }

            vector = vector.Select(a => a / sum).ToArray();

            return vector;

        }


    }

}
