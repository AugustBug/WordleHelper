using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WordleHelper
{
    public partial class MainWindow : Window
    {
        private MyWordleHelper helper;

        private const string KEYBOARD_LETTERS = "QWERTYUIOPASDFGHJKLZXCVBNM";

        private Dictionary<char, Label> mapKeyboardLabels;

        private List<SelectionLetter> listSelectionLetters;
        private const int REMAINING_WORDS_VIEW_COUNT = 200;

        enum SUGGEST_MODE
        {
            MAX_GREEN,
            MAX_YELLOW,
            SMART
        }
        private SUGGEST_MODE currentSuggestMode;

        public MainWindow()
        {
            InitializeComponent();

            helper = new MyWordleHelper();
            helper.init(this);

            currentSuggestMode = SUGGEST_MODE.MAX_GREEN;
        }

        #region KEYBOARD_PANEL
        public void createKeyboard()
        {
            mapKeyboardLabels = new Dictionary<char, Label>();

            // create row and columns
            for (int i = 0; i < 23; i++)
            {
                gridKeyboard.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < 3; i++)
            {
                gridKeyboard.RowDefinitions.Add(new RowDefinition());
            }

            int leftIndex = 0;
            int topIndex = 0;
            for (int i = 0; i < KEYBOARD_LETTERS.Length; i++)
            {
                if (i == 10)
                {
                    leftIndex = 1;
                    topIndex = 1;
                }
                else if (i == 19)
                {
                    leftIndex = 2;
                    topIndex = 2;
                }

                Label lbl = new Label();
                lbl.Content = KEYBOARD_LETTERS[i];
                lbl.Background = getResultColor(MyWordleHelper.RESULT.EMPTY);
                lbl.FontWeight = FontWeights.Bold;
                lbl.HorizontalContentAlignment = HorizontalAlignment.Center;
                lbl.BorderBrush = Brushes.White;
                lbl.BorderThickness = new Thickness(1);
                Grid.SetColumn(lbl, leftIndex);
                Grid.SetColumnSpan(lbl, 2);
                Grid.SetRow(lbl, topIndex);

                gridKeyboard.Children.Add(lbl);
                mapKeyboardLabels.Add(KEYBOARD_LETTERS[i], lbl);

                leftIndex += 2;
            }
            // dummies
            Label lblDummy1 = new Label();
            lblDummy1.Background = Brushes.LightGray;
            Grid.SetColumn(lblDummy1, 19);
            Grid.SetColumnSpan(lblDummy1, 2);
            Grid.SetRow(lblDummy1, 1);

            gridKeyboard.Children.Add(lblDummy1);

            Label lblDummy2 = new Label();
            lblDummy2.Background = Brushes.LightGray;
            Grid.SetColumn(lblDummy2, 0);
            Grid.SetColumnSpan(lblDummy2, 2);
            Grid.SetRow(lblDummy2, 2);

            gridKeyboard.Children.Add(lblDummy2);

            Label lblDummy3 = new Label();
            lblDummy3.Background = Brushes.LightGray;
            Grid.SetColumn(lblDummy3, 16);
            Grid.SetColumnSpan(lblDummy3, 5);
            Grid.SetRow(lblDummy3, 2);

            gridKeyboard.Children.Add(lblDummy3);

        }

        public void clearKeyboardPanel()
        {
            Dictionary<char, Label>.KeyCollection mapKeyboardLetters = mapKeyboardLabels.Keys;
            foreach (char letter in mapKeyboardLetters)
            {
                mapKeyboardLabels[letter].Background = getResultColor(MyWordleHelper.RESULT.EMPTY);
            }
        }

        public void refreshKeyboardLetters(List<Tuple<char, MyWordleHelper.RESULT>> guess)
        {
            foreach (Tuple<char, MyWordleHelper.RESULT> tup in guess)
            {
                if (tup.Item2 == MyWordleHelper.RESULT.CORRECT)
                {
                    mapKeyboardLabels[tup.Item1].Background = getResultColor(MyWordleHelper.RESULT.CORRECT);
                }
                else if (tup.Item2 == MyWordleHelper.RESULT.CORRECT_BUT_WRONG_POS)
                {
                    if (mapKeyboardLabels[tup.Item1].Background != getResultColor(MyWordleHelper.RESULT.CORRECT))
                    {
                        mapKeyboardLabels[tup.Item1].Background = getResultColor(MyWordleHelper.RESULT.CORRECT_BUT_WRONG_POS);
                    }
                }
                else if (tup.Item2 == MyWordleHelper.RESULT.WRONG)
                {
                    if (mapKeyboardLabels[tup.Item1].Background != getResultColor(MyWordleHelper.RESULT.CORRECT))
                    {
                        mapKeyboardLabels[tup.Item1].Background = getResultColor(MyWordleHelper.RESULT.WRONG);
                    }
                }
            }
        }

        #endregion

        public void setPreviousGuess(int index, List<Tuple<char, MyWordleHelper.RESULT>> result)
        {
            if (result.Count == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    Label lbl = new Label();
                    lbl.Content = result[i].Item1;
                    lbl.Background = getResultColor(result[i].Item2);
                    lbl.HorizontalContentAlignment = HorizontalAlignment.Center;
                    lbl.FontWeight = FontWeights.Bold;

                    Grid.SetColumn(lbl, i + 1);
                    Grid.SetRow(lbl, index * 2);
                    gridPreviousGuesses.Children.Add(lbl);
                }
            }
        }

        // should be called after analizeLetterPossibilities since required map is filled there
        public void refreshWordSuggestions(List<string> words)
        {
            // clear suggestions
            lbSuggestions.Items.Clear();
            lbSuggestionsAvg.Items.Clear();
            lbSuggestionsMin.Items.Clear();
            lbSuggestionsCoef.Items.Clear();

            int suggCount = 8;
            List<Tuple<string, double>> coefs = new List<Tuple<string, double>>();
            for (int i = 0; i < suggCount; i++)
            {
                coefs.Add(new Tuple<string, double>("AAAAA", 0));
            }

            foreach (string word in words)
            {
                double coef = 1;

                if (currentSuggestMode == SUGGEST_MODE.MAX_GREEN)
                {
                    coef = 100;
                    for (int i = 0; i < 5; i++)
                    {
                        coef *= helper.getGreenPossibility(i, word[i]);
                    }
                }
                else if (currentSuggestMode == SUGGEST_MODE.MAX_YELLOW)
                {
                    coef = 1;
                    for (int i = 0; i < 5; i++)
                    {
                        if (word.Substring(0, i).Contains(word[i]))
                        {
                            coef *= 0.5;
                        }
                        Console.WriteLine(word + " : " + i);
                        coef *= helper.getYellowPossibility(word[i]);
                    }
                }

                if (coef > coefs[suggCount - 1].Item2)
                {
                    // enter and try to climb up
                    coefs[suggCount - 1] = new Tuple<string, double>(word, coef);
                    for (int k = suggCount - 2; k >= 0; k--)
                    {
                        if (coef > coefs[k].Item2)
                        {
                            string tempWord = coefs[k].Item1;
                            double tempVal = coefs[k].Item2;
                            coefs[k] = new Tuple<string, double>(word, coef);
                            coefs[k + 1] = new Tuple<string, double>(tempWord, tempVal);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                //Console.WriteLine(word + " : " + coef.ToString("0.######"));
            }

            for (int i = 0; i < suggCount; i++)
            {
                if (coefs[i].Item2 > 0)
                {
                    // print
                    if (currentSuggestMode == SUGGEST_MODE.MAX_GREEN)
                    {
                        lbSuggestions.Items.Add(coefs[i].Item1);
                        lbSuggestionsCoef.Items.Add(coefs[i].Item2.ToString("0.####"));
                    }
                    else if (currentSuggestMode == SUGGEST_MODE.MAX_YELLOW)
                    {
                        lbSuggestions.Items.Add(coefs[i].Item1);
                        lbSuggestionsMin.Items.Add(coefs[i].Item2.ToString("0.####"));
                    }
                }
            }
        }

        public void createInputPanels()
        {
            listSelectionLetters = new List<SelectionLetter>();
            for (int i = 0; i < 5; i++)
            {
                SelectionLetter sLetter = new SelectionLetter();
                sLetter.init(this, i);
                Grid.SetRow(sLetter, 0);
                Grid.SetColumn(sLetter, i);

                gridMidTop.Children.Add(sLetter);
                listSelectionLetters.Add(sLetter);
            }
        }

        public void clearSelectionPanel()
        {
            for (int i = 0; i < listSelectionLetters.Count; i++)
            {
                listSelectionLetters[i].reset();
            }
        }

        public List<SelectionLetter> getSelectionLetters()
        {
            return listSelectionLetters;
        }

        private void autoSelectWord(string word)
        {
            if (word.Trim().Length == 5)
            {
                for (int i = 0; i < listSelectionLetters.Count; i++)
                {
                    listSelectionLetters[i].autoSet(word[i]);
                }
            }
        }

        private void printPossibility(Label lbl, List<Tuple<char, double>> poss)
        {
            string s = "";
            foreach (Tuple<char, double> tup in poss)
            {
                if (tup.Item2 > 0)
                {
                    s += tup.Item1 + " %" + (100 * tup.Item2).ToString("0.##") + "\n";
                }
            }
            lbl.Content = s;
        }

        public void refreshLetterPossibilities()
        {
            // analize letter possibilities for letter positions
            printPossibility(tb1, helper.analizeLetterPossibility(0));
            printPossibility(tb2, helper.analizeLetterPossibility(1));
            printPossibility(tb3, helper.analizeLetterPossibility(2));
            printPossibility(tb4, helper.analizeLetterPossibility(3));
            printPossibility(tb5, helper.analizeLetterPossibility(4));
        }

        #region UTILITIES
        private int minOf(int first, int second)
        {
            return first < second ? first : second;
        }

        private Brush getResultColor(MyWordleHelper.RESULT res)
        {
            if (res == MyWordleHelper.RESULT.CORRECT)
            {
                return Brushes.Green;
            }
            else if (res == MyWordleHelper.RESULT.CORRECT_BUT_WRONG_POS)
            {
                return Brushes.Yellow;
            }
            else if (res == MyWordleHelper.RESULT.WRONG)
            {
                return Brushes.Gray;
            }
            return Brushes.LightGray;
        }
        #endregion

        #region UI_REFS

        public void printGuessNum(int num)
        {
            lblGuessNum.Content = "GUESS " + num;
        }

        public void setInitRestartButton(bool isInit)
        {
            btnInitRestart.Content = isInit ? "INIT" : "RESTART";
        }

        public void setSubmitButtonEnable(bool isEnable)
        {
            btnSubmmit.IsEnabled = isEnable;
        }

        public void showMessage(string message)
        {
            MessageBox.Show(message);
        }

        public void stepOnGuess(int guessIteration)
        {
            if (guessIteration > 5)
            {
                btnSubmmit.IsEnabled = false;
            }
            else
            {
                lblGuessNum.Content = "GUESS " + (guessIteration + 1);
            }
        }

        public void clearPreviousGuesses()
        {
            gridPreviousGuesses.Children.Clear();
        }

        public void refreshRemainingWordsLB(List<string> words)
        {
            // TODO
            // page structure for remaining words

            lbRemainingWords.Items.Clear();
            for (int i = 0; i < minOf(REMAINING_WORDS_VIEW_COUNT, words.Count); i++)
            {
                lbRemainingWords.Items.Add(words[i]);
            }

            // if words count is zero - stucked
            // TODO

            lblWordsCandidateCount.Content = words.Count + " possible words";
        }
        #endregion

        #region INTERACTIONS
        private void BtnInitRestart_Click(object sender, RoutedEventArgs e)
        {
            helper.initForSuggestion();
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // to be able to create fivy file
            helper.extract5LenWords();
        }

        private void BtnSubmmit_Click(object sender, RoutedEventArgs e)
        {
            helper.submit();
        }

        private void LbRemainingWords_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbRemainingWords.SelectedIndex >= 0)
            {
                string selectedItem = lbRemainingWords.SelectedItem.ToString();
                autoSelectWord(selectedItem);
            }
        }

        private void LbSuggestions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbSuggestions.SelectedIndex >= 0)
            {
                string selectedItem = lbSuggestions.SelectedItem.ToString();
                autoSelectWord(selectedItem);
            }
        }

        private void BtnSuggestModeSmart_Click(object sender, RoutedEventArgs e)
        {
            currentSuggestMode = SUGGEST_MODE.SMART;
            // refreshWordSuggestions(helper.getFilteredWords());
            showMessage("Not implemented yet!");
        }

        private void BtnSuggestModeYellow_Click(object sender, RoutedEventArgs e)
        {
            currentSuggestMode = SUGGEST_MODE.MAX_YELLOW;
            refreshWordSuggestions(helper.getFilteredWords());
        }

        private void BtnSuggestModeGreen_Click(object sender, RoutedEventArgs e)
        {
            currentSuggestMode = SUGGEST_MODE.MAX_GREEN;
            refreshWordSuggestions(helper.getFilteredWords());
        }
        #endregion

        #region SELECTION_LETTER_CALLS
        public bool isValidChar(char c)
        {
            return helper.isValidChar(c);
        }

        public char getValidChar(char c)
        {
            return helper.getValidChar(c);
        }
        #endregion
    }
}
