using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Buttons
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class QuetionData
    {
        [Index(0)]
        public string Question { get; set; }

        [Index(1)]
        public string Answer1 { get; set; }

        [Index(2)]
        public string Answer2 { get; set; }
    }

    public partial class MainWindow : Window
    {
        private SerialPort serialPort;
        private string logs = string.Empty;
        private bool isFisrt = true;
        private QuetionData[] quetionDatas = new QuetionData[0];
        private int questionNumber = 0;
        private QuetionData currentQuestionData;
        private Label[] answerLabels = new Label[2];

        private void readCSVFromPath(string path)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            var reader = new StreamReader(path);
            var csvReader = new CsvHelper.CsvReader(reader, config);
            quetionDatas = csvReader.GetRecords<QuetionData>().ToArray();
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnMainWindowLoaded;
            Ports.SelectionChanged += OnPortsSelectionChanged;
            KeyDown += OnKeyDown;
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                var csvPath = args[1];
                readCSVFromPath(csvPath);
            }
            answerLabels[0] = AnswerLeft;
            answerLabels[1] = AnswerRight;
        }

        private void handleSpace()
        {
            if (
                Question.Text != string.Empty && (
                    AnswerLeft.Content.ToString() == string.Empty ||
                    AnswerRight.Content.ToString() == string.Empty ||
                    CorrectAnswer.Content.ToString() == string.Empty
                )
            ) return;

            Question.Text = string.Empty;
            AnswerLeft.Content = string.Empty;
            AnswerRight.Content = string.Empty;
            CorrectAnswer.Content = string.Empty;
            Last.Content = string.Empty;

            if (questionNumber >= quetionDatas.Length) return;

            updateScore();

            currentQuestionData = quetionDatas[questionNumber];

            Question.Text = currentQuestionData.Question + " (" + currentQuestionData.Answer1 + " или " + currentQuestionData.Answer2 + ")";

            questionNumber++;
        }

        private void updateScore()
        {
            ScoreLeft.Content = Int32.Parse(ScoreLeft.Content.ToString()) + 1;
            ScoreRight.Content = Int32.Parse(ScoreRight.Content.ToString()) + 1;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                isFisrt = true;

                var newLine = "==";
                ListEvents.Items.Insert(0, newLine);
                try
                {
                    if (serialPort == null)
                    {
                        throw new Exception();
                    }
                    serialPort.WriteLine("red");
                }
                catch (Exception exception)
                {
                    OpenPort(Ports.Items[0] as string);
                }

                handleSpace();
            }

            if (CorrectAnswer.Content.ToString() == String.Empty && currentQuestionData != null)
            {
                if (e.Key == Key.D1)
                {
                    CorrectAnswer.Content = currentQuestionData.Answer1;
                }

                if (e.Key == Key.D2)
                {
                    CorrectAnswer.Content = currentQuestionData.Answer2;
                }
            }
        }

        private void OnPortsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var port = e.AddedItems[0] as string;

            OpenPort(port);
        }

        private void OpenPort(string port)
        {
            try
            {
                serialPort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                serialPort.Open();
                serialPort.DataReceived += OnSerialPortDataReceived;
                serialPort.WriteLine("red");
                Ports.Visibility = Visibility.Hidden;
            }
            catch (Exception exception)
            {
            }
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {   
            foreach (var portName in SerialPort.GetPortNames())
            {
                Ports.Items.Add(portName);
            }
        }

        private void handleButtonPressed(string line)
        {
            if (currentQuestionData != null && Int32.TryParse(line, out int buttonNumber))
            {
                int player = (buttonNumber - 1) / 2;
                var answer = (buttonNumber + 1) % 2 == 0 ? currentQuestionData.Answer1 : currentQuestionData.Answer2;

                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    if (answerLabels[player].Content == string.Empty)
                    {
                        answerLabels[player].Content = answer;
                    }
                }));
                isFisrt = false;
            }
        }

        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
            {
                var line = serialPort.ReadLine();
                logs += line;

                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    ListEvents.Items.Insert(0, line);
                    var lastStrings = Last.Content as string;
                    if (!lastStrings.Any(x => x == line.First()))
                    {
                        Last.Content += line.First() + Environment.NewLine;
                    }
                }));

                handleButtonPressed(line);
            }
        }
    }
}
