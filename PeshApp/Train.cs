using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PeshApp
{
    class Train
    {
        string[] _trainData;
        Model _model;
        Dictionary<string, float[]> _phrasesVectors;
        string[] _stopWords = new string[] {"потом","быть","было","были","меня","тебя","тебе",
            "есть","если","который","котор","один","такой","только","весь","всей","прост","просто","потом",
            "когда","надо","тольк","типа","чтобы","этого","этот","того","перед",
            "тоже","можно","про","мне","там","уже","нас","мой","как","она","так","это","том","нет",
            "тому","еще","для","где","раз","под","вот","или","лет","сво","вас","над","ней","они",
            "эта","оля","них","без","сами","мои","нам","тут","сто","своей","вам","нее","дает",
            "тех","оно","чтоб","моя","тем","сих","пор","тво","мир","эту","снов","ага","дом","две",
            "при","пол","остав","ваш","мая","три","либо","баб","сне","тож",
            "тип","пара","мат","нами","кого","ято","одног","нос","ого","жил","тест","ради","еле",
            "бог","логич","факт","кот","душ","дни","обе","вел","дам","вне","ряд","дал","еда","мин",
            "осталь","кроме","конеч","даже","почти","друг",
            "начин","вдруг","сказа","вообщ","кому","согла","короч",
            "после","начал","напри","мозг",
            "какой","всех","мало","людей","пока","стать","место","него","столь",
            "общем","нескол","всяки","одной","этому","дело","поэто","свою","этом","особе","всем","кстат","изза","след","значи",
            "свое","свои","наше","между","более","пора","часть","главн","таки","твое",
            "моем","одно","свой","будь","держа","тема","снова","предло","дать","стал","иной","нибуд"};


        public Train(string[] trainData)
        {
            _trainData = trainData;
            _model = new Model();
        }

        /// <summary>
        /// Обучить модель и задать вектора для всех фраз, чтобы потом искать в них.
        /// </summary>
        /// <returns></returns>
        public Model TrainModel()
        {
            if (_trainData == null)
            {
                return null;
            }

            //Определяем, какого размера вектор и какие там слова
            ArrangeVectorStructure();
            //Сопоставляем каждому слову вектор
            GetWordsContext();
            //Сопоставляем каждому предложению вектор
            GetPhrasesContext();
            //Сохраняем в файлы тексты
            var fname = SavePhrases();
            _model.Mark = fname;
            //Сохраняем в файлы модель
            _model.SaveModelToFile(fname+"_model.mod");

            return _model;
        }


        /// <summary>
        /// Сохраняем вектора фраз в файл. 
        /// </summary>
        string SavePhrases()
        {
            var filename = Helper.GenerateFileName("");
            var text = new List<string>();
            text.Add(_model.VectorLength.ToString());

            foreach (var k in _phrasesVectors.Keys)
            {
                var idx = _phrasesVectors[k].ToList().IndexOf(_phrasesVectors[k].Max());
                text.Add(string.Format("{0};{1};{2}",k, idx, string.Join(";", Helper.VectorToIdx(_phrasesVectors[k]))));
            }


            File.WriteAllLines(filename+"_txts.txt", text.ToArray());
            return filename;
        }

        /// <summary>
        /// Узнаем какого размера будет вектор и задаем последовательность слов в нем.
        /// </summary>
        void ArrangeVectorStructure()
        {
            _model.Dictonary = Helper.GetDictionary(_trainData);

            //Ограничение на размер вектора задается частотой слов, которые исключаются 
            //Исключаются очень частые и очень редкие
            // И слово не длиннее 40 символов.
            _model.Dictonary = _model.Dictonary.Where(a => DictionaryFilter(a)).ToDictionary(a => a.Key, b => b.Value);
            _model.VectorLength = _model.Dictonary.Count;

            _model.VectorStructure = _model.Dictonary.Keys.ToList();

        }

        bool DictionaryFilter(KeyValuePair<string,int> kpw)
        {
            if (kpw.Value <= 20)
                return false;
            //Важный параметр обрезания сверху общеупотребительных слов. 
            if (kpw.Value >7000)
                return false;
            //обрезаются слишком длинные слова
            if (kpw.Key.Length > 40)
                return false;
            //Список стопслов наверху
            if (_stopWords.Contains(kpw.Key))
                return false;

            return true;
        }


        /// <summary>
        /// Задаем контекст каждого слова
        /// </summary>
        void GetWordsContext()
        {

            //!!!Этот процесс параллелится для быстроты. Но я так не сделал!!!
            _model.Vecs = new Dictionary<string, float[]>();
            var wideWords = new List<string>();

            for (int i = 0; i < _model.VectorStructure.Count; i++)
            {
                var word = _model.VectorStructure[i];

                //Смотрим на то как обучается
                if (i % 50 == 0)
                {
                    Console.Clear();
                    Console.WriteLine((100 * (double)i / (double)_model.VectorStructure.Count).ToString() + "%");
                }

                //cоздаем вектор для слова
                var vector = new float[_model.VectorLength];

                //Учиться на всех слишком долго. Хотя бы на 1000 использованиях.
                var counter = 0;
                foreach (var phrase in _trainData)
                {
                    if (!phrase.Contains(word))
                        continue;
                    //Приводим слова в рабочее состояние
                    var phraseArr = phrase.Split(' ').Where(w => w.Length > 3).Select(a => Helper.TrimWord(a)).Distinct().ToList();
                    var wordPlace = phraseArr.IndexOf(word);

                    //N слов до, N после
                    var limit = 8;
                    var beg = wordPlace-limit<0?0 : wordPlace - limit;
                    var end = wordPlace + limit > phraseArr.Count-1 ? phraseArr.Count - 1 : wordPlace + limit;

                    //смотрим, есть ли они в векторе
                    for (int j = beg; j < end; j++)
                    {
                        var w = phraseArr[j];    
                        //С этой добавкой будет идеологически близкое искать, но не совсем.
                        //if (w == word)
                        //   continue;


                        var index = _model.VectorStructure.IndexOf(w);
                        if (index >= 0)
                        {
                            //Если наши слово в _vectorStructure, прибавляем в векторе 1.
                            vector[index]++;
                            counter++;

                        }

                    }
                    if (counter > 4000) { break; }

                }


                //Фильтр
                vector = vector.Select(a => a<3?0:a).ToArray();

                //Нормировка вектора
                var sum = vector.Sum();
                vector = vector.Select(a => a / sum).ToArray();


                //var count = vector.Where(a => a != 0).Count();
                ////Откидываем слова, которые слишком размазаны по вектору, а значит значат что попало.
                //if (count > 550)
                //{
                //    wideWords.Add(word);
                //    //continue;
                //}


                _model.Vecs.Add(word, vector);
            }

            //Чисто проверить, какие слова много значат.
            var jw = string.Join("\",\"",wideWords);
        }

        /// <summary>
        /// Для каждой фразы считается ее суммарный вектор
        /// </summary>
        void GetPhrasesContext()
        {
            //!!!Этот процесс параллелится для быстроты. Но я так не сделал!!!
            _phrasesVectors = new Dictionary<string, float[]>();
            for (int i = 0; i < _trainData.Length; i++)
            {
                var phrase = _trainData[i];
                //Смотрим на то как обучается
                if (i % 5000 == 0)
                {
                    Console.Clear();
                    Console.WriteLine((100 * (double)i / (double)_trainData.Count()).ToString() + "%");
                }

                var vector = _model.VectorizePhrase(phrase);

                if(vector == null)
                {
                    continue;
                }

                _phrasesVectors.Add(phrase, vector);
            }


        }
    } 
}
