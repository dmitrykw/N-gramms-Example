using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using System.Text.RegularExpressions;

namespace NGramsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }

        }



        private void button2_Click(object sender, EventArgs e)
        {

            if (backgroundWorker1.IsBusy != true)
            {
                this.backgroundWorker1.RunWorkerAsync();
            }
        }

        //Функция получения контекстного предложения в котором первым зарегистрировано слово или словосочетание
        private string GetFirstSentence(string word, string alltext) //Принимаем слово(или словосочетание) и текст, в котором ищем
        {
            string[] SplittedSentences = alltext.Split(new char[] {'.'}); //Разделяем текст на предложения по точке
            string[] Sentences = SplittedSentences.Where(x => x != "").ToArray(); //Вырезаем пустые элементы массива

            foreach (string Sentence in Sentences) 
            {
                int Count = new Regex(word).Matches(Sentence).Count; //В цикле регуляркой находим первое предложение в котором зарегистрировалось слово или словосочетание, и сразу же выходим возвращая результат (это предложение и возвращаем), не дожидаясь завершения цика.
                if (Count > 0)
                {
                    return Sentence;
                }                
            }
            return ""; 
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
           

            string[] Files = Directory.GetFiles(textBox1.Text); //Получаем файлы список файлов

            string Alltext = ""; //Объявляем переменную для текста

            foreach (string myFile in Files)  //Собираем все файлы в одну переменную
            {
                Alltext = Alltext + File.ReadAllText(myFile);      //Записываем содержимое каждого файла в переменную
            }


            // Если стоит галка переводим всё что будем сравнивать в нижний регистр, чтобы при расчетах избежать разнницы в регистре.
            if (checkBox1.Checked == false)
            {
                Alltext = Alltext.ToLower();//Получаем нижний регистр
            }


            //Разбиваем текст на массив слов
            string[] SplittedWords = Alltext.Split(new char[] { '.', ',', ' ', '-', ';', ':', ')', '(','–', '!', '?' });
            //Удаляем пустые элементы массива
            string[] Words = SplittedWords.Where(x => x != "").ToArray();

            //Формируем Datatable
            DataTable dt = new DataTable();
            dt.CaseSensitive = true; //Включаем регистрозависимость
            dt.Columns.Add("word", typeof(string)); //Задаем названия столбцов и типы даннных
            dt.Columns.Add("count", typeof(int));
            dt.Columns.Add("firstsentence", typeof(string));


            int amount = Convert.ToInt32(textBox2.Text); //Получаем кол-во слов которые нам надо учитывать.
            if (amount > 1) //Если их больше одного увеличим на единицу - нужно для верности дальнейших расчетов
            {
                amount++;
            }

            string LongWord = ""; //Создадим переменную для длинного слова. Оно потербуется если amount (кол-во слов nграммы) больше одного.

            Action action = () => { progressBar1.Maximum = Words.Count(); }; //Записываем в прогресс бар максимальное значение равное кол-ву обрабатываемых слов
            Invoke(action);
            

            int i = 1;
            foreach (string Word in Words) //Перебираем массив слов
            {
                backgroundWorker1.ReportProgress(i); //Инкрементируем прогрессбар
                i++;

                if (amount > 1) //Если колво слов больше одного - вычтем один на этой итерации и добавим текущее итерируемое word к LongWord
                {
                    amount = amount - 1;
                    LongWord = LongWord + " " + Word;
                    LongWord = LongWord.Trim(); //Обрежем пробелы по краям
                }
                else //Если кол - слов не больше 1, значит или мы набрали уже достаточно в Longword, либо вообще был выбрано только одно слово по умолчанию и небыло никакого LongWord :)
                {
                    if (LongWord != "") //Если LongWord не пустое, значит было выбрано более одного слова, а LongWord уже наполнился и мы можем приступать к его обработке
                    {
                        if (dt.Select("word = '" + LongWord + "'").Count() < 1) //Проверяем что это слово уже небыло добавлено в таблицу на предыдущих итерациях - проверяем, что выборка из datatable по этому слову выдает 0 строк.
                        {
                            int Count = new Regex("\\W" + LongWord + "\\W").Matches(Alltext).Count; //Считаем колво вхождений слова во всем тексте регулярками
                            string Sentencecontext = GetFirstSentence(LongWord, Alltext); //Получаем от соответвующего метода предложение в котором было употреблено первый раз данное словосочетание

                            DataRow row = dt.NewRow(); //Добавляем новую строку
                            row["word"] = LongWord;
                            row["count"] = Count;
                            row["firstsentence"] = Sentencecontext;
                            dt.Rows.Add(row);
                            dt.AcceptChanges();
                        }
                        LongWord = ""; //Обнуляем LongWord
                        amount = Convert.ToInt32(textBox2.Text) + 1; //Возвращаем amount исходное значение, чтобы ожидать новое соловосочетание на следующих итерациях цикла
                    }
                    else //Если LongWord таки оказалось пустым, значит было выбрано одно слово в качетсве n-граммы
                        if (dt.Select("word = '" + Word + "'").Count() < 1) //Проверяем что это слово уже небыло добавлено в таблицу на предыдущих итерациях - проверяем, что выборка из datatable по этому слову выдает 0 строк.
                    {
                        int Count = new Regex("\\W" + Word + "\\W").Matches(Alltext).Count; //Считаем колво вхождений слова во всем тексте регулярками
                        string Sentencecontext = GetFirstSentence(Word, Alltext); //Получаем от соответвующего метода предложение в котором было употреблено первый раз данное словосочетание

                        DataRow row = dt.NewRow();//Добавляем новую строку
                        row["word"] = Word;
                        row["count"] = Count;
                        row["firstsentence"] = Sentencecontext;
                        dt.Rows.Add(row);
                        dt.AcceptChanges();
                    }
                }

            }

            //Удаляем из таблицы строки в которых счетчик вхождений равен 0            
            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToInt32(row["count"]) == 0)
                {
                    row.Delete();
                }
            }
            dt.AcceptChanges();

            e.Result = dt;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGridView1.DataSource = e.Result; //Заполняем datagridview
            dataGridView1.Sort(dataGridView1.Columns["count"], ListSortDirection.Descending); // Сортируем по count
           

        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
    }
}
