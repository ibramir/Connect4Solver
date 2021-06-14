using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Connect4Solver.Model;

namespace Connect4Solver.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly DependencyProperty AutoSolveProperty =
            DependencyProperty.Register("AutoSolve", typeof(bool),
                typeof(MainWindow), new UIPropertyMetadata(false));

        private bool AutoSolve
        {
            get => (bool) GetValue(AutoSolveProperty);
            set => SetValue(AutoSolveProperty, value);
        }
        
        private Dictionary<string, Shape> _shapes;
        private TextBlock[] _scoreTexts;
        private Board _board;
        private Stone _turn;
        private Solver _solver;


        public MainWindow()
        {
            InitializeComponent();
            InitDictionary();
            _scoreTexts = new[] {Score1, Score2, Score3, Score4, Score5, Score6, Score7};
            _solver = new Solver();
            Reset();
        }

        private void Reset()
        {
            _solver.Reset();
            _board = new Board();
            _turn = Stone.Red;
            AutoSolve = false;
            Brush white = new SolidColorBrush(Colors.GhostWhite);
            foreach (var shape in _shapes.Values)
            {
                shape.Fill = white;
                shape.Tag = "empty";
            }

            HideScores();
            TimeText.Visibility = Visibility.Hidden;
            SolveButton.IsEnabled = false;
        }

        private void HideScores()
        {
            foreach (TextBlock text in _scoreTexts)
            {
                text.Visibility = Visibility.Hidden;
            }
        }

        private void NewClick(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void SolveClick(object sender, RoutedEventArgs e)
        {
            Solve();
        }

        private void Solve()
        {
            var watch = Stopwatch.StartNew();
            int?[] scores = _solver.Analyze(_board);
            watch.Stop();
            for (int i = 0; i < Board.W; i++)
            {
                if (scores[i] != null)
                {
                    _scoreTexts[i].Text = scores[i].ToString();
                    _scoreTexts[i].Visibility = Visibility.Visible;
                }
            }

            SolveButton.IsEnabled = false;
            TimeText.Visibility = Visibility.Visible;
            TimeText.Text = watch.ElapsedMilliseconds >= 1000
                ? $"{(double) watch.ElapsedMilliseconds / 1000:0.000}s"
                : $"{watch.ElapsedMilliseconds}ms";
        }

        private void OnColumnClick(object sender, RoutedEventArgs e)
        {
            TimeText.Visibility = Visibility.Hidden;
            int col = int.Parse((sender as Button).Tag as string) - 1;
            if (!_board.CanPlay(col))
                return;
            bool win = _board.IsWinningMove(col);
            _board.Play(col);
            for (int i = 1; i <= Board.H; i++)
            {
                var shape = _shapes[$"{col + 1}{i}"];
                if (!shape.Tag.Equals("empty"))
                    continue;
                shape.SetResourceReference(Shape.FillProperty,
                    _turn == Stone.Red ? "RedGradient" : "YellowGradient");
                shape.Tag = _turn;
                break;
            }

            if (win)
            {
                MessageBox.Show($"{_turn.ToString()} wins", "Game over");
                Reset();
            }
            else if (_board.IsDraw())
            {
                MessageBox.Show("Draw", "Game over");
                Reset();
            }
            else
            {
                HideScores();
                _turn = _turn == Stone.Red ? Stone.Yellow : Stone.Red;
                if (AutoSolve)
                    Solve();
                else
                    SolveButton.IsEnabled = true;
            }
        }

        private void InitDictionary()
        {
            _shapes = new Dictionary<string, Shape>
            {
                ["11"] = S11,
                ["21"] = S21,
                ["31"] = S31,
                ["41"] = S41,
                ["51"] = S51,
                ["61"] = S61,
                ["71"] = S71,
                ["12"] = S12,
                ["22"] = S22,
                ["32"] = S32,
                ["42"] = S42,
                ["52"] = S52,
                ["62"] = S62,
                ["72"] = S72,
                ["13"] = S13,
                ["23"] = S23,
                ["33"] = S33,
                ["43"] = S43,
                ["53"] = S53,
                ["63"] = S63,
                ["73"] = S73,
                ["14"] = S14,
                ["24"] = S24,
                ["34"] = S34,
                ["44"] = S44,
                ["54"] = S54,
                ["64"] = S64,
                ["74"] = S74,
                ["15"] = S15,
                ["25"] = S25,
                ["35"] = S35,
                ["45"] = S45,
                ["55"] = S55,
                ["65"] = S65,
                ["75"] = S75,
                ["16"] = S16,
                ["26"] = S26,
                ["36"] = S36,
                ["46"] = S46,
                ["56"] = S56,
                ["66"] = S66,
                ["76"] = S76
            };
        }
    }
}